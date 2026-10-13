using System.Collections.Generic;
using BurgerIdle.Economy;
using BurgerIdle.Money;
using BurgerIdle.Pickup;
using BurgerIdle.Player;
using BurgerIdle.Station;
using BurgerIdle.UI;
using BurgerIdle.Unlock;
using BurgerIdle.Upgrade;
using BurgerIdle.Workers;
using DG.Tweening;
using R3;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 씬에 배치된 오브젝트를 서로 연결한다. 오브젝트 자체는 사용자가 프리팹으로 만들어 씬에 둔다.
    /// 조이스틱 UI만 코드로 생성한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>해금 후 NavMesh를 다시 굽기까지 기다리는 시간 (시설 등장 트윈 0.45초 이후)</summary>
        const float NavMeshRebuildDelay = 0.6f;

        [Header("Data")]
        [Tooltip("비워 두면 코드 기본값을 쓴다")]
        [SerializeField] GameConfig config;

        [Header("Scene References")]
        [Tooltip("씬에 배치한 플레이어. 비워 두면 씬에서 찾는다.")]
        [SerializeField] PlayerController player;
        [Tooltip("씬에 배치한 바닥. 렌더러의 월드 범위가 이동 가능 범위가 된다.")]
        [SerializeField] Renderer ground;
        [Tooltip("씬에 배치한 HUD Canvas. 비워 두면 씬에서 찾는다.")]
        [SerializeField] Hud hud;
        [Tooltip("비워 두면 Camera.main을 사용한다.")]
        [SerializeField] Camera mainCamera;
        [Header("Player")]
        [Tooltip("플레이어가 바닥 가장자리에서 떨어져 있어야 하는 거리")]
        [SerializeField] float playerRadius = 0.5f;

        void Awake()
        {
            if (config == null)
                config = ScriptableObject.CreateInstance<GameConfig>();
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (hud == null)
                hud = FindAnyObjectByType<Hud>();

            // 비어 있는 항목을 한 번에 모두 알리도록 단락 평가(||)를 쓰지 않는다
            if (!IsInScene(player, "Player") | !IsInScene(ground, "Ground") | !IsInScene(hud, "Hud") | !IsInScene(mainCamera, "Camera"))
            {
                enabled = false;
                return;
            }

            var currency = new CurrencyManager(config.startMoney).AddTo(this);
            var levelTable = config.levelTable != null ? config.levelTable : ScriptableObject.CreateInstance<LevelTable>();
            var playerLevel = new PlayerLevel(levelTable, currency).AddTo(this);
            var upgrades = CreateUpgrades(currency, playerLevel);
            hud.Init(currency, playerLevel);

            // 해금 전 시설과 열리지 않은 Zone을 먼저 숨긴 뒤 NavMesh를 굽고, 그 위에 플레이어를 올린다
            SetupUnlockBoxes(currency, playerLevel);
            SetupZones();
            SetupNavMesh();
            SetupPlayer(currency, upgrades);
            SetupCamera();
            SetupStations(currency, upgrades);
            SetupUpgradePads(currency, playerLevel, upgrades);
            SetupWorkers(upgrades);
            CreateJoystick();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<DebugCheats>().Init(currency);
#endif
        }

        /// <summary>참조가 비었거나 프리팹 에셋(씬 밖)이면 오류를 남긴다.</summary>
        bool IsInScene(Component target, string label)
        {
            if (target == null)
            {
                Debug.LogError($"[GameBootstrap] {label}: 씬에 배치된 오브젝트를 찾지 못했습니다. 프리팹을 Hierarchy로 끌어다 놓고 연결하세요.", this);
                return false;
            }

            if (!target.gameObject.scene.IsValid())
            {
                Debug.LogError($"[GameBootstrap] {label}: 프리팹 에셋이 연결되어 있습니다. 씬(Hierarchy)에 배치한 오브젝트를 연결하세요.", this);
                return false;
            }

            return true;
        }

        /// <summary>업그레이드 스탯의 기본값은 GameConfig에서 가져온다.</summary>
        UpgradeManager CreateUpgrades(CurrencyManager currency, PlayerLevel playerLevel)
        {
            var baseValues = new Dictionary<StatKey, float>
            {
                [StatKey.PlayerMoveSpeed] = config.playerMoveSpeed,
                [StatKey.PlayerMaxCarry] = config.playerMaxCarry,
                [StatKey.PlayerIncomeMultiplier] = config.incomeMultiplierRange.x,
                [StatKey.MachineProducePerSecond] = config.machineProducePerSecond,
                [StatKey.MachineMaxStock] = config.machineMaxStock,
                [StatKey.TableEatPerItem] = config.tableEatPerItem,
                [StatKey.WorkerCount] = config.workerCountRange.x,
                [StatKey.WorkerMoveSpeed] = config.workerMoveSpeed,
                [StatKey.WorkerMaxCarry] = config.workerMaxCarry,
                [StatKey.WorkerActionSpeed] = 1f,
            };
            var ranges = new Dictionary<StatKey, (float min, float max)>
            {
                [StatKey.PlayerIncomeMultiplier] = (config.incomeMultiplierRange.x, config.incomeMultiplierRange.y),
                [StatKey.WorkerCount] = (config.workerCountRange.x, config.workerCountRange.y),
                [StatKey.WorkerActionSpeed] = (1f, config.workerActionSpeedMax),
            };

            return new UpgradeManager(config.upgrades, baseValues, ranges, currency, playerLevel,
                config.adDuration, config.adCooldown).AddTo(this);
        }

        void SetupPlayer(CurrencyManager currency, UpgradeManager upgrades)
        {
            var stats = new PlayerStats(upgrades.Stat(StatKey.PlayerMoveSpeed), upgrades.Stat(StatKey.PlayerMaxCarry)).AddTo(this);

            var area = ground.bounds;
            var bounds = Rect.MinMaxRect(
                area.min.x + playerRadius, area.min.z + playerRadius,
                area.max.x - playerRadius, area.max.z - playerRadius);

            player.Init(stats, mainCamera.transform, bounds, config.playerTurnSpeed, SetupPlayerAgent());

            GetOrAdd<CarryStack>(player.gameObject).Init(stats.MaxCarry);
            GetOrAdd<PickupInteractor>(player.gameObject).Init(config.pickupInterval);
            GetOrAdd<MoneyCollector>(player.gameObject).Init(currency, config.moneyCollectInterval);
        }

        /// <summary>
        /// NavMesh가 있으면 플레이어에 NavMeshAgent를 붙여 NavMesh 안으로만 움직이게 한다.
        /// 플레이어 루트가 바닥보다 떠 있으면(캡슐 중심이 루트인 경우) 그 높이를 Base Offset으로 맞춘다.
        /// </summary>
        NavMeshAgent SetupPlayerAgent()
        {
            if (!player.TryGetComponent<NavMeshAgent>(out var agent))
            {
                if (FindAll<NavMeshSurface>().Length == 0)
                    return null;

                var height = 0f;
                if (NavMesh.SamplePosition(player.transform.position, out var hit, 3f, NavMesh.AllAreas))
                    height = Mathf.Max(0f, player.transform.position.y - hit.position.y);

                agent = player.gameObject.AddComponent<NavMeshAgent>();
                agent.radius = playerRadius;
                agent.baseOffset = height;
            }

            return agent;
        }

        void SetupZones()
        {
            foreach (var zones in FindAll<ZoneManager>())
                zones.Init();
        }

        static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();

        void SetupUnlockBoxes(CurrencyManager currency, PlayerLevel playerLevel)
        {
            foreach (var box in FindAll<UnlockBox>())
            {
                box.Init(currency, player, config, playerLevel);

                // 해금 알림 문구가 있으면 알림 팝업으로 띄운다 (예: 데모 완료)
                var message = box.Definition != null ? box.Definition.unlockMessage : null;
                if (string.IsNullOrEmpty(message))
                    continue;
                box.IsUnlocked
                    .Where(unlocked => unlocked)
                    .Take(1)
                    .Subscribe(_ => hud.Notifications?.Show(message))
                    .AddTo(this);
            }
        }

        /// <summary>해금 전이라 비활성인 오브젝트도 포함해 초기화한다.</summary>
        void SetupStations(CurrencyManager currency, UpgradeManager upgrades)
        {
            foreach (var machine in FindAll<Machine>())
                machine.Init(upgrades.Stat(StatKey.MachineProducePerSecond), upgrades.Stat(StatKey.MachineMaxStock));
            var actionSpeed = upgrades.Stat(StatKey.WorkerActionSpeed);
            foreach (var counter in FindAll<Counter>())
                counter.Init(currency, player.transform, config, actionSpeed);
            foreach (var packing in FindAll<PackingTable>())
                packing.Init(player.transform, config, actionSpeed);
            foreach (var table in FindAll<Table>())
                table.Init(config, upgrades.Stat(StatKey.TableEatPerItem));
            var incomeMultiplier = upgrades.Stat(StatKey.PlayerIncomeMultiplier);
            foreach (var pile in FindAll<MoneyPile>())
                pile.Init(config.moneyPrefab, incomeMultiplier);
        }

        void SetupUpgradePads(CurrencyManager currency, PlayerLevel playerLevel, UpgradeManager upgrades)
        {
            var pads = FindAll<UpgradePad>();
            if (pads.Length == 0)
                return;

            var panels = FindAll<UpgradePanel>();
            if (panels.Length == 0)
            {
                Debug.LogError("[GameBootstrap] 업그레이드 패드가 있지만 씬에 UpgradePanel이 없습니다.", this);
                return;
            }

            var panel = panels[0];
            panel.Init(upgrades, currency, playerLevel);
            foreach (var pad in pads)
                pad.Init(player.transform, panel);
        }

        void SetupWorkers(UpgradeManager upgrades)
        {
            var managers = FindAll<WorkerManager>();
            if (managers.Length == 0)
                return;

            var maxCarry = upgrades.Stat(StatKey.WorkerMaxCarry)
                .Select(Mathf.RoundToInt)
                .ToReadOnlyReactiveProperty()
                .AddTo(this);
            managers[0].Init(upgrades.Stat(StatKey.WorkerCount), upgrades.Stat(StatKey.WorkerMoveSpeed), maxCarry, config.pickupInterval);
        }

        /// <summary>
        /// 시작 시(해금 전 시설이 숨겨진 상태) NavMesh를 굽고, 해금으로 시설이 나타날 때마다 다시 굽는다.
        /// 시설이 솟아오르는 트윈이 끝난 뒤 굽도록 잠시 기다린다.
        /// </summary>
        void SetupNavMesh()
        {
            var surfaces = FindAll<NavMeshSurface>();
            if (surfaces.Length == 0)
                return;

            // 플레이어는 아직 NavMeshAgent가 없어서 첫 굽기에 캡슐이 장애물로 들어가므로 직접 뺀다
            NavMeshIgnore.Mark(player.gameObject);

            // 이미 데이터가 있으면 그 자리에서 갱신한다 (통째로 다시 만들면 위에 서 있던 플레이어·알바가 NavMesh에서 떨어짐)
            void Rebuild()
            {
                foreach (var surface in surfaces)
                {
                    if (surface == null || !surface.isActiveAndEnabled)
                        continue;
                    if (surface.navMeshData != null)
                        surface.UpdateNavMesh(surface.navMeshData);
                    else
                        surface.BuildNavMesh();
                }
            }

            Rebuild();
            foreach (var box in FindAll<UnlockBox>())
            {
                box.IsUnlocked
                    .Where(unlocked => unlocked)
                    .Take(1)
                    .Subscribe(_ => DOVirtual.DelayedCall(NavMeshRebuildDelay, Rebuild).SetLink(gameObject))
                    .AddTo(this);
            }
        }

        static T[] FindAll<T>() where T : Object =>
            FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        void SetupCamera()
        {
            mainCamera.fieldOfView = config.cameraFieldOfView;
            GetOrAdd<CameraFollow>(mainCamera.gameObject).Init(player.transform, config.cameraPitch, config.cameraYaw, config.cameraDistance, config.cameraFollowSharpness);
        }

        void CreateJoystick()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasObject = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 화면 전체가 터치 영역이므로 HUD·패널 Canvas보다 아래에 둔다
            canvas.sortingOrder = -100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var area = CreateRect("JoystickArea", canvasObject.transform);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            // 투명하지만 터치는 받는다
            area.gameObject.AddComponent<Image>().color = Color.clear;

            var radius = config.joystickRadius;
            var background = CreateCircle("Background", area, radius * 2f, new Color(1f, 1f, 1f, 0.25f));
            var knob = CreateCircle("Knob", background, radius * 0.9f, new Color(1f, 1f, 1f, 0.8f));

            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Init(background, knob, radius, config.joystickRestHeight, config.joystickHideWhenIdle);
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static RectTransform CreateCircle(string name, Transform parent, float size, Color color)
        {
            var rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprites.Circle;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
