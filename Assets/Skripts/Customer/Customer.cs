using BurgerIdle.Station;
using DG.Tweening;
using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>
    /// 매장 손님. 줄 서기·음식 수령·퇴장은 QueueMember가 처리한다.
    /// 주문 수량은 Order Range 안에서 무작위. 카운터 맨 앞에서 빈 테이블 좌석을 예약해야 판매가 진행되고,
    /// 주문을 모두 받으면 좌석으로 걸어가 음식을 테이블 가운데에 내려놓는다.
    /// 식사는 테이블이 진행하고, 끝나면 테이블이 FinishMeal을 호출해 함께 앉은 손님이 함께 퇴장한다.
    /// 일반 손님과 VIP 손님은 같은 컴포넌트를 쓰고 프리팹 설정(색·주문 수량·팁 배율)만 다르다.
    /// </summary>
    public sealed class Customer : QueueMember
    {
        enum Phase
        {
            None,
            ToTable,
            Eating,
        }

        [Header("Order")]
        [Tooltip("주문 수량 범위 (최소, 최대). 일반 1~4, VIP 6~10")]
        [SerializeField] Vector2Int orderRange = new Vector2Int(1, 4);
        [Tooltip("팁 배율. 일반 1, VIP 3")]
        [SerializeField] float tipMultiplier = 1f;

        Table table;
        int seat = -1;
        Phase phase;

        public float TipMultiplier => tipMultiplier;

        protected override int RollOrderCount() => Random.Range(orderRange.x, orderRange.y + 1);

        protected override void OnBegin()
        {
            table = null;
            seat = -1;
            phase = Phase.None;
        }

        public override bool TryPrepareForService() =>
            table != null || Table.TryReserveNearest(this, transform.position, out table, out seat);

        protected override void OnServed()
        {
            if (table == null)
            {
                Leave();
                return;
            }

            Walker.Start(table.SitPosition(seat));
            phase = Phase.ToTable;
        }

        protected override void UpdateServed()
        {
            if (phase != Phase.ToTable || !Walker.Step(transform, Speed, TurnSpeed))
                return;

            transform.DORotateQuaternion(table.SitRotation(seat), 0.2f);
            phase = Phase.Eating;
            table.Sit(seat, TakeItems());
        }

        /// <summary>테이블의 식사가 끝나면 테이블이 호출한다. 출구로 퇴장한다.</summary>
        public void FinishMeal()
        {
            table = null;
            seat = -1;
            phase = Phase.None;
            Leave();
        }
    }
}
