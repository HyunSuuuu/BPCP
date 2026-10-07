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
        Leaving,
    }

    /// <summary>
    /// 줄을 서서 음식을 받는 대상의 공통 동작. 손님(Customer)과 드라이브스루 차량(P11)이 상속한다.
    /// 입구 안쪽 점 → 줄 끝 합류 → 줄을 따라 전진 → 맨 앞에서 대기 → 음식 수령 → 출구로 퇴장.
    /// 음식을 받은 뒤의 행동은 OnServed를 재정의해 바꾼다. (손님의 테이블 이동은 P7)
    /// </summary>
    public abstract class QueueMember : MonoBehaviour
    {
        const float ArriveThreshold = 0.02f;
        protected const float TurnSpeed = 720f;

        [Tooltip("받은 음식이 놓이는 지점. 비워 두면 Item Anchor Height 높이에 자동 생성")]
        [FormerlySerializedAs("foodAnchor")]
        [SerializeField] Transform itemAnchor;
        [FormerlySerializedAs("foodAnchorHeight")]
        [SerializeField] float itemAnchorHeight = 2.1f;

        readonly ReactiveProperty<QueueMemberState> state = new ReactiveProperty<QueueMemberState>(QueueMemberState.Entering);
        readonly RouteWalker walker = new RouteWalker();

        CustomerQueue queue;
        Vector3[] exitRoute;
        float speed;
        float pathDistance;
        PickupObject item;

        public ReadOnlyReactiveProperty<QueueMemberState> State => state;
        protected float Speed => speed;

        protected virtual void Awake()
        {
            state.AddTo(this);
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
            item = null;

            walker.Start(entranceInside, queue.Evaluate(queue.PathLength));
            state.Value = QueueMemberState.Entering;
            queue.Join(this);
        }

        /// <summary>카운터에서 음식을 받는다. 줄에서 빠진 뒤 OnServed를 호출한다.</summary>
        public void Serve(PickupObject food)
        {
            item = food;
            var t = food.transform;
            t.DOKill();
            t.SetParent(itemAnchor, true);
            t.DOLocalJump(Vector3.zero, 1f, 1, 0.25f);
            t.DOLocalRotateQuaternion(Quaternion.identity, 0.25f);

            queue.Leave(this);
            OnServed();
        }

        /// <summary>음식을 받은 뒤의 행동. 기본은 바로 퇴장.</summary>
        protected virtual void OnServed() => Leave();

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

                case QueueMemberState.Leaving:
                    if (walker.Step(transform, speed, TurnSpeed))
                        Finish();
                    break;
            }
        }

        void Finish()
        {
            if (item != null)
            {
                item.Release();
                item = null;
            }

            ObjectPools.Release(this);
        }
    }
}
