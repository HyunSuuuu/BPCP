using System;
using System.Collections.Generic;
using BurgerIdle.Economy;
using R3;

namespace BurgerIdle.Upgrade
{
    /// <summary>
    /// 업그레이드 레벨과 스탯 값을 관리한다. 구매는 돈(TryPurchase)과 광고 시뮬레이션(TryUpgradeByAd) 두 곳에서만 처리한다.
    /// 스탯을 쓰는 쪽(플레이어, 기계, 테이블)은 Stat(key)를 구독해 값이 바뀌면 바로 반영한다.
    /// </summary>
    public sealed class UpgradeManager : IDisposable
    {
        readonly List<UpgradeDefinition> definitions = new List<UpgradeDefinition>();
        readonly Dictionary<UpgradeDefinition, ReactiveProperty<int>> levels = new Dictionary<UpgradeDefinition, ReactiveProperty<int>>();
        readonly Dictionary<UpgradeDefinition, float> adReadyTime = new Dictionary<UpgradeDefinition, float>();
        readonly Dictionary<StatKey, ReactiveProperty<float>> stats = new Dictionary<StatKey, ReactiveProperty<float>>();
        readonly IReadOnlyDictionary<StatKey, float> baseValues;
        readonly IReadOnlyDictionary<StatKey, (float min, float max)> ranges;
        readonly CurrencyManager currency;
        readonly PlayerLevel playerLevel;
        readonly float adCooldown;

        public IReadOnlyList<UpgradeDefinition> Definitions => definitions;

        /// <summary>광고 시뮬레이션 시청 시간 (초)</summary>
        public float AdDuration { get; }

        /// <param name="ranges">스탯별 최소·최대값. 업그레이드해도 이 범위를 넘지 않는다 (없는 스탯은 제한 없음)</param>
        /// <param name="adDuration">광고 시뮬레이션 시청 시간 (초)</param>
        /// <param name="adCooldown">광고 업그레이드 후 같은 항목을 다시 광고로 올릴 수 있기까지의 시간 (초)</param>
        public UpgradeManager(IEnumerable<UpgradeDefinition> definitions, IReadOnlyDictionary<StatKey, float> baseValues,
            IReadOnlyDictionary<StatKey, (float min, float max)> ranges, CurrencyManager currency, PlayerLevel playerLevel,
            float adDuration, float adCooldown)
        {
            this.baseValues = baseValues;
            this.ranges = ranges;
            this.currency = currency;
            this.playerLevel = playerLevel;
            AdDuration = adDuration;
            this.adCooldown = adCooldown;

            if (definitions == null)
                return;

            foreach (var definition in definitions)
            {
                if (definition == null || levels.ContainsKey(definition))
                    continue;
                this.definitions.Add(definition);
                levels.Add(definition, new ReactiveProperty<int>(0));
            }
        }

        public ReadOnlyReactiveProperty<int> LevelOf(UpgradeDefinition definition) => levels[definition];

        /// <summary>스탯 현재 값. 업그레이드 항목이 없는 스탯은 기본값 그대로.</summary>
        public ReadOnlyReactiveProperty<float> Stat(StatKey key)
        {
            if (!stats.TryGetValue(key, out var property))
            {
                property = new ReactiveProperty<float>(Calculate(key));
                stats.Add(key, property);
            }

            return property;
        }

        public float ValueAt(UpgradeDefinition definition, int level) =>
            Clamp(definition.stat, BaseValue(definition.stat) + level * definition.valuePerLevel);

        /// <summary>최대 레벨이거나, 스탯이 이미 범위 끝(최대·최소)에 닿아 더 올려도 효과가 없으면 true.</summary>
        public bool IsMaxed(UpgradeDefinition definition)
        {
            if (levels[definition].Value >= definition.maxLevel)
                return true;
            if (!ranges.TryGetValue(definition.stat, out var range))
                return false;

            var current = Calculate(definition.stat);
            return definition.valuePerLevel > 0f ? current >= range.max : current <= range.min;
        }

        public bool IsLocked(UpgradeDefinition definition) => playerLevel.Level.CurrentValue < definition.requiredPlayerLevel;

        public int CurrentCost(UpgradeDefinition definition) => definition.CostAt(levels[definition].Value);

        public bool TryPurchase(UpgradeDefinition definition)
        {
            if (IsMaxed(definition) || IsLocked(definition))
                return false;
            if (!currency.TrySpend(CurrencyType.Money, CurrentCost(definition)))
                return false;

            LevelUp(definition);
            return true;
        }

        /// <summary>광고 업그레이드까지 남은 대기 시간 (초). 0이면 바로 가능.</summary>
        public float AdCooldownRemaining(UpgradeDefinition definition) =>
            adReadyTime.TryGetValue(definition, out var ready) ? Math.Max(0f, ready - UnityEngine.Time.unscaledTime) : 0f;

        public bool CanUpgradeByAd(UpgradeDefinition definition) =>
            !IsMaxed(definition) && !IsLocked(definition) && AdCooldownRemaining(definition) <= 0f;

        /// <summary>광고 시청 보상으로 비용 없이 1레벨 올린다. 이후 같은 항목은 대기 시간이 생긴다.</summary>
        public bool TryUpgradeByAd(UpgradeDefinition definition)
        {
            if (!CanUpgradeByAd(definition))
                return false;

            adReadyTime[definition] = UnityEngine.Time.unscaledTime + adCooldown;
            LevelUp(definition);
            return true;
        }

        void LevelUp(UpgradeDefinition definition)
        {
            levels[definition].Value++;
            if (stats.TryGetValue(definition.stat, out var property))
                property.Value = Calculate(definition.stat);
        }

        float Calculate(StatKey key)
        {
            var value = BaseValue(key);
            foreach (var definition in definitions)
            {
                if (definition.stat == key)
                    value += levels[definition].Value * definition.valuePerLevel;
            }

            return Clamp(key, value);
        }

        float Clamp(StatKey key, float value) =>
            ranges.TryGetValue(key, out var range) ? Math.Clamp(value, range.min, range.max) : value;

        float BaseValue(StatKey key) => baseValues.TryGetValue(key, out var value) ? value : 0f;

        public void Dispose()
        {
            foreach (var property in levels.Values)
                property.Dispose();
            foreach (var property in stats.Values)
                property.Dispose();
        }
    }
}
