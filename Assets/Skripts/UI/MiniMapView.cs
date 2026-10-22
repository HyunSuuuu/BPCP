using System;
using System.Collections.Generic;
using BurgerIdle.Areas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 미니맵. 우하단 나침반 버튼으로 열고, 여러 매장(지역)을 아이콘으로 보여 준다. 플레이어는 표시하지 않는다.
    /// - 드래그로 지도를 움직인다 (ScrollRect)
    /// - 매장을 누르면 매장 UI(입장·수거)가 뜨고, 매장 UI 밖을 누르거나 드래그하면 사라진다
    /// - 열려 있는 동안 게임을 멈춘다. X 버튼·뒤로가기(PC는 Esc)로 닫는다
    /// - 열고 닫을 때와 입장할 때 화면이 페이드된다
    /// HUD Canvas 안의 미니맵 오브젝트(항상 켜 둠)에 붙이고, 실제 화면은 자식 Window에 둔다.
    /// </summary>
    public sealed class MiniMapView : MonoBehaviour
    {
        [Header("Open / Close")]
        [Tooltip("HUD 우하단 나침반 버튼. 누르면 미니맵이 열린다")]
        [SerializeField] Button openButton;
        [Tooltip("미니맵 구석의 X 버튼")]
        [SerializeField] Button closeButton;
        [Tooltip("열고 닫을 미니맵 화면 (이 오브젝트의 자식, 전체 화면을 덮고 Raycast를 막는 배경 포함)")]
        [SerializeField] GameObject window;
        [Tooltip("열고 닫을 때 페이드 (선택. 비우면 바로 전환)")]
        [SerializeField] ScreenFader fader;

        [Header("Map")]
        [Tooltip("드래그로 움직일 지도 (선택). 열 때 현재 지역이 가운데 오도록 Content를 옮긴다")]
        [SerializeField] ScrollRect scrollRect;
        [Tooltip("매장 아이콘이 생성될 부모 (보통 ScrollRect의 Content, Anchor·Pivot 가운데). 아이콘 위치 = Area의 Map Position")]
        [SerializeField] RectTransform content;
        [Tooltip("매장 아이콘 프리팹 (MiniMapStoreIcon)")]
        [SerializeField] MiniMapStoreIcon iconPrefab;

        [Header("Store UI")]
        [Tooltip("매장을 눌렀을 때 뜨는 매장 UI (Window 안, Content 밖에 둔다)")]
        [SerializeField] MiniMapStorePopup popup;
        [Tooltip("켜면 매장 UI를 누른 아이콘 위치에 띄운다. 끄면 배치한 자리에 그대로 뜬다")]
        [SerializeField] bool followIcon = true;
        [Tooltip("아이콘 위치에서 매장 UI를 옮길 거리 (UI 좌표)")]
        [SerializeField] Vector2 popupOffset = new Vector2(0f, 180f);

        readonly List<MiniMapStoreIcon> icons = new List<MiniMapStoreIcon>();

        AreaManager areas;
        Canvas canvas;
        float pausedTimeScale = 1f;
        bool isOpen;

        public void Init(AreaManager areas)
        {
            if (window == null || content == null || iconPrefab == null || popup == null)
            {
                Debug.LogError($"[MiniMapView] {name}: Window, Content, Icon Prefab, Popup을 연결하세요.", this);
                enabled = false;
                return;
            }

            this.areas = areas;
            canvas = GetComponentInParent<Canvas>();
            window.SetActive(false);

            if (openButton != null)
                openButton.onClick.AddListener(Open);
            else
                Debug.LogWarning($"[MiniMapView] {name}: Open Button(나침반 버튼)이 없어 미니맵을 열 수 없습니다.", this);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            popup.Init(areas, Enter);

            foreach (var region in areas.Regions)
            {
                var icon = Instantiate(iconPrefab, content);
                icon.name = $"{iconPrefab.name}_{region.Index + 1}";
                icon.Rect.anchoredPosition = region.Area.MapPosition;
                icon.Bind(region, areas.Current, ShowPopup);
                icons.Add(icon);
            }
        }

        public void Open()
        {
            if (!enabled || isOpen || IsFading)
                return;

            isOpen = true;
            Transition(() =>
            {
                window.SetActive(true);
                popup.Hide();
                FocusCurrent();
                Pause();
            });
        }

        public void Close()
        {
            if (!isOpen || IsFading)
                return;

            isOpen = false;
            Transition(() =>
            {
                window.SetActive(false);
                popup.Hide();
                Resume();
            });
        }

        /// <summary>미니맵을 닫고 그 지역으로 이동한다. 지금 있는 지역이면 닫기만 한다.</summary>
        void Enter(RegionState region)
        {
            if (!isOpen || IsFading)
                return;

            isOpen = false;
            Transition(() =>
            {
                window.SetActive(false);
                popup.Hide();
                Resume();
                areas.Enter(region);
            });
        }

        void Update()
        {
            if (!isOpen || IsFading)
                return;

            // Android 뒤로가기 키는 Input System에서 Esc로 들어온다
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            // 매장 UI 밖을 누르거나 드래그를 시작하면 매장 UI를 닫는다 (다른 매장을 누르면 손을 뗄 때 다시 열림)
            if (popup.IsShown && PressedOutside(popup.Rect))
                popup.Hide();
        }

        void ShowPopup(MiniMapStoreIcon icon)
        {
            popup.Show(icon.Region);
            if (!followIcon)
                return;

            popup.Rect.position = icon.Rect.position;
            popup.Rect.anchoredPosition += popupOffset;
        }

        /// <summary>열 때 현재 지역 아이콘이 화면 가운데 오도록 지도를 옮긴다.</summary>
        void FocusCurrent()
        {
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            var current = areas.Current.CurrentValue;
            foreach (var icon in icons)
            {
                if (icon.Region == current)
                {
                    content.anchoredPosition = -icon.Rect.anchoredPosition;
                    return;
                }
            }
        }

        bool PressedOutside(RectTransform rect)
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return false;

            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return !RectTransformUtility.RectangleContainsScreenPoint(rect, pointer.position.ReadValue(), eventCamera);
        }

        bool IsFading => fader != null && fader.IsBusy;

        void Transition(Action atBlack)
        {
            if (fader != null)
                fader.Play(atBlack);
            else
                atBlack();
        }

        void Pause()
        {
            pausedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        void Resume() => Time.timeScale = pausedTimeScale;
    }
}
