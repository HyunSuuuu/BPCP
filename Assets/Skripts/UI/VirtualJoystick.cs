using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 플로팅 조이스틱. 터치한 위치에 나타나고, 값은 On-Screen Control로 가상 Gamepad 왼쪽 스틱에 보낸다.
    /// 이 컴포넌트가 붙은 RectTransform 전체가 터치 영역이다.
    /// hideWhenIdle이면 손을 떼고 있는 동안 숨기고, 아니면 화면 하단 중앙에 대기 표시한다.
    /// </summary>
    public sealed class VirtualJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField] string stickControlPath = "<Gamepad>/leftStick";

        RectTransform area;
        RectTransform background;
        RectTransform knob;
        float radius;
        float restHeight;
        bool hideWhenIdle;
        Vector2 origin;
        bool dragging;

        protected override string controlPathInternal
        {
            get => stickControlPath;
            set => stickControlPath = value;
        }

        public void Init(RectTransform background, RectTransform knob, float radius, float restHeight, bool hideWhenIdle)
        {
            area = (RectTransform)transform;
            this.background = background;
            this.knob = knob;
            this.radius = radius;
            this.restHeight = restHeight;
            this.hideWhenIdle = hideWhenIdle;

            background.gameObject.SetActive(!hideWhenIdle);
        }

        void LateUpdate()
        {
            // 대기 표시 모드에서는 화면 하단 중앙에 둔다. 해상도가 바뀌어도 따라가도록 매 프레임 갱신.
            if (!dragging && !hideWhenIdle && area != null)
                background.anchoredPosition = new Vector2(0f, -area.rect.height * 0.5f + restHeight);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!TryGetLocalPoint(eventData, out origin))
                return;

            dragging = true;
            background.gameObject.SetActive(true);
            background.anchoredPosition = origin;
            knob.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || !TryGetLocalPoint(eventData, out var point))
                return;

            var delta = Vector2.ClampMagnitude(point - origin, radius);
            knob.anchoredPosition = delta;
            SendValueToControl(delta / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            knob.anchoredPosition = Vector2.zero;
            SendValueToControl(Vector2.zero);

            if (hideWhenIdle)
                background.gameObject.SetActive(false);
        }

        bool TryGetLocalPoint(PointerEventData eventData, out Vector2 point) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out point);
    }
}
