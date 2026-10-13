using BurgerIdle.Customers;
using R3;
using TMPro;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 줄 맨 앞(카운터 앞) 손님의 주문 말풍선. "받은 수 / 주문 수"를 표시하고 카메라를 향한다.
    /// 손님 프리팹 안의 World Space Canvas에 붙인다. VIP는 프리팹에서 말풍선 색을 다르게 한다.
    /// </summary>
    public sealed class OrderBubble : MonoBehaviour
    {
        [Tooltip("켜고 끌 말풍선 루트 (보통 이 오브젝트의 자식 패널)")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text countText;

        Transform cameraTransform;

        void Awake()
        {
            var member = GetComponentInParent<QueueMember>();
            if (member == null || root == null)
            {
                Debug.LogError($"[OrderBubble] {name}: 손님(QueueMember) 프리팹 안에 두고 Root를 연결하세요.", this);
                enabled = false;
                return;
            }

            root.SetActive(false);

            Observable.CombineLatest(member.State, member.ReceivedCount, member.OrderCount,
                    (state, received, order) => (state, received, order))
                .Subscribe(x =>
                {
                    root.SetActive(x.state == QueueMemberState.AtCounter && x.received < x.order);
                    if (countText != null)
                        countText.text = $"{x.received}/{x.order}";
                })
                .AddTo(this);
        }

        void LateUpdate()
        {
            if (!root.activeSelf)
                return;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform != null)
                transform.rotation = cameraTransform.rotation;
        }
    }
}
