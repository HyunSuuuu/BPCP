using BurgerIdle.Core;
using UnityEngine;

namespace BurgerIdle.Money
{
    /// <summary>
    /// 돈 오브젝트. 생성 시점의 금액을 갖는다. 돈 프리팹 루트에 붙인다.
    /// </summary>
    public sealed class MoneyObject : MonoBehaviour
    {
        Vector3 baseScale;

        public int Amount { get; set; }

        void Awake()
        {
            baseScale = transform.localScale;
            NavMeshIgnore.Mark(gameObject);
        }

        /// <summary>부모를 바꾸고 프리팹 크기로 되돌린다. 위치는 유지한다. (PickupObject.AttachTo와 같은 이유)</summary>
        public void AttachTo(Transform parent)
        {
            transform.SetParent(parent, true);
            transform.localScale = baseScale;
        }

        public void Release() => ObjectPools.Release(this);
    }
}
