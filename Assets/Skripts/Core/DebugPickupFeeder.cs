using BurgerIdle.Pickup;
using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 테스트용 공급기. 일정 간격으로 프리팹을 풀에서 꺼내 PickupPile에 채운다. (쓰레기 테스트용, P7 테이블 구현 후 제거)
    /// </summary>
    [RequireComponent(typeof(PickupPile))]
    public sealed class DebugPickupFeeder : MonoBehaviour
    {
        [SerializeField] PickupObject prefab;
        [SerializeField] float interval = 0.5f;

        PickupPile pile;
        float timer;

        void Awake() => pile = GetComponent<PickupPile>();

        void Start()
        {
            if (prefab == null)
                Debug.LogWarning($"[DebugPickupFeeder] {name}: Prefab이 비어 있습니다.", this);
            else if (prefab.Category != pile.Category)
                Debug.LogWarning($"[DebugPickupFeeder] {name}: Prefab 종류({prefab.Category})와 PickupPile Category({pile.Category})가 달라 채울 수 없습니다.", this);
        }

        void Update()
        {
            if (prefab == null || !pile.CanAdd(prefab))
                return;

            timer += Time.deltaTime;
            if (timer < interval)
                return;

            timer = 0f;
            var item = ObjectPools.Get(prefab, transform.position + Vector3.up, Quaternion.identity);
            pile.Add(item);
        }
    }
}
