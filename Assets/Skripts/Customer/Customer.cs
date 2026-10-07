using BurgerIdle.Core;
using BurgerIdle.Pickup;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Customers
{
    public enum CustomerState
    {
        Entering,
        Queueing,
        AtCounter,
        Leaving,
    }

    /// <summary>
    /// 손님. 입구 → 줄 끝 합류 → 줄을 따라 전진 → 카운터 앞에서 대기 → 음식을 받으면 퇴장.
    /// (테이블 식사는 P7에서 추가) Transform 이동만 사용한다.
    /// </summary>
    public sealed class Customer : MonoBehaviour
    {
        const float ArriveThreshold = 0.02f;
        const float TurnSpeed = 720f;

        [Tooltip("받은 음식이 놓이는 지점. 비워 두면 Food Anchor Height 높이에 자동 생성")]
        [SerializeField] Transform foodAnchor;
        [SerializeField] float foodAnchorHeight = 2.1f;

        readonly ReactiveProperty<CustomerState> state = new ReactiveProperty<CustomerState>(CustomerState.Entering);

        CustomerQueue queue;
        Vector3 exitPosition;
        float speed;
        float pathDistance;
        PickupObject food;

        public ReadOnlyReactiveProperty<CustomerState> State => state;

        void Awake()
        {
            state.AddTo(this);
            if (foodAnchor == null)
            {
                foodAnchor = new GameObject("FoodAnchor").transform;
                foodAnchor.SetParent(transform, false);
                foodAnchor.localPosition = Vector3.up * foodAnchorHeight;
            }
        }

        /// <summary>풀에서 꺼낸 직후 호출. 줄 끝으로 걸어가 합류한다.</summary>
        public void Begin(CustomerQueue queue, Vector3 exitPosition)
        {
            this.queue = queue;
            this.exitPosition = exitPosition;
            speed = queue.Config.moveSpeed;
            food = null;
            state.Value = CustomerState.Entering;
            queue.Join(this);
        }

        /// <summary>카운터에서 음식을 받는다. 줄에서 빠져 퇴장한다.</summary>
        public void Serve(PickupObject item)
        {
            food = item;
            var t = item.transform;
            t.DOKill();
            t.SetParent(foodAnchor, true);
            t.DOLocalJump(Vector3.zero, 1f, 1, 0.25f);
            t.DOLocalRotateQuaternion(Quaternion.identity, 0.25f);

            queue.Leave(this);
            state.Value = CustomerState.Leaving;
        }

        void Update()
        {
            switch (state.Value)
            {
                case CustomerState.Entering:
                    // 줄 끝(경로 마지막 점)까지 직선으로 걸어간 뒤 경로에 합류
                    var joinDistance = queue.PathLength;
                    if (MoveTowards(queue.Evaluate(joinDistance)))
                    {
                        pathDistance = joinDistance;
                        state.Value = CustomerState.Queueing;
                    }
                    break;

                case CustomerState.Queueing:
                    var slot = queue.SlotDistance(this);
                    pathDistance = Mathf.MoveTowards(pathDistance, slot, speed * Time.deltaTime);
                    transform.position = queue.Evaluate(pathDistance);
                    Face(queue.ForwardAt(pathDistance));

                    if (queue.IsFront(this) && pathDistance <= ArriveThreshold)
                        state.Value = CustomerState.AtCounter;
                    break;

                case CustomerState.Leaving:
                    if (MoveTowards(exitPosition))
                        Finish();
                    break;
            }
        }

        bool MoveTowards(Vector3 target)
        {
            var position = transform.position;
            target.y = position.y;
            var direction = target - position;
            Face(direction);
            transform.position = Vector3.MoveTowards(position, target, speed * Time.deltaTime);
            return (target - transform.position).sqrMagnitude <= ArriveThreshold * ArriveThreshold;
        }

        void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), TurnSpeed * Time.deltaTime);
        }

        void Finish()
        {
            if (food != null)
            {
                food.Release();
                food = null;
            }

            ObjectPools.Release(this);
        }
    }
}
