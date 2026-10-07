using BurgerIdle.Core;
using UnityEngine;

namespace BurgerIdle.Pickup
{
    public enum PickupKind
    {
        Burger,
        Coffee,
        Trash,
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

        public PickupKind Kind => kind;
        public PickupCategory Category => kind == PickupKind.Trash ? PickupCategory.Trash : PickupCategory.Food;
        public float StackHeight => stackHeight;

        public void Release() => ObjectPools.Release(this);
    }
}
