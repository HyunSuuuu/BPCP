using System;
using BurgerIdle.Economy;
using BurgerIdle.Upgrade;
using R3;
using UnityEngine;

namespace BurgerIdle.Areas
{
    /// <summary>
    /// 지역 하나의 데이터 묶음: 레벨(경험치), 업그레이드, 열림·클리어 여부, 지속 수입 적립액.
    /// 떠난 지역도 메모리에 그대로 남으므로 지역을 옮길 때 따로 저장·복원하지 않는다. (파일 저장은 P14에서 이 묶음을 직렬화)
    /// </summary>
    public sealed class RegionState : IDisposable
    {
        readonly ReactiveProperty<bool> isOpen = new ReactiveProperty<bool>(false);
        readonly ReactiveProperty<bool> isCleared = new ReactiveProperty<bool>(false);
        readonly ReactiveProperty<int> storedIncome = new ReactiveProperty<int>(0);
        readonly ReadOnlyReactiveProperty<float> incomeMultiplier;
        readonly IDisposable clearSubscription;
        float incomeRemainder;

        public Area Area { get; }
        /// <summary>Areas 아래 순서 (0부터)</summary>
        public int Index { get; }
        public string DisplayName => string.IsNullOrEmpty(Area.DisplayName) ? $"지역 {Index + 1}" : Area.DisplayName;
        public PlayerLevel Level { get; }
        public UpgradeManager Upgrades { get; }

        /// <summary>판매·팁·배달 수익 배율 = 플레이어 수익 배율(이 지역 업그레이드) × 지역 배율</summary>
        public ReadOnlyReactiveProperty<float> IncomeMultiplier => incomeMultiplier;
        public ReadOnlyReactiveProperty<bool> IsOpen => isOpen;
        /// <summary>지역 레벨이 최대 레벨(LevelTable Max Level)에 닿으면 클리어</summary>
        public ReadOnlyReactiveProperty<bool> IsCleared => isCleared;
        /// <summary>쌓인 지속 수입 (미니맵에서 수거)</summary>
        public ReadOnlyReactiveProperty<int> StoredIncome => storedIncome;
        public int IncomePerMinute => Area.IncomePerMinute;

        public RegionState(Area area, int index, PlayerLevel level, UpgradeManager upgrades)
        {
            Area = area;
            Index = index;
            Level = level;
            Upgrades = upgrades;

            var areaMultiplier = area.IncomeMultiplier;
            incomeMultiplier = upgrades.Stat(StatKey.PlayerIncomeMultiplier)
                .Select(value => value * areaMultiplier)
                .ToReadOnlyReactiveProperty();

            clearSubscription = level.Level
                .Where(value => value >= level.MaxLevel)
                .Take(1)
                .Subscribe(_ => isCleared.Value = true);
        }

        public void Open() => isOpen.Value = true;

        /// <summary>적립 상한 = 분당 수입 × 상한 시간(분)</summary>
        public int IncomeCap(float capMinutes) =>
            (int)Math.Min((long)Math.Round(IncomePerMinute * (double)capMinutes), int.MaxValue);

        /// <summary>클리어한 지역에 지속 수입을 쌓는다. 플레이어가 이 지역을 떠나 있을 때만 AreaManager가 부른다.</summary>
        public void Accrue(float seconds, float capMinutes)
        {
            if (!isCleared.Value || IncomePerMinute <= 0 || seconds <= 0f)
                return;

            var cap = IncomeCap(capMinutes);
            if (storedIncome.Value >= cap)
                return;

            incomeRemainder += IncomePerMinute * seconds / 60f;
            var whole = Mathf.FloorToInt(incomeRemainder);
            if (whole <= 0)
                return;

            incomeRemainder -= whole;
            storedIncome.Value = (int)Math.Min((long)storedIncome.Value + whole, cap);
        }

        /// <summary>쌓인 지속 수입을 꺼내고 0으로 되돌린다.</summary>
        public int TakeIncome()
        {
            var amount = storedIncome.Value;
            storedIncome.Value = 0;
            incomeRemainder = 0f;
            return amount;
        }

        public void Dispose()
        {
            clearSubscription.Dispose();
            incomeMultiplier.Dispose();
            isOpen.Dispose();
            isCleared.Dispose();
            storedIncome.Dispose();
            Level.Dispose();
            Upgrades.Dispose();
        }
    }
}
