using BurgerIdle.Economy;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 돈·경험치 표시. 값이 바뀌면 카운트업 트윈으로 갱신한다. (레벨·경험치 바는 P8에서 추가)
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        [SerializeField] TMP_Text moneyText;
        [SerializeField] TMP_Text expText;
        [SerializeField] float countDuration = 0.3f;

        public void Init(CurrencyManager currency)
        {
            Bind(currency.Money, moneyText);
            Bind(currency.Exp, expText);
        }

        void Bind(ReadOnlyReactiveProperty<int> source, TMP_Text label)
        {
            if (label == null)
                return;

            var shown = source.CurrentValue;
            label.text = Format(shown);

            source.Skip(1)
                .Subscribe(value =>
                {
                    label.DOKill();
                    DOTween.To(() => shown, x => { shown = x; label.text = Format(x); }, value, countDuration)
                        .SetEase(Ease.OutQuad)
                        .SetTarget(label)
                        .SetLink(label.gameObject);

                    label.transform.DOKill();
                    if (value > shown)
                        label.transform.DOPunchScale(Vector3.one * 0.15f, countDuration, 6, 0.5f)
                            .SetLink(label.gameObject)
                            .OnKill(() => label.transform.localScale = Vector3.one);
                })
                .AddTo(this);
        }

        static string Format(int value) => value.ToString("N0");
    }
}
