using BurgerIdle.Economy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 에디터·개발 빌드 전용 테스트 키.
    /// M: 돈 +10 / K: 돈 +100 / N: 돈 30 지불(TrySpend) / B: 돈 최대 30 지불(SpendUpTo) / E: 경험치 +5
    /// T: 게임 속도 1배 ↔ 3배
    /// </summary>
    public sealed class DebugCheats : MonoBehaviour
    {
        const float FastTimeScale = 3f;

        CurrencyManager currency;

        public void Init(CurrencyManager currency) => this.currency = currency;

        void OnDestroy() => Time.timeScale = 1f;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || currency == null)
                return;

            if (keyboard.mKey.wasPressedThisFrame)
                currency.Add(CurrencyType.Money, 10);
            if (keyboard.kKey.wasPressedThisFrame)
                currency.Add(CurrencyType.Money, 100);
            if (keyboard.nKey.wasPressedThisFrame)
                Debug.Log($"[Debug] TrySpend(30) → {currency.TrySpend(CurrencyType.Money, 30)}");
            if (keyboard.bKey.wasPressedThisFrame)
                Debug.Log($"[Debug] SpendUpTo(30) → {currency.SpendUpTo(30)}");
            if (keyboard.eKey.wasPressedThisFrame)
                currency.Add(CurrencyType.Exp, 5);
            if (keyboard.tKey.wasPressedThisFrame)
            {
                Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? FastTimeScale : 1f;
                Debug.Log($"[Debug] 게임 속도 x{Time.timeScale}");
            }
        }
    }
}
