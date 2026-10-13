using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 범용 알림 팝업 (레벨업, 해금 완료 등). 메시지가 연달아 오면 차례로 보여 준다.
    /// HUD의 알림 프리팹 루트에 붙인다. 평소에는 숨겨져 있다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class NotificationPopup : MonoBehaviour
    {
        [SerializeField] TMP_Text messageText;
        [Tooltip("메시지 하나를 보여 주는 시간 (초, 등장·퇴장 제외)")]
        [SerializeField] float holdDuration = 1.2f;

        readonly Queue<string> pending = new Queue<string>();

        CanvasGroup canvasGroup;
        bool showing;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public void Show(string message)
        {
            // 비활성으로 두었어도 동작하도록 켠다 (Awake에서 투명 상태로 초기화됨)
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            pending.Enqueue(message);
            if (!showing)
                ShowNext();
        }

        void ShowNext()
        {
            if (pending.Count == 0)
            {
                showing = false;
                return;
            }

            showing = true;
            if (messageText != null)
                messageText.text = pending.Dequeue();
            else
                pending.Dequeue();

            canvasGroup.DOKill();
            transform.DOKill();
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.6f;

            DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, 0.2f))
                .Join(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
                .AppendInterval(holdDuration)
                .Append(canvasGroup.DOFade(0f, 0.3f))
                .OnComplete(ShowNext)
                .SetLink(gameObject);
        }
    }
}
