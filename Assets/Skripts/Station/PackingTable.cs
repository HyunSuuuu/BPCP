using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Pickup;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 포장 테이블. 투입 칸에 버거가 Burgers Per Pack개 이상 있고 출력 칸에 자리가 있으면,
    /// 포장 위치에 사람이 서 있는 동안 진행도가 차고 다 차면 버거를 소모해 포장 버거 1개를 출력 칸에 쌓는다.
    /// 플레이어가 우선이고, 플레이어가 없으면 배정된 알바가 포장한다 (간격: 플레이어 고정, 알바는 기본 간격 ÷ 행동 속도).
    /// 자리를 비우면 진행도는 유지한 채 멈춘다.
    ///
    ///   PackingTable (이 컴포넌트)
    ///   ├─ Input      ← PickupPile + InteractionZone + PickupReceiver (버거 투입 칸)
    ///   ├─ Output     ← PickupPile + InteractionZone + PickupSource (포장 버거 출력 칸)
    ///   ├─ PackPoint  ← InteractionZone (포장하는 사람이 서는 위치)
    ///   └─ Gauge      ← World Space Canvas + Filled Image (선택)
    /// </summary>
    public sealed class PackingTable : MonoBehaviour, ISupplyTarget
    {
        static readonly List<PackingTable> active = new List<PackingTable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        [Header("Links")]
        [Tooltip("버거 투입 칸 (PickupPile + PickupReceiver). 버거만 받도록 자동 설정")]
        [SerializeField] PickupPile input;
        [Tooltip("포장 버거 출력 칸 (PickupPile + PickupSource). 포장 버거만 받도록 자동 설정")]
        [SerializeField] PickupPile output;
        [Tooltip("포장하는 사람이 서는 위치")]
        [SerializeField] InteractionZone packPoint;

        [Header("Capacity")]
        [Tooltip("버거 투입 칸 상한")]
        [Min(1)] [SerializeField] int inputCapacity = 8;
        [Tooltip("포장 버거 출력 칸 상한. 가득 차면 포장하지 않는다")]
        [Min(1)] [SerializeField] int outputCapacity = 4;

        [Header("Gauge (선택)")]
        [Tooltip("진행도 표시 묶음. 포장 중일 때만 보인다")]
        [SerializeField] GameObject gaugeRoot;
        [Tooltip("Image Type = Filled. 진행도가 fillAmount가 된다")]
        [SerializeField] Image gaugeFill;

        Transform player;
        GameConfig config;
        ReadOnlyReactiveProperty<float> workerActionSpeed;
        Transform workerPacker;
        IPickupReceiver receiver;
        float progress;

        /// <summary>해금되어 활성화된 포장 테이블 (알바의 포장 대상 탐색용)</summary>
        public static IReadOnlyList<PackingTable> Active => active;

        public InteractionZone PackPoint => packPoint;
        public bool IsPlayerPacking => player != null && packPoint.Contains(player.position);

        /// <summary>포장 재료와 출력 자리가 있다</summary>
        public bool CanPack => config != null && input.Count.CurrentValue >= config.burgersPerPack && !output.IsFull;

        /// <summary>지금 포장할 수 있는데 플레이어가 포장 위치에 없다 → 알바가 맡을 수 있음</summary>
        public bool NeedsPacker => CanPack && !IsPlayerPacking;

        // ISupplyTarget: 출력 칸을 채우는 데 필요한 버거 중 투입 칸에 아직 없는 개수
        public PickupKind Kind => PickupKind.Burger;
        public int Shortage
        {
            get
            {
                if (config == null || output.IsFull)
                    return 0;
                var needed = config.burgersPerPack * (outputCapacity - output.Count.CurrentValue) - input.Count.CurrentValue;
                return Mathf.Clamp(needed, 0, inputCapacity - input.Count.CurrentValue);
            }
        }
        public IPickupReceiver Receiver => receiver ??= SupplyTargets.FindReceiver(this, input);

        void OnEnable()
        {
            active.Add(this);
            SupplyTargets.Register(this);
        }

        void OnDisable()
        {
            active.Remove(this);
            SupplyTargets.Unregister(this);
        }

        public void Init(Transform player, GameConfig config, ReadOnlyReactiveProperty<float> workerActionSpeed)
        {
            this.player = player;
            this.workerActionSpeed = workerActionSpeed;

            if (input == null || output == null || packPoint == null)
            {
                Debug.LogError($"[PackingTable] {name}: Input, Output, Pack Point를 연결하세요.", this);
                enabled = false;
                return;
            }

            if (config.packedBurgerPrefab == null || config.packedBurgerPrefab.Kind != PickupKind.PackedBurger)
            {
                Debug.LogError($"[PackingTable] {name}: GameConfig의 Packed Burger Prefab을 연결하세요 (Kind = Packed Burger).", this);
                enabled = false;
                return;
            }

            this.config = config;
            input.SetAcceptedKinds(PickupKind.Burger);
            input.SetCapacity(inputCapacity);
            output.SetAcceptedKinds(PickupKind.PackedBurger);
            output.SetCapacity(outputCapacity);
            ShowGauge(false);
        }

        /// <summary>알바가 포장을 맡는다. 포장 위치에 도착해야 실제로 진행된다.</summary>
        public void AssignWorker(Transform worker) => workerPacker = worker;

        public void ReleaseWorker(Transform worker)
        {
            if (workerPacker == worker)
                workerPacker = null;
        }

        void Update()
        {
            if (config == null)
                return;

            if (!CanPack || !TryGetPackInterval(out var interval))
            {
                ShowGauge(false);
                return;
            }

            progress += Time.deltaTime;
            ShowGauge(true);
            if (gaugeFill != null)
                gaugeFill.fillAmount = Mathf.Clamp01(progress / interval);

            if (progress < interval)
                return;

            progress = 0f;
            Pack();
        }

        bool TryGetPackInterval(out float interval)
        {
            if (IsPlayerPacking)
            {
                interval = config.playerPackInterval;
                return true;
            }

            if (workerPacker != null && workerActionSpeed != null && packPoint.Contains(workerPacker.position))
            {
                interval = config.workerPackInterval / Mathf.Max(0.01f, workerActionSpeed.CurrentValue);
                return true;
            }

            interval = 0f;
            return false;
        }

        void Pack()
        {
            if (!input.TryConsume(config.burgersPerPack))
                return;

            var packed = ObjectPools.Get(config.packedBurgerPrefab, packPoint.Center + Vector3.up, Quaternion.identity);
            output.Add(packed);
        }

        void ShowGauge(bool visible)
        {
            if (gaugeRoot != null && gaugeRoot.activeSelf != visible)
                gaugeRoot.SetActive(visible);
        }
    }
}
