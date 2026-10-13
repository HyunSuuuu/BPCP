using BurgerIdle.Core;
using UnityEngine;

namespace BurgerIdle.Pickup
{
    public enum PickupKind
    {
        Burger,
        Coffee,
        Trash,
        /// <summary>포장 테이블에서 버거 여러 개로 만든 포장 버거 (드라이브스루 판매)</summary>
        PackedBurger,
    }

    /// <summary>음식과 쓰레기는 동시에 들 수 없다. 적재 종류 판정에 쓴다.</summary>
    public enum PickupCategory
    {
        None,
        Food,
        Trash,
    }

    /// <summary>
    /// 플레이어·알바가 집을 수 있는 오브젝트. 음식·쓰레기 프리팹 루트에 붙인다.
    /// </summary>
    public sealed class PickupObject : MonoBehaviour
    {
        [SerializeField] PickupKind kind;
        [Tooltip("스택에 쌓일 때 한 칸의 높이")]
        [SerializeField] float stackHeight = 0.3f;

        Vector3 baseScale;

        public PickupKind Kind => kind;
        public PickupCategory Category => kind == PickupKind.Trash ? PickupCategory.Trash : PickupCategory.Food;
        public float StackHeight => stackHeight;

        void Awake()
        {
            baseScale = transform.localScale;
            NavMeshIgnore.Mark(gameObject);
        }

        /// <summary>
        /// 부모를 바꾸고 프리팹 크기로 되돌린다. 위치는 유지한다.
        /// 부모가 해금 연출 등으로 커지는 중일 때 크기가 부모 배율만큼 뻥튀기되는 것을 막는다.
        /// </summary>
        public void AttachTo(Transform parent)
        {
            transform.SetParent(parent, true);
            transform.localScale = baseScale;
        }

        public void Release() => ObjectPools.Release(this);
    }
}
