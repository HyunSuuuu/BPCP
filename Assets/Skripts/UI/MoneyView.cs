using BurgerIdle.Economy;
using R3;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>소지금 표시. HUD의 돈 묶음 프리팹 루트에 붙인다.</summary>
    public sealed class MoneyView : MonoBehaviour
    {
        [Tooltip("소지금 숫자 (CountUpText가 붙은 TMP 텍스트)")]
        [SerializeField] CountUpText moneyText;

        public void Bind(CurrencyManager currency)
        {
            if (moneyText == null)
            {
                Debug.LogError($"[MoneyView] {name}: Money Text를 연결하세요.", this);
                return;
            }

            currency.Money.Subscribe(moneyText.SetValue).AddTo(this);
        }
    }
}
