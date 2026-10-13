using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.Delivery
{
    /// <summary>
    /// 배달원 머리 위 말풍선. 주문 중에만 보이고 "SPECIAL ORDER"(프리팹 고정 텍스트), 남은 시간(분:초),
    /// 품목 아이콘, "제출 / 필요"를 표시하며 카메라를 향한다.
    /// 배달원 프리팹 안의 World Space Canvas에 붙인다.
    /// </summary>
    public sealed class DeliveryBubble : MonoBehaviour
    {
        [Tooltip("켜고 끌 말풍선 루트 (보통 이 오브젝트의 자식 패널)")]
        [SerializeField] GameObject root;
        [Tooltip("남은 시간 (분:초)")]
        [SerializeField] TMP_Text timeText;
        [Tooltip("\"제출 / 필요\"")]
        [SerializeField] TMP_Text countText;
        [Tooltip("품목 아이콘 (DeliveryConfig의 Order Icon, 선택)")]
        [SerializeField] Image icon;
        [Tooltip("남은 시간이 급할 때(Urgent Time 이하) 시간 글자 색")]
        [SerializeField] Color urgentColor = new Color(1f, 0.3f, 0.25f);

        DeliveryRider rider;
        Transform cameraTransform;
        Color timeColor = Color.white;
        int shownSeconds = -1;

        void Awake()
        {
            rider = GetComponentInParent<DeliveryRider>();
            if (rider == null || root == null)
            {
                Debug.LogError($"[DeliveryBubble] {name}: 배달원(DeliveryRider) 프리팹 안에 두고 Root를 연결하세요.", this);
                enabled = false;
                return;
            }

            if (timeText != null)
                timeColor = timeText.color;
            root.SetActive(false);

            rider.IsWaiting
                .Subscribe(waiting =>
                {
                    root.SetActive(waiting);
                    shownSeconds = -1;
                    if (waiting && icon != null && rider.Config != null && rider.Config.orderIcon != null)
                        icon.sprite = rider.Config.orderIcon;
                })
                .AddTo(this);

            Observable.CombineLatest(rider.Submitted, rider.Required, (submitted, required) => (submitted, required))
                .Subscribe(x =>
                {
                    if (countText != null)
                        countText.text = $"{x.submitted}/{x.required}";
                })
                .AddTo(this);
        }

        void LateUpdate()
        {
            if (!root.activeSelf)
                return;

            var seconds = Mathf.CeilToInt(rider.TimeLeft.CurrentValue);
            if (timeText != null && seconds != shownSeconds)
            {
                shownSeconds = seconds;
                timeText.text = $"{seconds / 60}:{seconds % 60:00}";
                timeText.color = rider.IsUrgent ? urgentColor : timeColor;
            }

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform != null)
                transform.rotation = cameraTransform.rotation;
        }
    }
}
