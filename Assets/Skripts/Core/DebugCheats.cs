using BurgerIdle.Areas;
using BurgerIdle.Economy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 에디터·개발 빌드 전용 테스트 키.
    /// M: 돈 +10 / K: 돈 +100 / N: 돈 30 지불(TrySpend) / B: 돈 최대 30 지불(SpendUpTo)
    /// E: 현재 지역 경험치 +5 / L: 현재 지역 레벨 +1 (레벨 0이면 1로)
    /// R: 다음 열린 지역으로 이동 (미니맵 없이 지역 이동 테스트)
    /// T: 게임 속도 1배 ↔ 3배
    /// </summary>
    public sealed class DebugCheats : MonoBehaviour
    {
        const float FastTimeScale = 3f;

        CurrencyManager currency;
        AreaManager areas;

        public void Init(CurrencyManager currency, AreaManager areas)
        {
            this.currency = currency;
            this.areas = areas;
        }

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
                CurrentLevel?.AddExp(5);
            if (keyboard.lKey.wasPressedThisFrame)
                LevelUp();
            if (keyboard.rKey.wasPressedThisFrame)
                EnterNextRegion();
            if (keyboard.tKey.wasPressedThisFrame)
            {
                Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? FastTimeScale : 1f;
                Debug.Log($"[Debug] 게임 속도 x{Time.timeScale}");
            }
        }

        PlayerLevel CurrentLevel => areas != null ? areas.Current.CurrentValue?.Level : null;

        void LevelUp()
        {
            var level = CurrentLevel;
            if (level == null)
                return;

            if (level.Level.CurrentValue < 1)
                level.RaiseTo(1);
            else
                level.AddExp(level.ExpToNext.CurrentValue - level.ExpInLevel.CurrentValue);
        }

        /// <summary>현재 지역 다음의 열린 지역으로 옮긴다. 마지막이면 처음으로 돌아간다.</summary>
        void EnterNextRegion()
        {
            if (areas == null || areas.Regions.Count < 2)
                return;

            var start = areas.Current.CurrentValue.Index;
            for (var step = 1; step < areas.Regions.Count; step++)
            {
                var region = areas.Regions[(start + step) % areas.Regions.Count];
                if (region.IsOpen.CurrentValue)
                {
                    areas.Enter(region);
                    return;
                }
            }

            Debug.Log("[Debug] 이동할 수 있는 다른 지역이 없습니다 (아직 열리지 않음)");
        }
    }
}
