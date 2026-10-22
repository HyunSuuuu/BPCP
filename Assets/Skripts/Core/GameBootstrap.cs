using System.Collections.Generic;
using BurgerIdle.Areas;
using BurgerIdle.Delivery;
using BurgerIdle.Economy;
using BurgerIdle.Money;
using BurgerIdle.Online;
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
    /// 지역(Area)마다 레벨·업그레이드(RegionState)를 만들고, 각 시설은 자기가 속한 지역의 것에 연결한다.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>해금 후 NavMesh를 다시 굽기까지 기다리는 시간 (시설 등장 트윈 0.45초 이후)</summary>
        const float NavMeshRebuildDelay = 0.6f;
        const float AgentSearchRadius = 2f;
        /// <summary>NavMesh도 바닥도 없을 때의 이동 범위 (사실상 제한 없음)</summary>
        static readonly Rect UnlimitedBounds = Rect.MinMaxRect(-100000f, -100000f, 100000f, 100000f);

        [Header("Data")]
        [Tooltip("비워 두면 코드 기본값을 쓴다")]
        [SerializeField] GameConfig config;

        [Header("Scene References")]
        [Tooltip("씬에 배치한 플레이어. 비워 두면 씬에서 찾는다.")]
        [SerializeField] PlayerController player;
        [Tooltip("NavMesh가 없을 때만 쓰는 바닥. 렌더러의 월드 범위가 이동 가능 범위가 된다. 비워도 된다.")]
        [SerializeField] Renderer ground;
        [Tooltip("씬에 배치한 HUD Canvas. 비워 두면 씬에서 찾는다.")]
        [SerializeField] Hud hud;
        [Tooltip("비워 두면 Camera.main을 사용한다.")]
        [SerializeField] Camera mainCamera;
        [Tooltip("지역 관리 (Areas 오브젝트의 AreaManager). 비워 두면 씬에서 찾는다.")]
        [SerializeField] AreaManager areas;
        [Header("Player")]
        [Tooltip("플레이어가 바닥 가장자리에서 떨어져 있어야 하는 거리")]
        [SerializeField] float playerRadius = 0.5f;

        readonly List<RegionState> regions = new List<RegionState>();
        readonly Dictionary<Area, RegionState> regionByArea = new Dictionary<Area, RegionState>();
        /// <summary>실행 중에 직접 구운 NavMesh. 에디터에서 Bake해 둔 에셋은 지역 프리팹끼리 공유되고 기준 위치가 달라 쓰지 않는다</summary>
        readonly HashSet<NavMeshSurface> builtSurfaces = new HashSet<NavMeshSurface>();
        NavMeshSurface[] surfaces;

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
            if (areas == null)
                areas = FindAnyObjectByType<AreaManager>(FindObjectsInactive.Include);

            // 비어 있는 항목을 한 번에 모두 알리도록 단락 평가(||)를 쓰지 않는다
            if (!IsInScene(player, "Player") | !IsInScene(hud, "Hud") | !IsInScene(mainCamera, "Camera")
                | !IsInScene(areas, "AreaManager(Areas)") | (ground != null && !IsInScene(ground, "Ground")))
            {
                enabled = false;
                return;
            }

            var currency = new CurrencyManager(config.startMoney).AddTo(this);
            var levelTable = config.levelTable != null ? config.levelTable : ScriptableObject.CreateInstance<LevelTable>();
            if (!CreateRegions(currency, levelTable))
            {
                enabled = false;
                return;
            }

            // 첫 지역만 남기고 숨긴 뒤에 나머지를 초기화한다 (NavMesh를 첫 지역만으로 굽기 위해)
            areas.Init(regions, currency, hud, OnEnterRegion);
            var currentLevel = areas.Current
                .Select(region => region.Level)
                .ToReadOnlyReactiveProperty()
                .AddTo(this);
            hud.Init(currency, currentLevel);
            GameStats.StartSession();
            FirebaseService.Init(hud.Notifications);
            RecordClearOnFinish();

            // 해금 전 시설과 열리지 않은 Zone을 먼저 숨긴 뒤 NavMesh를 굽고, 그 위에 플레이어를 올린다
            SetupUnlockBoxes(currency);
            SetupZones();
            SetupNavMesh();
            SetupPlayer(currency);
            SetupCamera();
            SetupStations(currency);
            SetupUpgradePads(currency);
            SetupWorkers();
            SetupMiniMaps();
            CreateJoystick();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<DebugCheats>().Init(currency, areas);
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

        /// <summary>Areas 아래 Area마다 레벨·업그레이드 묶음을 만든다.</summary>
        bool CreateRegions(CurrencyManager currency, LevelTable levelTable)
        {
            var found = areas.FindAreas();
            if (found.Length == 0)
            {
                Debug.LogError("[GameBootstrap] Areas(AreaManager) 아래에 Area가 없습니다. 지역 Area를 Areas 오브젝트의 자식으로 옮기세요.", areas);
                return false;
            }

            for (var i = 0; i < found.Length; i++)
            {
                var level = new PlayerLevel(levelTable);
                var region = new RegionState(found[i], i, level, CreateUpgrades(currency, level)).AddTo(this);
                regions.Add(region);
                regionByArea[found[i]] = region;
            }

            return true;
        }

        /// <summary>
        /// 오브젝트가 속한 지역. Area 밖에 있으면 첫 지역으로 처리한다 (지역이 하나일 때와 같은 동작).
        /// </summary>
        RegionState RegionOf(Component component)
        {
            var area = component.GetComponentInParent<Area>(true);
            if (area != null && regionByArea.TryGetValue(area, out var region))
                return region;

            if (regions.Count > 1)
                Debug.LogWarning($"[GameBootstrap] {component.name}({component.GetType().Name})이(가) Areas 아래 Area 밖에 있어 첫 지역으로 처리합니다. 지역마다 따로 동작하려면 그 지역 Area 안으로 옮기세요.", component);
            return regions[0];
        }

        /// <summary>업그레이드 스탯의 기본값은 GameConfig에서 가져온다. 지역마다 하나씩 만든다.</summary>
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
                config.adDuration, config.adCooldown);
        }

        /// <summary>플레이어가 있는 지역의 업그레이드 스탯. 지역을 옮기면 그 지역 값으로 바뀐다.</summary>
        ReadOnlyReactiveProperty<float> CurrentStat(StatKey key) =>
            areas.Current
                .Select(region => region.Upgrades.Stat(key).AsObservable())
                .Switch()
                .ToReadOnlyReactiveProperty()
                .AddTo(this);

        /// <summary>마지막 지역 클리어(데모 완료) 시 클리어 기록을 Firebase에 남긴다 (Firebase가 꺼져 있으면 무시)</summary>
        void RecordClearOnFinish()
        {
            areas.IsFinished
                .Where(finished => finished)
                .Take(1)
                .Subscribe(_ => FirebaseService.RecordClear(GameStats.PlaySeconds,
                    areas.Current.CurrentValue.Level.Level.CurrentValue, GameStats.Sales))
                .AddTo(this);
        }

        void SetupPlayer(CurrencyManager currency)
        {
            var stats = new PlayerStats(CurrentStat(StatKey.PlayerMoveSpeed), CurrentStat(StatKey.PlayerMaxCarry)).AddTo(this);

            var bounds = UnlimitedBounds;
            if (ground != null)
            {
                var area = ground.bounds;
                bounds = Rect.MinMaxRect(
                    area.min.x + playerRadius, area.min.z + playerRadius,
                    area.max.x - playerRadius, area.max.z - playerRadius);
            }

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
                if (surfaces == null || surfaces.Length == 0)
                    return null;

                // 루트에서 발끝(렌더러 아래쪽)까지의 높이. NavMesh 높이로 재면, NavMesh가 아직 등록되기 전(지역의
                // NavMeshSurface.OnEnable보다 이 Awake가 먼저 불릴 때) 0이 되어 캡슐이 바닥에 반쯤 묻힌다
                var height = 0f;
                var body = player.GetComponentInChildren<Renderer>();
                if (body != null)
                    height = Mathf.Max(0f, player.transform.position.y - body.bounds.min.y);

                agent = player.gameObject.AddComponent<NavMeshAgent>();
                agent.radius = playerRadius;
                agent.baseOffset = height;
            }

            // 알바와 서로 밀어내지 않는다 (벽·시설은 NavMesh로 계속 막힘)
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            return agent;
        }

        void SetupZones()
        {
            foreach (var zones in FindAll<ZoneManager>())
                zones.Init();
        }

        static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();

        void SetupUnlockBoxes(CurrencyManager currency)
        {
            foreach (var box in FindAll<UnlockBox>())
            {
                var region = RegionOf(box);
                box.Init(currency, player, config, region.Level, region.Area.UnlockCostMultiplier);

                // 해금 알림 문구가 있으면 알림 팝업으로 띄운다
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

        /// <summary>해금 전이라 비활성인 오브젝트(열리지 않은 지역 포함)도 포함해 초기화한다.</summary>
        void SetupStations(CurrencyManager currency)
        {
            foreach (var machine in FindAll<Machine>())
            {
                var upgrades = RegionOf(machine).Upgrades;
                machine.Init(upgrades.Stat(StatKey.MachineProducePerSecond), upgrades.Stat(StatKey.MachineMaxStock));
            }
            foreach (var counter in FindAll<Counter>())
            {
                var region = RegionOf(counter);
                counter.Init(region.Level, player.transform, config, region.Upgrades.Stat(StatKey.WorkerActionSpeed));
            }
            foreach (var packing in FindAll<PackingTable>())
                packing.Init(player.transform, config, RegionOf(packing).Upgrades.Stat(StatKey.WorkerActionSpeed));
            foreach (var table in FindAll<Table>())
                table.Init(config, RegionOf(table).Upgrades.Stat(StatKey.TableEatPerItem));
            foreach (var pile in FindAll<MoneyPile>())
                pile.Init(config.moneyPrefab, RegionOf(pile).IncomeMultiplier);
            foreach (var spawner in FindAll<DeliverySpawner>())
            {
                var region = RegionOf(spawner);
                spawner.Init(currency, region.Level, config, region.IncomeMultiplier, hud.Notifications);
            }
        }

        void SetupUpgradePads(CurrencyManager currency)
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

            // 패널은 하나를 같이 쓰고, 패드가 열 때 자기 지역의 업그레이드를 넘긴다
            var panel = panels[0];
            panel.Init(currency);
            foreach (var pad in pads)
            {
                var region = RegionOf(pad);
                pad.Init(player.transform, panel, region.Upgrades, region.Level);
            }
        }

        void SetupWorkers()
        {
            foreach (var manager in FindAll<WorkerManager>())
            {
                var upgrades = RegionOf(manager).Upgrades;
                var maxCarry = upgrades.Stat(StatKey.WorkerMaxCarry)
                    .Select(Mathf.RoundToInt)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this);
                manager.Init(upgrades.Stat(StatKey.WorkerCount), upgrades.Stat(StatKey.WorkerMoveSpeed), maxCarry, config.pickupInterval);
            }
        }

        void SetupMiniMaps()
        {
            foreach (var miniMap in FindAll<MiniMapView>())
                miniMap.Init(areas);
        }

        /// <summary>
        /// 시작 시(해금 전 시설이 숨겨진 상태) NavMesh를 굽고, 해금으로 시설이 나타날 때마다 다시 굽는다.
        /// 시설이 솟아오르는 트윈이 끝난 뒤 굽도록 잠시 기다린다. 숨겨진 지역은 처음 들어갈 때 굽는다.
        /// </summary>
        void SetupNavMesh()
        {
            surfaces = FindAll<NavMeshSurface>();
            if (surfaces.Length == 0)
                return;

            // 플레이어는 아직 NavMeshAgent가 없어서 첫 굽기에 캡슐이 장애물로 들어가므로 직접 뺀다
            NavMeshIgnore.Mark(player.gameObject);

            RebuildNavMesh();
            foreach (var box in FindAll<UnlockBox>())
            {
                box.IsUnlocked
                    .Where(unlocked => unlocked)
                    .Take(1)
                    .Subscribe(_ => DOVirtual.DelayedCall(NavMeshRebuildDelay, RebuildNavMesh).SetLink(gameObject))
                    .AddTo(this);
            }
        }

        /// <summary>
        /// 켜져 있는 NavMesh를 다시 굽는다.
        /// 이미 데이터가 있으면 그 자리에서 갱신한다 (통째로 다시 만들면 위에 서 있던 플레이어·알바가 NavMesh에서 떨어짐)
        /// </summary>
        void RebuildNavMesh()
        {
            foreach (var surface in surfaces)
            {
                if (!IsLive(surface))
                    continue;
                if (builtSurfaces.Contains(surface))
                    surface.UpdateNavMesh(surface.navMeshData);
                else
                    Build(surface);
            }
        }

        /// <summary>
        /// 이 지역 위치 기준으로 새 NavMesh 데이터를 굽는다. 에디터에서 Bake한 에셋이 연결돼 있어도 바꿔 끼운다
        /// (그 에셋은 원본 Area 위치(0,0,0) 기준이라, 다른 위치의 지역에서 UpdateNavMesh하면 범위가 어긋나 NavMesh가 비어 버림).
        /// </summary>
        void Build(NavMeshSurface surface)
        {
            surface.BuildNavMesh();
            builtSurfaces.Add(surface);
        }

        /// <summary>
        /// 다른 지역으로 옮긴 직후 (도착 지역은 이미 켜져 있음). 들고 있던 물건을 버리고, NavMesh를 준비하고, 시작 위치로 순간이동한다.
        /// </summary>
        void OnEnterRegion(RegionState region)
        {
            if (player.TryGetComponent<CarryStack>(out var stack))
                stack.Clear();

            if (surfaces != null)
            {
                foreach (var surface in surfaces)
                {
                    if (!IsLive(surface))
                        continue;

                    // 지역 안 NavMesh는 한 번 구우면 지역을 다시 켤 때 그대로 올라온다. 처음 들어가는 지역만 굽는다.
                    // 지역 밖(씬 전체) NavMesh는 새 지역 바닥이 들어가도록 다시 굽는다.
                    var inRegion = surface.transform.IsChildOf(region.Area.transform);
                    if (!inRegion || !builtSurfaces.Contains(surface))
                        Build(surface);
                }
            }

            // 지역이 다시 켜질 때 NavMesh보다 먼저 켜진 알바는 NavMesh 위에 올라가지 못하므로 다시 올린다
            foreach (var agent in region.Area.GetComponentsInChildren<NavMeshAgent>())
            {
                if (agent.isActiveAndEnabled && !agent.isOnNavMesh
                    && NavMesh.SamplePosition(agent.transform.position, out var hit, AgentSearchRadius, NavMesh.AllAreas))
                    agent.Warp(hit.position);
            }

            player.Teleport(region.Area.GetStartPosition());
            if (mainCamera.TryGetComponent<CameraFollow>(out var follow))
                follow.Snap();
        }

        /// <summary>
        /// 켜져 있는 NavMesh인지. isActiveAndEnabled는 OnEnable이 불리기 전이면 false라서(시작 직후 Awake 순서에 따라) 쓰지 않는다.
        /// OnEnable 전에 구운 데이터는 OnEnable에서 NavMesh에 등록된다.
        /// </summary>
        static bool IsLive(NavMeshSurface surface) =>
            surface != null && surface.enabled && surface.gameObject.activeInHierarchy;

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
