using System.Collections.Generic;
using R3;
using UnityEngine;

namespace BurgerIdle.Workers
{
    /// <summary>
    /// 알바 고용. 인원수 스탯(업그레이드 "고용")이 오르면 직원 대기 지점에서 알바를 새로 만든다.
    /// 씬의 빈 오브젝트에 붙인다.
    /// </summary>
    public sealed class WorkerManager : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("알바 프리팹 (NavMeshAgent + CarryStack + Worker)")]
        [SerializeField] Worker prefab;
        [Tooltip("고용한 알바가 나타나는 직원 대기 지점 (NavMesh 위)")]
        [SerializeField] Transform spawnPoint;

        readonly List<Worker> workers = new List<Worker>();

        ReadOnlyReactiveProperty<float> moveSpeed;
        ReadOnlyReactiveProperty<int> maxCarry;
        float actionInterval;

        public IReadOnlyList<Worker> Workers => workers;

        public void Init(ReadOnlyReactiveProperty<float> count, ReadOnlyReactiveProperty<float> moveSpeed,
            ReadOnlyReactiveProperty<int> maxCarry, float actionInterval)
        {
            if (prefab == null || spawnPoint == null)
            {
                Debug.LogError($"[WorkerManager] {name}: Prefab과 Spawn Point를 연결하세요.", this);
                return;
            }

            this.moveSpeed = moveSpeed;
            this.maxCarry = maxCarry;
            this.actionInterval = actionInterval;

            count.Subscribe(value => SpawnUpTo(Mathf.RoundToInt(value))).AddTo(this);
        }

        void SpawnUpTo(int target)
        {
            while (workers.Count < target)
            {
                var worker = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
                worker.name = $"{prefab.name}_{workers.Count + 1}";
                worker.Init(moveSpeed, maxCarry, actionInterval);
                workers.Add(worker);
            }
        }
    }
}
