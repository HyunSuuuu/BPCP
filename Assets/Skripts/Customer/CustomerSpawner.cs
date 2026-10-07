using BurgerIdle.Core;
using BurgerIdle.Unlock;
using R3;
using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>
    /// 입구에서 일정 간격으로 손님을 내보낸다. 지정한 구역(Zone 0)이 모두 해금되기 전에는 동작하지 않는다.
    /// 줄이 가득 차면 입장하지 않는다.
    /// </summary>
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] Customer prefab;
        [SerializeField] CustomerQueue queue;
        [Tooltip("손님이 나타나는 위치")]
        [SerializeField] Transform entrance;
        [Tooltip("음식을 받은 손님이 걸어가 사라지는 위치")]
        [SerializeField] Transform exit;
        [Tooltip("이 구역이 모두 해금되면 손님 입장 시작. 비워 두면 바로 시작")]
        [SerializeField] UnlockZone openZone;

        bool isOpen;
        float timer;

        void Start()
        {
            if (prefab == null || queue == null || entrance == null || exit == null)
            {
                Debug.LogError($"[CustomerSpawner] {name}: Prefab, Queue, Entrance, Exit를 연결하세요.", this);
                enabled = false;
                return;
            }

            if (openZone == null)
                isOpen = true;
            else
                openZone.IsCompleted.Subscribe(completed => isOpen = completed).AddTo(this);
        }

        void Update()
        {
            if (!isOpen)
                return;

            timer += Time.deltaTime;
            if (timer < queue.Config.spawnInterval || queue.IsFull)
                return;

            timer = 0f;
            var customer = ObjectPools.Get(prefab, entrance.position, entrance.rotation);
            customer.Begin(queue, exit.position);
        }
    }
}
