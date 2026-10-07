using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 프리팹별 오브젝트 풀. 음식·쓰레기·돈·손님·이펙트가 사용한다.
    /// 반납 시 진행 중인 트윈을 정리하고 크기·회전을 프리팹 값으로 되돌린다.
    /// </summary>
    public static class ObjectPools
    {
        static readonly Dictionary<Component, ObjectPool<Component>> pools = new Dictionary<Component, ObjectPool<Component>>();
        static Transform root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pools.Clear();
            root = null;
        }

        static Transform Root
        {
            get
            {
                if (root == null)
                    root = new GameObject("[Pools]").transform;
                return root;
            }
        }

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = CreatePool(prefab);
                pools.Add(prefab, pool);
            }

            var instance = (T)pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public static void Release(Component instance)
        {
            if (instance.TryGetComponent<PooledObject>(out var handle))
                handle.Release();
            else
                Object.Destroy(instance.gameObject);
        }

        static ObjectPool<Component> CreatePool(Component prefab)
        {
            var prefabScale = prefab.transform.localScale;
            ObjectPool<Component> pool = null;
            pool = new ObjectPool<Component>(
                createFunc: () =>
                {
                    var instance = Object.Instantiate(prefab, Root);
                    instance.name = prefab.name;
                    instance.gameObject.AddComponent<PooledObject>().Bind(pool, instance);
                    return instance;
                },
                actionOnGet: instance =>
                {
                    instance.transform.SetParent(null, false);
                    instance.gameObject.SetActive(true);
                },
                actionOnRelease: instance =>
                {
                    var t = instance.transform;
                    t.DOKill();
                    t.SetParent(Root, false);
                    t.localScale = prefabScale;
                    t.localRotation = Quaternion.identity;
                    instance.gameObject.SetActive(false);
                },
                actionOnDestroy: instance => Object.Destroy(instance.gameObject));
            return pool;
        }
    }
}
