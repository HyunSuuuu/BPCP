using BurgerIdle.Areas;
using BurgerIdle.Core;
using BurgerIdle.Unlock;
using R3;
using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>
    /// Area 입구에서 일정 간격으로 줄 설 대상(손님·차량)을 내보낸다.
    /// 지정한 구역이 모두 해금되기 전에는 동작하지 않고, 줄이 가득 차면 입장하지 않는다.
    /// 차량은 출입문 종류를 차량 입구·출구로 바꿔 같은 컴포넌트를 쓴다. (P11)
    /// </summary>
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] QueueMember prefab;
        [SerializeField] CustomerQueue queue;
        [SerializeField] Area area;
        [SerializeField] GateType entranceType = GateType.PeopleEntrance;
        [SerializeField] GateType exitType = GateType.PeopleExit;
        [Tooltip("이 구역이 모두 해금되면 입장 시작. 비워 두면 바로 시작")]
        [SerializeField] UnlockZone openZone;

        bool isOpen;
        float timer;

        void Start()
        {
            if (prefab == null || queue == null || area == null)
            {
                Debug.LogError($"[CustomerSpawner] {name}: Prefab, Queue, Area를 연결하세요.", this);
                enabled = false;
                return;
            }

            if (!area.TryGetGate(entranceType, out _) || !area.TryGetGate(exitType, out _))
            {
                Debug.LogError($"[CustomerSpawner] {name}: Area에 {entranceType}·{exitType} 출입문이 없습니다. AreaGate의 Usage와 Outside·Inside 점을 확인하세요.", this);
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
            area.TryGetGate(entranceType, out var entrance);
            area.TryGetGate(exitType, out var exit);

            var member = ObjectPools.Get(prefab, entrance.OutsidePoint, entrance.EnterRotation);
            member.Begin(queue, entrance.InsidePoint, exit.ExitRoute);
        }
    }
}
