using System;
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
        [Serializable]
        public sealed class Variant
        {
            public QueueMember prefab;
            [Tooltip("등장 가중치. 예: 일반 9, VIP 1 → VIP 10%")]
            [Min(0f)] public float weight = 1f;
        }

        [Header("Spawn")]
        [Tooltip("Variants가 비어 있을 때 사용하는 기본 프리팹")]
        [SerializeField] QueueMember prefab;
        [Tooltip("가중치에 따라 무작위로 고르는 프리팹 목록 (일반·VIP 손님). 비우면 Prefab만 사용")]
        [SerializeField] Variant[] variants;

        [Header("Route")]
        [SerializeField] CustomerQueue queue;
        [SerializeField] Area area;
        [Tooltip("손님은 People Entrance, 차량은 Vehicle Entrance")]
        [SerializeField] GateType entranceType = GateType.PeopleEntrance;
        [Tooltip("손님은 People Exit, 차량은 Vehicle Exit")]
        [SerializeField] GateType exitType = GateType.PeopleExit;

        [Header("Open Condition")]
        [Tooltip("이 시설 묶음(UnlockZone)이 모두 해금되면 입장 시작. 보통 같은 Zone의 Facilities.\n" +
                 "비워 두면 이 스포너가 속한 Zone의 Facilities, 그것도 없으면 바로 시작")]
        [SerializeField] UnlockZone openZone;

        bool isOpen;
        float timer;

        void Start()
        {
            if (PickPrefab() == null || queue == null || area == null)
            {
                Debug.LogError($"[CustomerSpawner] {name}: Prefab(또는 Variants), Queue, Area를 연결하세요.", this);
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
                openZone = GetComponentInParent<Zone>(true)?.Facilities;

            if (openZone == null)
            {
                Debug.LogWarning($"[CustomerSpawner] {name}: Open Zone이 없어 바로 입장을 시작합니다.", this);
                isOpen = true;
            }
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

            var member = ObjectPools.Get(PickPrefab(), entrance.OutsidePoint, entrance.EnterRotation);
            member.Begin(queue, entrance.InsidePoint, exit.ExitRoute);
        }

        QueueMember PickPrefab()
        {
            if (variants == null || variants.Length == 0)
                return prefab;

            var total = 0f;
            foreach (var variant in variants)
            {
                if (variant.prefab != null)
                    total += variant.weight;
            }

            if (total <= 0f)
                return prefab;

            var roll = UnityEngine.Random.Range(0f, total);
            foreach (var variant in variants)
            {
                if (variant.prefab == null)
                    continue;
                roll -= variant.weight;
                if (roll <= 0f)
                    return variant.prefab;
            }

            return prefab;
        }
    }
}
