using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 캐릭터 머리 위의 픽업 오브젝트 스택. 음식과 쓰레기는 섞지 않는다.
    /// 이동하면 위쪽일수록 늦게 따라오며 기울어진다.
    /// </summary>
    public sealed class CarryStack : MonoBehaviour
    {
        [Header("Anchor")]
        [Tooltip("스택이 시작되는 머리 위 지점. 비워 두면 Anchor Height 높이에 자동 생성")]
        [SerializeField] Transform anchor;
        [SerializeField] float anchorHeight = 2.1f;

        [Header("Tween")]
        [SerializeField] float jumpPower = 1f;
        [SerializeField] float jumpDuration = 0.25f;

        [Header("Sway")]
        [Tooltip("이동 속도 대비 기울어지는 정도")]
        [SerializeField] float swayAmount = 0.03f;
        [SerializeField] float maxLag = 0.3f;
        [SerializeField] float swayStiffness = 150f;
        [SerializeField] float swayDamping = 14f;

        readonly List<PickupObject> items = new List<PickupObject>();
        readonly HashSet<PickupObject> settling = new HashSet<PickupObject>();
        readonly ReactiveProperty<int> count = new ReactiveProperty<int>(0);
        readonly ReactiveProperty<PickupCategory> category = new ReactiveProperty<PickupCategory>(PickupCategory.None);

        ReadOnlyReactiveProperty<int> capacity;
        Vector3 lastPosition;
        Vector3 lag;
        Vector3 lagVelocity;

        public ReadOnlyReactiveProperty<int> Count => count;
        public ReadOnlyReactiveProperty<PickupCategory> Category => category;
        public bool IsFull => capacity == null || items.Count >= capacity.CurrentValue;

        /// <summary>최대적재량 스탯을 연결한다. 업그레이드로 값이 바뀌면 바로 반영된다.</summary>
        public void Init(ReadOnlyReactiveProperty<int> capacity) => this.capacity = capacity;

        void Awake()
        {
            count.AddTo(this);
            category.AddTo(this);

            if (anchor == null)
            {
                anchor = new GameObject("CarryAnchor").transform;
                anchor.SetParent(transform, false);
                anchor.localPosition = Vector3.up * anchorHeight;
            }

            lastPosition = transform.position;
        }

        public bool CanAccept(PickupCategory itemCategory) =>
            !IsFull && (items.Count == 0 || itemCategory == category.Value);

        public PickupObject Peek() => items.Count > 0 ? items[items.Count - 1] : null;

        /// <summary>쌓인 물건 (0 = 맨 아래)</summary>
        public IReadOnlyList<PickupObject> Items => items;

        /// <summary>
        /// 위에서부터 조건에 맞는 첫 물건. 받는 곳이 맨 위 물건을 못 받아도 그 아래의 받을 수 있는 물건을 고를 때 쓴다.
        /// </summary>
        public PickupObject FindFromTop(System.Predicate<PickupObject> match)
        {
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (match(items[i]))
                    return items[i];
            }

            return null;
        }

        /// <summary>중간에 있는 물건을 꺼낸다. 위에 있던 물건은 LateUpdate에서 아래로 내려앉는다.</summary>
        public PickupObject Take(PickupObject item)
        {
            if (!items.Remove(item))
                return null;

            settling.Remove(item);
            UpdateState();

            item.transform.DOKill();
            item.AttachTo(null);
            return item;
        }

        public void Push(PickupObject item)
        {
            var t = item.transform;
            t.DOKill();
            item.AttachTo(anchor);

            var index = items.Count;
            items.Add(item);
            settling.Add(item);
            UpdateState();

            t.DOLocalRotateQuaternion(Quaternion.identity, jumpDuration);
            t.DOLocalJump(SlotPosition(index), jumpPower, 1, jumpDuration)
                .OnComplete(() => settling.Remove(item));
        }

        public PickupObject Pop()
        {
            if (items.Count == 0)
                return null;

            var item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            settling.Remove(item);
            UpdateState();

            item.transform.DOKill();
            item.AttachTo(null);
            return item;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            // 이동 속도 반대 방향으로 끌려가는 스프링
            var velocity = (transform.position - lastPosition) / dt;
            velocity.y = 0f;
            lastPosition = transform.position;

            var target = Vector3.ClampMagnitude(-velocity * swayAmount, maxLag);
            lagVelocity += (target - lag) * (swayStiffness * dt);
            lagVelocity *= Mathf.Exp(-swayDamping * dt);
            lag += lagVelocity * dt;

            var localLag = Quaternion.Inverse(anchor.rotation) * lag;
            var height = 0f;
            foreach (var item in items)
            {
                if (!settling.Contains(item))
                    item.transform.localPosition = new Vector3(0f, height, 0f) + localLag * (height * height);
                height += item.StackHeight;
            }
        }

        Vector3 SlotPosition(int index)
        {
            var height = 0f;
            for (var i = 0; i < index; i++)
                height += items[i].StackHeight;
            return new Vector3(0f, height, 0f);
        }

        void UpdateState()
        {
            count.Value = items.Count;
            category.Value = items.Count > 0 ? items[0].Category : PickupCategory.None;
        }
    }
}
