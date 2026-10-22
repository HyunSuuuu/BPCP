using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Customers;
using BurgerIdle.Economy;
using BurgerIdle.Money;
using BurgerIdle.Pickup;
using R3;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 카운터 판매. 응대 위치에 판매자가 있고, 재고가 있고, 줄 맨 앞 손님이 대기 중이면 일정 간격으로 1개씩 판매한다.
    /// 판매자는 플레이어가 우선이고, 플레이어가 없으면 배정된 알바가 판다 (간격: 플레이어 고정, 알바는 기본 간격 ÷ 행동 속도).
    /// 판매 수익은 카운터 돈 더미에 쌓이고(수거해야 소지금), 경험치는 이 카운터가 속한 지역 레벨에 바로 지급한다. 판매가는 물건 종류별 (GameConfig.PriceOf).
    /// 맨 앞 대상이 판매 준비(손님은 테이블 좌석 예약)에 실패하면 판매하지 않는다.
    /// 드라이브스루 카운터도 이 컴포넌트를 쓴다 (Sell Kind = Packed Burger, Queue = 차량 줄).
    /// </summary>
    public sealed class Counter : MonoBehaviour, ISupplyTarget
    {
        static readonly List<Counter> active = new List<Counter>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        [Header("Sell")]
        [Tooltip("파는 종류. 재고 더미는 이 종류만 받는다. 일반 카운터 = Burger, 드라이브스루 = Packed Burger")]
        [SerializeField] PickupKind sellKind = PickupKind.Burger;

        [Header("Links")]
        [Tooltip("음식 재고 (PickupReceiver로 채워지는 더미)")]
        [SerializeField] PickupPile stock;
        [Tooltip("판매자가 서는 응대 위치")]
        [SerializeField] InteractionZone servicePoint;
        [Tooltip("손님 줄 (드라이브스루는 차량 줄)")]
        [SerializeField] CustomerQueue queue;
        [SerializeField] MoneyPile moneyPile;

        PlayerLevel level;
        Transform player;
        GameConfig config;
        ReadOnlyReactiveProperty<float> workerActionSpeed;
        Transform workerSeller;
        QueueMember currentCustomer;
        IPickupReceiver receiver;
        float progress;

        /// <summary>해금되어 활성화된 카운터 (알바의 판매 대상 탐색용)</summary>
        public static IReadOnlyList<Counter> Active => active;

        public PickupKind Kind => sellKind;
        public int Shortage => StockShortage;
        public IPickupReceiver Receiver => receiver ??= SupplyTargets.FindReceiver(this, stock);

        public InteractionZone ServicePoint => servicePoint;
        public bool IsPlayerServing => player != null && servicePoint.Contains(player.position);

        /// <summary>지금 실제로 팔 수 있는데 플레이어가 응대 위치에 없다 → 알바가 맡을 수 있음</summary>
        public bool NeedsSeller => CanServeFront && !IsPlayerServing;

        /// <summary>맨 앞 손님이 좌석을 구하지 못해 판매가 막혀 있다 → 청소가 가장 급함</summary>
        public bool IsBlockedBySeat
        {
            get
            {
                var front = WaitingFront;
                return front != null && !front.TryPrepareForService();
            }
        }

        /// <summary>줄의 남은 주문량보다 재고가 모자란 개수 (0이면 운반 불필요)</summary>
        public int StockShortage => Mathf.Max(0, queue.RemainingDemand - stock.Count.CurrentValue);

        /// <summary>맨 앞 손님이 기다리고, 좌석이 확보(또는 확보 가능)되고, 재고가 있다</summary>
        bool CanServeFront
        {
            get
            {
                var front = WaitingFront;
                return front != null && stock.Count.CurrentValue > 0 && front.TryPrepareForService();
            }
        }

        QueueMember WaitingFront
        {
            get
            {
                var front = queue.Front;
                return front != null && front.State.CurrentValue == QueueMemberState.AtCounter ? front : null;
            }
        }

        void OnEnable()
        {
            active.Add(this);
            SupplyTargets.Register(this);
        }

        void OnDisable()
        {
            active.Remove(this);
            SupplyTargets.Unregister(this);
        }

        /// <param name="level">이 카운터가 속한 지역의 레벨 (판매 경험치를 받음)</param>
        public void Init(PlayerLevel level, Transform player, GameConfig config, ReadOnlyReactiveProperty<float> workerActionSpeed)
        {
            this.level = level;
            this.player = player;
            this.config = config;
            this.workerActionSpeed = workerActionSpeed;

            if (stock == null || servicePoint == null || queue == null || moneyPile == null)
            {
                Debug.LogError($"[Counter] {name}: Stock, Service Point, Queue, Money Pile을 연결하세요.", this);
                enabled = false;
                return;
            }

            stock.SetAcceptedKinds(sellKind);
        }

        /// <summary>알바가 판매를 맡는다. 응대 위치에 도착해야 실제로 판매한다.</summary>
        public void AssignWorker(Transform worker) => workerSeller = worker;

        public void ReleaseWorker(Transform worker)
        {
            if (workerSeller == worker)
                workerSeller = null;
        }

        void Update()
        {
            if (level == null)
                return;

            var customer = queue.Front;
            if (customer == null || customer.State.CurrentValue != QueueMemberState.AtCounter)
                return;

            // 손님이 바뀌면 진행도 초기화, 판매자가 자리를 비우면 진행도 유지한 채 멈춤
            if (customer != currentCustomer)
            {
                currentCustomer = customer;
                progress = 0f;
            }

            // 손님은 빈 테이블 좌석을 예약해야 음식을 받을 수 있다 (예약 실패 시 계속 대기)
            if (!customer.TryPrepareForService() || stock.Count.CurrentValue == 0)
                return;

            if (!TryGetSellInterval(out var interval))
                return;

            progress += Time.deltaTime;
            if (progress < interval)
                return;

            progress = 0f;
            Sell(customer);
        }

        /// <summary>플레이어가 응대 위치에 있으면 플레이어 간격, 없으면 도착한 알바의 간격.</summary>
        bool TryGetSellInterval(out float interval)
        {
            if (IsPlayerServing)
            {
                interval = config.playerSellInterval;
                return true;
            }

            if (workerSeller != null && workerActionSpeed != null && servicePoint.Contains(workerSeller.position))
            {
                interval = config.workerSellInterval / Mathf.Max(0.01f, workerActionSpeed.CurrentValue);
                return true;
            }

            interval = 0f;
            return false;
        }

        void Sell(QueueMember customer)
        {
            if (!stock.TryTakeTop(out var food))
                return;

            var kind = food.Kind;
            customer.Serve(food);
            moneyPile.Add(config.PriceOf(kind), stock.transform.position + Vector3.up);
            level.AddExp(config.ExpOf(kind));
            GameStats.AddSales(1);
        }
    }
}
