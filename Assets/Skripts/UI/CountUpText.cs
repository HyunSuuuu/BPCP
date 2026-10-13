using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 숫자 텍스트 공용 연출. 값이 바뀌면 카운트업 트윈으로 갱신하고, 늘어날 때 살짝 튄다.
    /// 숫자를 보여 줄 TextMeshPro 오브젝트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CountUpText : MonoBehaviour
    {
        [Tooltip("표시 형식. {0}이 숫자 자리 (예: {0:N0}, {0:N0}원). {0} 없이 N0·0·#,0처럼 숫자 서식만 적어도 된다")]
        [SerializeField] string format = "{0:N0}";
        [SerializeField] float duration = 0.3f;
        [Tooltip("값이 늘어날 때 튀는 크기 (0이면 튀지 않음)")]
        [SerializeField] float punchScale = 0.15f;

        TMP_Text label;
        int shown;
        bool initialized;

        void Awake() => label = GetComponent<TMP_Text>();

        /// <summary>처음 호출은 연출 없이 바로 표시하고, 이후는 카운트업한다.</summary>
        public void SetValue(int value)
        {
            if (label == null)
                label = GetComponent<TMP_Text>();

            if (!initialized)
            {
                initialized = true;
                shown = value;
                label.text = Format(value);
                return;
            }

            var increased = value > shown;
            label.DOKill();
            DOTween.To(() => shown, x => { shown = x; label.text = Format(x); }, value, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(label)
                .SetLink(gameObject);

            if (!increased || punchScale <= 0f)
                return;

            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * punchScale, duration, 6, 0.5f).SetLink(gameObject);
        }

        /// <summary>"{0:N0}원"처럼 자리가 있으면 끼워 넣고, "N0"처럼 숫자 서식만 있으면 그 서식으로, 비어 있으면 기본 N0.</summary>
        string Format(int value)
        {
            if (string.IsNullOrEmpty(format))
                return value.ToString("N0");
            return format.Contains("{0") ? string.Format(format, value) : value.ToString(format);
        }
    }
}
