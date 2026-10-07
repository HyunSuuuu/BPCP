using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 픽업 오브젝트를 격자로 쌓아 보관하는 더미. 기계 출력 지점, 카운터 재고, 테이블 위 쓰레기에 쓴다.
    /// 한 층(columns × rows)이 차면 위로 쌓는다. 꺼낼 때는 마지막에 놓인 것부터.
    /// </summary>
    public sealed class PickupPile : MonoBehaviour
    {
        [SerializeField] PickupCategory category = PickupCategory.Food;
        [Tooltip("최대 보관 개수 (0이면 무제한)")]
        [SerializeField] int capacity = 8;

        [Header("Layout")]
        [Tooltip("첫 번째 칸의 위치 (이 오브젝트 기준). 받침대 위에 쌓으려면 Y를 받침대 윗면 높이로")]
        [SerializeField] Vector3 origin = new Vector3(0f, 0.5f, 0f);
        [SerializeField] int columns = 1;
        [SerializeField] int rows = 1;
        [SerializeField] Vector2 spacing = new Vector2(0.5f, 0.5f);

        [Header("Tween")]
        [SerializeField] float jumpPower = 1f;
        [SerializeField] float jumpDuration = 0.25f;

        readonly List<PickupObject> items = new List<PickupObject>();
        readonly ReactiveProperty<int> count = new ReactiveProperty<int>(0);

        public PickupCategory Category => category;
        public ReadOnlyReactiveProperty<int> Count => count;
        public int Capacity => capacity;
        public bool IsFull => capacity > 0 && items.Count >= capacity;

        void Awake() => count.AddTo(this);

        void OnDrawGizmosSelected()
        {
            // 첫 칸 위치 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.TransformPoint(origin), Vector3.one * 0.2f);
        }

        /// <summary>업그레이드로 재고 상한이 바뀔 때 호출한다.</summary>
        public void SetCapacity(int value) => capacity = Mathf.Max(0, value);

        public bool CanAdd(PickupObject item) => item.Category == category && !IsFull;

        public void Add(PickupObject item)
        {
            var t = item.transform;
            t.DOKill();
            t.SetParent(transform, true);
            t.DOLocalJump(SlotPosition(items.Count, item), jumpPower, 1, jumpDuration);
            t.DOLocalRotateQuaternion(Quaternion.identity, jumpDuration);

            items.Add(item);
            count.Value = items.Count;
        }

        public bool TryTakeTop(out PickupObject item)
        {
            if (items.Count == 0)
            {
                item = null;
                return false;
            }

            item = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            count.Value = items.Count;

            item.transform.DOKill();
            item.transform.SetParent(null, true);
            return true;
        }

        Vector3 SlotPosition(int index, PickupObject item)
        {
            var perLayer = Mathf.Max(1, columns * rows);
            var layer = index / perLayer;
            var cell = index % perLayer;
            var column = cell % columns;
            var row = cell / columns;

            return origin + new Vector3(
                (column - (columns - 1) * 0.5f) * spacing.x,
                layer * item.StackHeight,
                (row - (rows - 1) * 0.5f) * spacing.y);
        }
    }
}
