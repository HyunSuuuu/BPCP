using BurgerIdle.Core;
using BurgerIdle.Customers;
using BurgerIdle.Economy;
using BurgerIdle.Money;
using BurgerIdle.Pickup;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 카운터 판매. 응대 위치에 판매자가 있고, 재고가 있고, 줄 맨 앞 손님이 대기 중이면 일정 간격으로 1개씩 판매한다.
    /// 판매 수익은 카운터 돈 더미에 쌓이고(수거해야 소지금), 경험치는 바로 지급한다.
    /// (빈 테이블 좌석 조건은 P7, 알바 판매는 P9에서 추가)
    /// </summary>
    public sealed class Counter : MonoBehaviour
    {
        [Tooltip("음식 재고 (PickupReceiver로 채워지는 더미)")]
        [SerializeField] PickupPile stock;
        [Tooltip("판매자가 서는 응대 위치")]
        [SerializeField] InteractionZone servicePoint;
        [SerializeField] CustomerQueue queue;
        [SerializeField] MoneyPile moneyPile;

        CurrencyManager currency;
        Transform player;
        GameConfig config;
        QueueMember currentCustomer;
        float progress;

        public void Init(CurrencyManager currency, Transform player, GameConfig config)
        {
            this.currency = currency;
            this.player = player;
            this.config = config;

            if (stock == null || servicePoint == null || queue == null || moneyPile == null)
            {
                Debug.LogError($"[Counter] {name}: Stock, Service Point, Queue, Money Pile을 연결하세요.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (currency == null)
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

            if (!servicePoint.Contains(player.position) || stock.Count.CurrentValue == 0)
                return;

            progress += Time.deltaTime;
            if (progress < config.playerSellInterval)
                return;

            progress = 0f;
            Sell(customer);
        }

        void Sell(QueueMember customer)
        {
            if (!stock.TryTakeTop(out var food))
                return;

            customer.Serve(food);
            moneyPile.Add(config.burgerPrice, stock.transform.position + Vector3.up);
            currency.Add(CurrencyType.Exp, config.sellExp);
        }
    }
}
