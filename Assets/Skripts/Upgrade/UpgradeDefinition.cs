using UnityEngine;

namespace BurgerIdle.Upgrade
{
    /// <summary>업그레이드 패드가 보여 줄 묶음.</summary>
    public enum UpgradeCategory
    {
        Player,
        Machine,
        Table,
        Worker,
    }

    /// <summary>업그레이드가 바꾸는 스탯. 기본값은 GameConfig에서 온다.</summary>
    public enum StatKey
    {
        PlayerMoveSpeed,
        PlayerMaxCarry,
        /// <summary>수익 배율. 돈 더미에 쌓이는 금액 × 배율 (반올림), x1 ~ x10</summary>
        PlayerIncomeMultiplier,
        MachineProducePerSecond,
        MachineMaxStock,
        TableEatPerItem,
        /// <summary>알바 인원수 (고용). 0명 시작, 첫 업그레이드 = 첫 고용</summary>
        WorkerCount,
        WorkerMoveSpeed,
        WorkerMaxCarry,
        /// <summary>알바 행동 속도 (판매·포장 속도 배율, 기본 1배). 간격 = 기본 간격 ÷ 이 값</summary>
        WorkerActionSpeed,
    }

    /// <summary>
    /// 업그레이드 항목. 스탯 값 = GameConfig 기본값 + 레벨 × Value Per Level.
    /// 비용 = round(Base Cost × Cost Growth^현재 레벨).
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "BurgerIdle/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Header("Display")]
        public string id;
        public string displayName;
        [Tooltip("카드에 표시할 아이콘")]
        public Sprite icon;

        [Header("Effect")]
        public UpgradeCategory category;
        public StatKey stat;

        [Header("Level · Cost")]
        [Tooltip("0이면 레벨 조건 없음")]
        [Min(0)] public int requiredPlayerLevel;
        [Min(1)] public int maxLevel = 10;

        [Min(0)] public int baseCost = 50;
        [Min(1f)] public float costGrowth = 1.35f;
        [Tooltip("레벨당 스탯 변화량. 줄여야 하는 스탯(먹는 시간)은 음수")]
        public float valuePerLevel = 0.5f;

        [Tooltip("패널 표시 형식 (예: 0.0, 0)")]
        public string valueFormat = "0.##";

        public int CostAt(int level) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, level));
    }
}
