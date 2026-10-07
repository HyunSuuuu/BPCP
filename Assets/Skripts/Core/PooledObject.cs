using UnityEngine;
using UnityEngine.Pool;

namespace BurgerIdle.Core
{
    /// <summary>풀에서 생성된 인스턴스가 자기 풀을 기억하기 위한 핸들. ObjectPools가 자동으로 붙인다.</summary>
    public sealed class PooledObject : MonoBehaviour
    {
        ObjectPool<Component> pool;
        Component instance;

        internal void Bind(ObjectPool<Component> pool, Component instance)
        {
            this.pool = pool;
            this.instance = instance;
        }

        public void Release() => pool.Release(instance);
    }
}
