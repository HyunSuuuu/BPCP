using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Pickup;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

namespace BurgerIdle.Customers
{
    public enum QueueMemberState
    {
        Entering,
        Queueing,
        AtCounter,
        /// <summary>주문을 모두 받은 뒤 퇴장 전까지의 개별 행동 (손님: 테이블 이동·식사)</summary>
        Served,
        Leaving,
    }

    /// <summary>
    /// 줄을 서서 음식을 받는 대상의 공통 동작. 손님(Customer)과 드라이브스루 차량(P11)이 상속한다.
    /// 입구 안쪽 점 → 줄 끝 합류 → 줄을 따라 전진 → 맨 앞에서 주문 수량만큼 수령 → 출구로 퇴장.
    /// 주문 수량은 RollOrderCount, 수령 후 행동은 OnServed·UpdateServed를 재정의해 바꾼다. 기본은 1개, 바로 퇴장.
    /// </summary>
    public abstract class QueueMember : MonoBehaviour
    {
        const float ArriveThreshold = 0.02f;
        protected const float TurnSpeed = 720f;

        [Header("Carry")]
        [Tooltip("받은 음식이 쌓이는 지점. 비워 두면 Item Anchor Height 높이에 자동 생성")]
        [FormerlySerializedAs("foodAnchor")]
        [SerializeField] Transform itemAnchor;
        [FormerlySerializedAs("foodAnchorHeight")]
        [SerializeField] float itemAnchorHeight = 2.1f;

        readonly ReactiveProperty<QueueMemberState> state = new ReactiveProperty<QueueMemberState>(QueueMemberState.Entering);
        readonly ReactiveProperty<int> orderCount = new ReactiveProperty<int>(1);
        readonly ReactiveProperty<int> receivedCount = new ReactiveProperty<int>(0);
        readonly RouteWalker walker = new RouteWalker();
        readonly List<PickupObject> items = new List<PickupObject>();

        CustomerQueue queue;
        Vector3[] exitRoute;
        float speed;
        float pathDistance;

        public ReadOnlyReactiveProperty<QueueMemberState> State => state;
        public ReadOnlyReactiveProperty<int> OrderCount => orderCount;
        public ReadOnlyReactiveProperty<int> ReceivedCount => receivedCount;
        /// <summary>아직 받지 못한 주문 수량</summary>
        public int RemainingOrder => Mathf.Max(0, orderCount.Value - receivedCount.Value);
        protected float Speed => speed;
        protected RouteWalker Walker => walker;

        protected virtual void Awake()
        {
            NavMeshIgnore.Mark(gameObject);
            state.AddTo(this);
            orderCount.AddTo(this);
            receivedCount.AddTo(this);
            if (itemAnchor == null)
            {
                itemAnchor = new GameObject("ItemAnchor").transform;
                itemAnchor.SetParent(transform, false);
                itemAnchor.localPosition = Vector3.up * itemAnchorHeight;
            }
        }

        /// <summary>
        /// 풀에서 꺼내 입구 바깥 점에 놓은 직후 호출한다.
        /// 입구 안쪽 점을 지나 줄 끝에 합류하고, 퇴장할 때는 exitRoute를 따라간다.
        /// </summary>
        public void Begin(CustomerQueue queue, Vector3 entranceInside, Vector3[] exitRoute)
        {
            this.queue = queue;
            this.exitRoute = exitRoute;
            speed = queue.Config.moveSpeed;
            items.Clear();
            receivedCount.Value = 0;
            orderCount.Value = Mathf.Max(1, RollOrderCount());

            walker.Start(entranceInside, queue.Evaluate(queue.PathLength));
            state.Value = QueueMemberState.Entering;
            queue.Join(this);
            OnBegin();
        }

        /// <summary>
        /// 카운터가 판매 직전에 호출한다. false면 판매하지 않고 계속 기다린다.
        /// (손님: 빈 테이블 좌석 예약에 성공해야 true)
        /// </summary>
        public virtual bool TryPrepareForService() => true;

        /// <summary>
        /// 카운터에서 음식 1개를 받아 머리 위에 쌓는다.
        /// 주문 수량을 다 받으면 줄에서 빠진 뒤 OnServed를 호출한다.
        /// </summary>
        public void Serve(PickupObject food)
        {
            var height = 0f;
            foreach (var held in items)
                height += held.StackHeight;

            var t = food.transform;
            t.DOKill();
            food.AttachTo(itemAnchor);
            t.DOLocalJump(Vector3.up * height, 1f, 1, 0.25f);
            t.DOLocalRotateQuaternion(Quaternion.identity, 0.25f);

            items.Add(food);
            receivedCount.Value++;
            if (receivedCount.Value < orderCount.Value)
                return;

            queue.Leave(this);
            state.Value = QueueMemberState.Served;
            OnServed();
        }

        /// <summary>Begin할 때 주문 수량을 정한다. 기본 1개.</summary>
        protected virtual int RollOrderCount() => 1;

        /// <summary>풀에서 꺼내 Begin할 때 호출. 상속 클래스의 상태 초기화용.</summary>
        protected virtual void OnBegin() { }

        /// <summary>주문을 모두 받은 직후의 행동. 기본은 바로 퇴장.</summary>
        protected virtual void OnServed() => Leave();

        /// <summary>Served 상태에서 매 프레임 호출. 끝나면 Leave()를 호출한다.</summary>
        protected virtual void UpdateServed() { }

        /// <summary>들고 있는 음식을 모두 넘겨준다 (테이블에 내려놓기 등). 이후 퇴장 시 반납하지 않는다.</summary>
        protected List<PickupObject> TakeItems()
        {
            var taken = new List<PickupObject>(items);
            items.Clear();
            return taken;
        }

        /// <summary>출구 경로(안쪽 점 → 바깥 점)를 따라 퇴장한다. 바깥 점에 도착하면 풀에 반납된다.</summary>
        protected void Leave()
        {
            walker.Start(exitRoute);
            state.Value = QueueMemberState.Leaving;
        }

        protected virtual void Update()
        {
            switch (state.Value)
            {
                case QueueMemberState.Entering:
                    if (walker.Step(transform, speed, TurnSpeed))
                    {
                        pathDistance = queue.PathLength;
                        state.Value = QueueMemberState.Queueing;
                    }
                    break;

                case QueueMemberState.Queueing:
                    var slot = queue.SlotDistance(this);
                    pathDistance = Mathf.MoveTowards(pathDistance, slot, speed * Time.deltaTime);
                    transform.position = queue.Evaluate(pathDistance);
                    RouteWalker.Face(transform, queue.ForwardAt(pathDistance), TurnSpeed);

                    if (queue.IsFront(this) && pathDistance <= ArriveThreshold)
                        state.Value = QueueMemberState.AtCounter;
                    break;

                case QueueMemberState.Served:
                    UpdateServed();
                    break;

                case QueueMemberState.Leaving:
                    if (walker.Step(transform, speed, TurnSpeed))
                        Finish();
                    break;
            }
        }

        void Finish()
        {
            foreach (var held in items)
                held.Release();
            items.Clear();

            ObjectPools.Release(this);
        }
    }
}
