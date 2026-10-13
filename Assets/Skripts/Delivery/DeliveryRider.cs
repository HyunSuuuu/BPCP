using System;
using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Pickup;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Delivery
{
    /// <summary>
    /// 배달원. 사람 입구 안쪽 점 → 배달 지점으로 걸어가 주문을 시작한다 (말풍선 표시, 제한 시간 시작).
    /// 제출받은 물건은 머리 위에 쌓인다. 수량을 다 채우면 성공, 시간이 다 되면 실패(받은 물건은 사라짐).
    /// 끝나면 사람 출구로 퇴장하고 풀에 반납된다. 보상 지급은 스포너가 한다.
    /// 배달원 프리팹 루트(발 위치 Y 0)에 붙인다.
    /// </summary>
    public sealed class DeliveryRider : MonoBehaviour
    {
        const float TurnSpeed = 720f;

        enum Phase
        {
            Entering,
            Waiting,
            Leaving,
        }

        [Header("Carry")]
        [Tooltip("제출받은 물건이 쌓이는 지점. 비워 두면 Item Anchor Height 높이에 자동 생성")]
        [SerializeField] Transform itemAnchor;
        [SerializeField] float itemAnchorHeight = 2.1f;

        readonly RouteWalker walker = new RouteWalker();
        readonly List<PickupObject> items = new List<PickupObject>();
        readonly ReactiveProperty<bool> isWaiting = new ReactiveProperty<bool>(false);
        readonly ReactiveProperty<int> submitted = new ReactiveProperty<int>(0);
        readonly ReactiveProperty<int> required = new ReactiveProperty<int>(1);
        readonly ReactiveProperty<float> timeLeft = new ReactiveProperty<float>(0f);

        DeliveryConfig config;
        DeliveryPoint point;
        Vector3[] exitRoute;
        Action onArrived;
        Action<bool, int> onFinished;
        Phase phase;

        /// <summary>배달 지점에서 주문 중 (말풍선 표시)</summary>
        public ReadOnlyReactiveProperty<bool> IsWaiting => isWaiting;
        public ReadOnlyReactiveProperty<int> Submitted => submitted;
        public ReadOnlyReactiveProperty<int> Required => required;
        public ReadOnlyReactiveProperty<float> TimeLeft => timeLeft;
        public DeliveryConfig Config => config;
        public PickupKind OrderKind => config != null ? config.orderKind : PickupKind.Burger;
        public int Remaining => Mathf.Max(0, required.Value - submitted.Value);
        public bool IsUrgent => phase == Phase.Waiting && timeLeft.Value <= config.urgentTime;

        void Awake()
        {
            NavMeshIgnore.Mark(gameObject);
            isWaiting.AddTo(this);
            submitted.AddTo(this);
            required.AddTo(this);
            timeLeft.AddTo(this);

            if (itemAnchor == null)
            {
                itemAnchor = new GameObject("ItemAnchor").transform;
                itemAnchor.SetParent(transform, false);
                itemAnchor.localPosition = Vector3.up * itemAnchorHeight;
            }
        }

        /// <summary>
        /// 풀에서 꺼내 입구 바깥 점에 놓은 직후 호출한다.
        /// onArrived: 배달 지점에 도착해 주문을 시작할 때 / onFinished(성공 여부, 수량): 주문이 끝날 때.
        /// </summary>
        public void Begin(DeliveryConfig config, DeliveryPoint point, Vector3 entranceInside, Vector3[] exitRoute,
            Action onArrived, Action<bool, int> onFinished)
        {
            this.config = config;
            this.point = point;
            this.exitRoute = exitRoute;
            this.onArrived = onArrived;
            this.onFinished = onFinished;

            items.Clear();
            submitted.Value = 0;
            required.Value = Mathf.Max(1, UnityEngine.Random.Range(config.quantityRange.x, config.quantityRange.y + 1));
            timeLeft.Value = config.timeLimit;
            isWaiting.Value = false;

            walker.Start(entranceInside, point.StandPosition);
            phase = Phase.Entering;
        }

        public bool Accepts(PickupObject item) => phase == Phase.Waiting && item.Kind == config.orderKind && Remaining > 0;

        /// <summary>제출받은 물건을 머리 위에 쌓는다. 수량을 다 채우면 성공.</summary>
        public void Submit(PickupObject item)
        {
            var height = 0f;
            foreach (var held in items)
                height += held.StackHeight;

            var t = item.transform;
            t.DOKill();
            item.AttachTo(itemAnchor);
            t.DOLocalJump(Vector3.up * height, 1f, 1, 0.25f);
            t.DOLocalRotateQuaternion(Quaternion.identity, 0.25f);

            items.Add(item);
            submitted.Value++;
            if (Remaining == 0)
                Finish(true);
        }

        void Update()
        {
            if (config == null)
                return;

            switch (phase)
            {
                case Phase.Entering:
                    if (!walker.Step(transform, config.moveSpeed, TurnSpeed))
                        break;
                    transform.DORotateQuaternion(point.StandRotation, 0.2f);
                    phase = Phase.Waiting;
                    isWaiting.Value = true;
                    point.Open();
                    onArrived?.Invoke();
                    break;

                case Phase.Waiting:
                    timeLeft.Value = Mathf.Max(0f, timeLeft.Value - Time.deltaTime);
                    if (timeLeft.Value <= 0f)
                        Finish(false);
                    break;

                case Phase.Leaving:
                    if (walker.Step(transform, config.moveSpeed, TurnSpeed))
                        Despawn();
                    break;
            }
        }

        void Finish(bool success)
        {
            if (phase != Phase.Waiting)
                return;

            phase = Phase.Leaving;
            isWaiting.Value = false;
            point.Release(this);

            // 실패하면 제출받은 음식은 사라진다
            if (!success)
                ReleaseItems();

            walker.Start(exitRoute);
            onFinished?.Invoke(success, required.Value);
        }

        void ReleaseItems()
        {
            foreach (var held in items)
                held.Release();
            items.Clear();
        }

        void Despawn()
        {
            ReleaseItems();
            config = null;
            ObjectPools.Release(this);
        }
    }
}
