using System;
using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Customers;
using BurgerIdle.Money;
using BurgerIdle.Pickup;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 손님 테이블 (2인 합석). 카운터 맨 앞 손님이 빈 좌석을 예약해야 판매가 진행된다.
    /// 앉은 손님은 음식을 테이블 가운데(Food Pile)에 모아 함께 먹는다.
    /// 버거 1개당 시간마다 더미에서 1개씩 줄어들고(먹는 연출), 더미가 비면 식사가 끝난다.
    /// 식사 시간 = 버거 수 × 개당 시간이며, 식사 중에 합석하면 그 손님의 버거만큼 늘어난다.
    /// 식사가 끝나면 먹은 버거 수만큼 쓰레기가 Trash Pile에 쌓이고, 앉아 있던 손님마다 팁을 남기고 함께 퇴장한다.
    /// 청소 후 예약·착석한 손님이 누적 MustCleanCount명이 되면 강제 청소 상태가 되어,
    /// 손님 0명·쓰레기 0개가 될 때까지 새 예약을 받지 않는다. (GDD 6.9)
    /// 쓰레기는 Trash Pile에 붙인 PickupSource로 집어 간다.
    /// </summary>
    public sealed class Table : MonoBehaviour
    {
        [Serializable]
        public sealed class Seat
        {
            [Tooltip("손님이 서서(앉아서) 식사하는 위치. 이 Transform의 정면이 손님이 바라보는 방향")]
            public Transform sitPoint;
        }

        static readonly List<Table> active = new List<Table>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        [Header("Seats")]
        [SerializeField] Seat[] seats = new Seat[2];

        [Header("Piles")]
        [Tooltip("테이블 가운데 공유 음식 더미 (Category Food, Capacity 0). PickupSource·PickupReceiver를 붙이지 않는다")]
        [SerializeField] PickupPile foodPile;
        [Tooltip("테이블 가운데 쓰레기 더미 (Category Trash, Capacity 0). 같은 오브젝트에 InteractionZone + PickupSource를 붙인다")]
        [SerializeField] PickupPile trashPile;
        [Tooltip("식사 후 팁이 쌓이는 돈 더미")]
        [SerializeField] MoneyPile tipPile;

        readonly ReactiveProperty<int> seatedSinceClean = new ReactiveProperty<int>(0);
        readonly List<Customer> leaving = new List<Customer>();

        Customer[] occupants;
        bool[] seated;
        GameConfig config;
        ReadOnlyReactiveProperty<float> eatPerItem;
        bool mealActive;
        float biteTimer;
        int eatenCount;

        /// <summary>해금되어 활성화된 테이블 (알바의 청소 대상 탐색용)</summary>
        public static IReadOnlyList<Table> Active => active;

        public ReadOnlyReactiveProperty<int> SeatedSinceClean => seatedSinceClean;
        public ReadOnlyReactiveProperty<int> TrashCount => trashPile.Count;
        public bool IsMustClean => config != null && seatedSinceClean.Value >= config.tableMustCleanCount;
        /// <summary>쓰레기 더미의 PickupSource (알바가 쓰레기를 집는 대상)</summary>
        public IPickupProvider TrashProvider { get; private set; }

        void Awake()
        {
            seatedSinceClean.AddTo(this);
            occupants = new Customer[seats.Length];
            seated = new bool[seats.Length];
            if (trashPile != null)
                TrashProvider = trashPile.GetComponent<PickupSource>();

            if (trashPile != null)
                trashPile.Count.Subscribe(_ => TryReset()).AddTo(this);
        }

        void OnEnable() => active.Add(this);

        void OnDisable() => active.Remove(this);

        /// <summary>eatPerItem: 버거 1개를 먹는 시간 (업그레이드 스탯, 모든 테이블 공통)</summary>
        public void Init(GameConfig config, ReadOnlyReactiveProperty<float> eatPerItem)
        {
            this.config = config;
            this.eatPerItem = eatPerItem;

            if (foodPile == null || trashPile == null)
                Debug.LogError($"[Table] {name}: Food Pile과 Trash Pile을 연결하세요.", this);
            foreach (var seat in seats)
            {
                if (seat == null || seat.sitPoint == null)
                    Debug.LogError($"[Table] {name}: 좌석의 Sit Point를 연결하세요.", this);
            }
        }

        /// <summary>입장 가능한 테이블 중 from에서 가장 가까운 테이블의 빈 좌석을 예약한다.</summary>
        public static bool TryReserveNearest(Customer customer, Vector3 from, out Table table, out int seat)
        {
            table = null;
            seat = -1;
            var bestDistance = float.MaxValue;

            foreach (var candidate in active)
            {
                var free = candidate.FindFreeSeat();
                if (free < 0)
                    continue;

                var distance = (candidate.seats[free].sitPoint.position - from).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                table = candidate;
                seat = free;
            }

            if (table == null)
                return false;

            table.occupants[seat] = customer;
            table.seatedSinceClean.Value++;
            return true;
        }

        public Vector3 SitPosition(int seat) => seats[seat].sitPoint.position;
        public Quaternion SitRotation(int seat) => seats[seat].sitPoint.rotation;

        /// <summary>
        /// 손님이 좌석에 도착해 음식을 테이블 가운데에 내려놓는다.
        /// 식사 중이 아니면 식사를 시작하고, 식사 중이면 더미가 늘어난 만큼 식사가 길어진다.
        /// </summary>
        public void Sit(int seat, List<PickupObject> foods)
        {
            seated[seat] = true;
            foreach (var food in foods)
                foodPile.Add(food);

            if (mealActive)
                return;

            mealActive = true;
            biteTimer = 0f;
        }

        /// <summary>식사 중에는 버거당 시간마다 음식 더미에서 1개씩 먹어 줄이고, 더미가 비면 식사가 끝난다.</summary>
        void Update()
        {
            if (!mealActive)
                return;

            biteTimer += Time.deltaTime;
            if (biteTimer < eatPerItem.CurrentValue)
                return;

            biteTimer = 0f;
            if (foodPile.TryTakeTop(out var food))
            {
                eatenCount++;
                food.transform.DOScale(0f, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(food.Release);
            }

            if (foodPile.Count.CurrentValue == 0)
                EndMeal();
        }

        /// <summary>
        /// 식사 종료: 먹은 버거 수만큼 쓰레기 → 앉아 있던 손님마다 팁 → 함께 퇴장.
        /// 예약만 하고 아직 도착하지 않은 손님은 남는다 (도착하면 새 식사 시작).
        /// </summary>
        void EndMeal()
        {
            mealActive = false;

            SpawnTrash(eatenCount);
            eatenCount = 0;

            leaving.Clear();
            for (var i = 0; i < seats.Length; i++)
            {
                if (!seated[i])
                    continue;

                var customer = occupants[i];
                if (tipPile != null && customer != null)
                    tipPile.Add(Mathf.RoundToInt(config.tipAmount * customer.TipMultiplier), seats[i].sitPoint.position);

                leaving.Add(customer);
                occupants[i] = null;
                seated[i] = false;
            }

            foreach (var customer in leaving)
                customer.FinishMeal();
            leaving.Clear();

            TryReset();
        }

        void SpawnTrash(int count)
        {
            if (count <= 0)
                return;

            if (config.trashPrefab == null)
            {
                Debug.LogWarning($"[Table] {name}: GameConfig의 Trash Prefab이 비어 있어 쓰레기를 만들지 않습니다.", this);
                return;
            }

            var from = trashPile.transform.position + Vector3.up * 0.5f;
            for (var i = 0; i < count; i++)
                trashPile.Add(ObjectPools.Get(config.trashPrefab, from, Quaternion.identity));
        }

        /// <summary>빈 좌석 = 예약·착석한 손님이 없는 좌석. 강제 청소 중이면 없음.</summary>
        int FindFreeSeat()
        {
            if (config == null || IsMustClean)
                return -1;

            for (var i = 0; i < seats.Length; i++)
            {
                if (occupants[i] == null)
                    return i;
            }

            return -1;
        }

        /// <summary>손님 0명 && 쓰레기 0개면 누적 착석 수를 초기화해 다시 입장 가능하게 한다.</summary>
        void TryReset()
        {
            if (occupants == null || trashPile == null || trashPile.Count.CurrentValue > 0)
                return;

            foreach (var occupant in occupants)
            {
                if (occupant != null)
                    return;
            }

            seatedSinceClean.Value = 0;
        }

        void OnDrawGizmosSelected()
        {
            if (seats == null)
                return;

            Gizmos.color = Color.cyan;
            foreach (var seat in seats)
            {
                if (seat?.sitPoint == null)
                    continue;

                var p = seat.sitPoint.position + Vector3.up * 0.1f;
                Gizmos.DrawWireSphere(p, 0.3f);
                Gizmos.DrawLine(p, p + seat.sitPoint.forward * 0.6f);
            }
        }
    }
}
