using System;
using DG.Tweening;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 화면 전체 페이드. 어두워졌다가(페이드 아웃) 그 사이에 화면을 바꾸고 다시 밝아진다(페이드 인).
    /// 게임이 멈춰 있어도(Time.timeScale = 0) 동작한다.
    /// 전체 화면을 덮는 검은 Image + CanvasGroup 오브젝트에 붙이고, 다른 UI보다 위에 둔다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        [Tooltip("어두워지는 시간 = 밝아지는 시간 (초)")]
        [SerializeField, Min(0f)] float duration = 0.3f;

        CanvasGroup canvasGroup;

        /// <summary>페이드 중이면 true (그 사이 다른 전환은 무시)</summary>
        public bool IsBusy { get; private set; }

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            Clear();
        }

        /// <summary>화면을 어둡게 한 뒤 atBlack을 실행하고 다시 밝힌다.</summary>
        public void Play(Action atBlack)
        {
            if (IsBusy)
                return;

            // 비활성으로 두었어도 동작하도록 켠다 (Awake에서 투명 상태로 초기화됨)
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            IsBusy = true;
            canvasGroup.DOKill();
            canvasGroup.blocksRaycasts = true;

            DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, duration))
                .AppendCallback(() => atBlack?.Invoke())
                .Append(canvasGroup.DOFade(0f, duration))
                .OnComplete(() =>
                {
                    Clear();
                    IsBusy = false;
                })
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        void Clear()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
