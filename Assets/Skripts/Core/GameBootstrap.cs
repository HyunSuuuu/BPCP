using BurgerIdle.Economy;
using BurgerIdle.Money;
using BurgerIdle.Pickup;
using BurgerIdle.Player;
using BurgerIdle.Station;
using BurgerIdle.UI;
using BurgerIdle.Unlock;
using R3;
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
        [SerializeField] GameConfig config;
        [Tooltip("씬에 배치한 플레이어. 비워 두면 씬에서 찾는다.")]
        [SerializeField] PlayerController player;
        [Tooltip("씬에 배치한 바닥. 렌더러의 월드 범위가 이동 가능 범위가 된다.")]
        [SerializeField] Renderer ground;
        [Tooltip("씬에 배치한 HUD Canvas. 비워 두면 씬에서 찾는다.")]
        [SerializeField] Hud hud;
        [Tooltip("비워 두면 Camera.main을 사용한다.")]
        [SerializeField] Camera mainCamera;
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
            hud.Init(currency);

            SetupPlayer(currency);
            SetupCamera();
            SetupUnlockBoxes(currency);
            SetupStations(currency);
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

        void SetupPlayer(CurrencyManager currency)
        {
            var stats = new PlayerStats(config.playerMoveSpeed, config.playerMaxCarry).AddTo(this);

            var area = ground.bounds;
            var bounds = Rect.MinMaxRect(
                area.min.x + playerRadius, area.min.z + playerRadius,
                area.max.x - playerRadius, area.max.z - playerRadius);

            player.Init(stats, mainCamera.transform, bounds, config.playerTurnSpeed);

            GetOrAdd<CarryStack>(player.gameObject).Init(stats.MaxCarry);
            GetOrAdd<PickupInteractor>(player.gameObject).Init(config.pickupInterval);
            GetOrAdd<MoneyCollector>(player.gameObject).Init(currency, config.moneyCollectInterval);
        }

        static T GetOrAdd<T>(GameObject target) where T : Component =>
            target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();

        void SetupUnlockBoxes(CurrencyManager currency)
        {
            foreach (var box in FindAll<UnlockBox>())
                box.Init(currency, player, config);
        }

        /// <summary>해금 전이라 비활성인 오브젝트도 포함해 초기화한다.</summary>
        void SetupStations(CurrencyManager currency)
        {
            foreach (var machine in FindAll<Machine>())
                machine.Init(config.machineProducePerSecond, config.machineMaxStock);
            foreach (var counter in FindAll<Counter>())
                counter.Init(currency, player.transform, config);
            foreach (var pile in FindAll<MoneyPile>())
                pile.Init(config.moneyPrefab);
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
            joystick.Init(background, knob, radius, config.joystickRestHeight);
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
