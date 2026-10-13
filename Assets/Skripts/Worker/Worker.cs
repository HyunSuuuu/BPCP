using BurgerIdle.Pickup;
using BurgerIdle.Station;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace BurgerIdle.Workers
{
    /// <summary>
    /// 알바. 할 일을 스스로 골라 NavMesh로 이동해 처리한다. 모든 알바가 모든 일을 한다.
    /// 우선순위(상황에 따라 바뀜): 들고 있는 것 처리 > 막힌 좌석 청소 > 판매(일반·드라이브스루, 실제로 팔 수 있을 때) >
    /// 포장 > 운반(모자란 곳이 있을 때, 많이 모자란 곳 우선) > 일반 청소. 같으면 가까운 순.
    /// 운반은 모자란 곳(ISupplyTarget: 카운터, 드라이브스루, 포장 테이블)을 먼저 정하고, 그 종류를 내주는 가장 가까운 곳에서 집는다.
    /// 플레이어처럼 지나가며 자동으로 줍지 않고, 맡은 일의 대상에서만 줍고 내려놓는다.
    /// 플레이어가 먼저 일을 해 버리면(대상이 비거나 응대·포장 위치를 차지) 일을 포기하고 다음 일로 넘어간다.
    /// 할 일이 없으면 그 자리에서 기다린다. 돈은 수거하지 않는다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(CarryStack))]
    public sealed class Worker : MonoBehaviour
    {
        const float RetargetInterval = 0.25f;
        const float StationIdleGrace = 1.5f;
        const float NavMeshSearchRadius = 2f;

        // 우선순위 (높을수록 먼저). 같은 우선순위면 가까운 쪽
        const float PriorityCleanUrgent = 4f; // 좌석이 없어 손님이 막혔거나 강제 청소 테이블
        const float PrioritySell = 3f;        // 맨 앞 손님·차량에게 실제로 팔 수 있을 때만
        const float PriorityPack = 2.5f;      // 포장 재료와 출력 자리가 있을 때만
        const float PriorityCarry = 2f;       // 받는 곳의 재고가 모자랄 때만
        const float PriorityClean = 1f;       // 그 밖의 쓰레기
        const float ShortageBonusPerItem = 5f; // 모자란 개수 1개당 거리 5만큼 우대 (최대 10개)

        enum State
        {
            Idle,
            Moving,
            Working,
        }

        NavMeshAgent agent;
        CarryStack stack;
        ReadOnlyReactiveProperty<float> moveSpeed;
        float actionInterval;

        State state;
        TaskType task;
        object reserved;
        IPickupProvider provider;
        IPickupReceiver receiver;
        Counter counter;
        PackingTable packing;
        ISupplyTarget carryTarget;
        float actionTimer;
        float idleTimer;
        float stationIdleTimer;

        public TaskType CurrentTask => task;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            stack = GetComponent<CarryStack>();
        }

        public void Init(ReadOnlyReactiveProperty<float> moveSpeed, ReadOnlyReactiveProperty<int> maxCarry, float actionInterval)
        {
            this.moveSpeed = moveSpeed;
            this.actionInterval = actionInterval;
            stack.Init(maxCarry);

            if (!TrySnapToNavMesh())
                Debug.LogWarning($"[Worker] {name}: 근처에 NavMesh가 없습니다. 회전이 0인 오브젝트에 NavMeshSurface를 붙여 굽고, 직원 대기 지점을 파란 영역 위에 두세요.", this);
        }

        /// <summary>가까운 NavMesh 위로 옮긴다. NavMesh를 다시 굽는 중이거나 아직 없으면 false.</summary>
        bool TrySnapToNavMesh()
        {
            if (agent.isOnNavMesh)
                return true;
            if (!NavMesh.SamplePosition(transform.position, out var hit, NavMeshSearchRadius, NavMesh.AllAreas))
                return false;
            return agent.Warp(hit.position);
        }

        void OnDisable() => Finish();

        void Update()
        {
            if (moveSpeed == null)
                return;

            // NavMesh 위에 없으면(아직 안 구웠거나 다시 굽는 중) 일을 멈추고 주기적으로 자리를 다시 찾는다
            if (!agent.isOnNavMesh)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer > 0f)
                    return;
                idleTimer = RetargetInterval * 4f;
                if (!TrySnapToNavMesh())
                    return;
                Finish();
            }

            agent.speed = moveSpeed.CurrentValue;

            switch (state)
            {
                case State.Idle:
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f)
                    {
                        idleTimer = RetargetInterval;
                        ChooseTask();
                    }
                    break;

                case State.Moving:
                    if (!IsTaskValid())
                    {
                        Finish();
                        break;
                    }

                    if (TargetZone.Contains(transform.position))
                    {
                        agent.ResetPath();
                        state = State.Working;
                        actionTimer = 0f;
                    }
                    break;

                case State.Working:
                    if (!IsTaskValid())
                    {
                        Finish();
                        break;
                    }

                    Work();
                    break;
            }
        }

        // ── 일 고르기 ───────────────────────────────────────

        void ChooseTask()
        {
            if (stack.Peek() != null)
            {
                // 들고 있는 것부터 처리: 음식 → 운반 목적지(또는 모자란 곳·받을 수 있는 곳), 쓰레기 → 쓰레기통
                var target = FindDeliverReceiver();
                if (target != null)
                {
                    var trash = stack.Peek().Category == PickupCategory.Trash;
                    Begin(trash ? TaskType.Dispose : TaskType.Deliver, null, null, target, null, null);
                }
                return;
            }

            carryTarget = null;
            var bestScore = float.MinValue;
            var bestTask = TaskType.None;
            IPickupProvider bestProvider = null;
            Counter bestCounter = null;
            PackingTable bestPacking = null;
            ISupplyTarget bestTarget = null;
            var position = transform.position;

            // 상황 파악: 좌석이 없어 손님이 막혀 있는지
            var seatBlocked = false;
            foreach (var c in Counter.Active)
                seatBlocked |= c.IsBlockedBySeat;

            // 판매 (일반 카운터, 드라이브스루)
            foreach (var c in Counter.Active)
            {
                if (!c.NeedsSeller || TaskBoard.IsReserved(c, this))
                    continue;
                var score = Score(PrioritySell, c.ServicePoint.Center, position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTask = TaskType.Sell;
                    bestCounter = c;
                }
            }

            // 포장
            foreach (var p in PackingTable.Active)
            {
                if (!p.NeedsPacker || TaskBoard.IsReserved(p, this))
                    continue;
                var score = Score(PriorityPack, p.PackPoint.Center, position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTask = TaskType.Pack;
                    bestPacking = p;
                }
            }

            // 운반: 모자란 곳마다 그 종류를 내주는 가장 가까운 곳을 찾는다 (재고가 충분하면 운반하지 않음)
            foreach (var target in SupplyTargets.Active)
            {
                var shortage = target.Shortage;
                if (shortage <= 0 || target.Receiver == null)
                    continue;

                var source = FindNearestProvider(target.Kind, position);
                if (source == null)
                    continue;

                var score = Score(PriorityCarry, source.Zone.Center, position) + Mathf.Min(shortage, 10) * ShortageBonusPerItem;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTask = TaskType.Carry;
                    bestProvider = source;
                    bestTarget = target;
                }
            }

            // 청소
            foreach (var table in Table.Active)
            {
                var p = table.TrashProvider;
                if (p == null || !p.HasItem || TaskBoard.IsReserved(p, this))
                    continue;
                // 좌석이 막혔거나 강제 청소 중이면 가장 급하다
                var urgent = seatBlocked || table.IsMustClean;
                var score = Score(urgent ? PriorityCleanUrgent : PriorityClean, p.Zone.Center, position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTask = TaskType.Clean;
                    bestProvider = p;
                }
            }

            switch (bestTask)
            {
                case TaskType.Sell:
                    Begin(TaskType.Sell, bestCounter, null, null, bestCounter, null);
                    break;
                case TaskType.Pack:
                    Begin(TaskType.Pack, bestPacking, null, null, null, bestPacking);
                    break;
                case TaskType.Carry:
                    carryTarget = bestTarget;
                    Begin(TaskType.Carry, bestProvider, bestProvider, null, null, null);
                    break;
                case TaskType.Clean:
                    Begin(TaskType.Clean, bestProvider, bestProvider, null, null, null);
                    break;
            }
        }

        /// <summary>우선순위가 거리보다 항상 앞선다. 같은 우선순위면 가까운 쪽.</summary>
        static float Score(float priority, Vector3 target, Vector3 from) =>
            priority * 1000f - Vector3.Distance(from, target);

        IPickupProvider FindNearestProvider(PickupKind kind, Vector3 from)
        {
            IPickupProvider best = null;
            var bestDistance = float.MaxValue;
            foreach (var p in InteractionRegistry.Providers)
            {
                if (!p.HasItem || !p.HasKind(kind) || TaskBoard.IsReserved(p, this))
                    continue;
                var distance = (p.Zone.Center - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = p;
                }
            }

            return best;
        }

        /// <summary>
        /// 들고 있는 물건을 내려놓을 곳. 운반을 시작할 때 정한 목적지 → 모자란 곳 중 가까운 곳 → 받을 수 있는 가장 가까운 곳 순.
        /// </summary>
        IPickupReceiver FindDeliverReceiver()
        {
            if (carryTarget != null && carryTarget.Receiver != null && CanDropAny(carryTarget.Receiver))
                return carryTarget.Receiver;
            carryTarget = null;

            var position = transform.position;
            IPickupReceiver best = null;
            var bestDistance = float.MaxValue;
            foreach (var target in SupplyTargets.Active)
            {
                var r = target.Receiver;
                if (r == null || target.Shortage <= 0 || !CanDropAny(r))
                    continue;
                var distance = (r.Zone.Center - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = r;
                }
            }

            if (best != null)
                return best;

            foreach (var r in InteractionRegistry.Receivers)
            {
                if (!CanDropAny(r))
                    continue;
                var distance = (r.Zone.Center - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = r;
                }
            }

            return best;
        }

        bool CanDropAny(IPickupReceiver r) => stack.FindFromTop(r.CanReceive) != null;

        void Begin(TaskType newTask, object reserveTarget, IPickupProvider newProvider, IPickupReceiver newReceiver,
            Counter newCounter, PackingTable newPacking)
        {
            if (reserveTarget != null && !TaskBoard.TryReserve(reserveTarget, this))
                return;

            task = newTask;
            reserved = reserveTarget;
            provider = newProvider;
            receiver = newReceiver;
            counter = newCounter;
            packing = newPacking;
            stationIdleTimer = 0f;

            if (counter != null)
                counter.AssignWorker(transform);
            if (packing != null)
                packing.AssignWorker(transform);

            state = State.Moving;
            agent.SetDestination(TargetZone.Center);
        }

        InteractionZone TargetZone => task switch
        {
            TaskType.Carry or TaskType.Clean => provider.Zone,
            TaskType.Deliver or TaskType.Dispose => receiver.Zone,
            TaskType.Sell => counter.ServicePoint,
            TaskType.Pack => packing.PackPoint,
            _ => null,
        };

        // ── 유효성 검사 (플레이어 가로채기 포함) ─────────────

        bool IsTaskValid()
        {
            switch (task)
            {
                case TaskType.Carry:
                case TaskType.Clean:
                    // 이동 중에 대상이 비면(플레이어가 가져감) 포기. 작업 중에는 Work에서 끝을 판단
                    return state == State.Working || provider.HasItem;

                case TaskType.Deliver:
                case TaskType.Dispose:
                    return CanDropAny(receiver);

                case TaskType.Sell:
                    if (counter == null || !counter.isActiveAndEnabled || counter.IsPlayerServing)
                        return false;
                    // 손님이 잠깐 없을 수 있으므로 판매할 일이 없는 상태가 일정 시간 이어질 때만 포기
                    return StationStillNeeded(counter.NeedsSeller);

                case TaskType.Pack:
                    if (packing == null || !packing.isActiveAndEnabled || packing.IsPlayerPacking)
                        return false;
                    return StationStillNeeded(packing.NeedsPacker);

                default:
                    return false;
            }
        }

        bool StationStillNeeded(bool needed)
        {
            stationIdleTimer = needed ? 0f : stationIdleTimer + Time.deltaTime;
            return stationIdleTimer < StationIdleGrace;
        }

        // ── 작업 ────────────────────────────────────────────

        void Work()
        {
            if (task == TaskType.Sell || task == TaskType.Pack)
                return; // 판매·포장은 카운터·포장 테이블이 진행한다

            actionTimer += Time.deltaTime;
            if (actionTimer < actionInterval)
                return;
            actionTimer = 0f;

            switch (task)
            {
                case TaskType.Carry:
                case TaskType.Clean:
                    if (stack.CanAccept(provider.Category) && provider.TryTake(out var item))
                        stack.Push(item);
                    else
                        Finish(); // 가득 찼거나 대상이 비었음 → 다음 선택에서 들고 있는 것을 처리
                    break;

                case TaskType.Deliver:
                case TaskType.Dispose:
                    var drop = stack.FindFromTop(receiver.CanReceive);
                    if (drop != null)
                        receiver.Receive(stack.Take(drop));
                    else
                        Finish();
                    break;
            }
        }

        /// <summary>예약을 풀고 대기 상태로. 바로 다음 일을 고른다. (운반 목적지는 들고 있는 동안 유지)</summary>
        void Finish()
        {
            TaskBoard.Release(reserved, this);
            if (counter != null)
                counter.ReleaseWorker(transform);
            if (packing != null)
                packing.ReleaseWorker(transform);

            task = TaskType.None;
            reserved = null;
            provider = null;
            receiver = null;
            counter = null;
            packing = null;
            state = State.Idle;
            idleTimer = 0f;

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.ResetPath();
        }
    }
}
