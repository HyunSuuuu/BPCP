using BurgerIdle.Core;
using UnityEngine;

namespace BurgerIdle.Money
{
    /// <summary>
    /// 돈 오브젝트. 생성 시점의 금액을 갖는다. 돈 프리팹 루트에 붙인다.
    /// </summary>
    public sealed class MoneyObject : MonoBehaviour
    {
        public int Amount { get; set; }

        public void Release() => ObjectPools.Release(this);
    }
}
