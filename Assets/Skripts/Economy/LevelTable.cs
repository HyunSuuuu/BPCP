using UnityEngine;

namespace BurgerIdle.Economy
{
    /// <summary>
    /// 경험치 → 레벨 곡선. 레벨 N에서 N+1까지 필요한 경험치 = round(Base Exp × Growth^(N-1)).
    /// 시작 레벨이 0이면 0 → 1은 경험치가 아니라 해금(UnlockDefinition의 Grant Level)으로 오른다.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelTable", menuName = "BurgerIdle/Level Table")]
    public sealed class LevelTable : ScriptableObject
    {
        [Tooltip("시작 레벨. 0이면 매장(Zone0) 해금 같은 Grant Level 해금으로 1이 될 때까지 경험치가 레벨에 반영되지 않음")]
        [Min(0)] public int startLevel = 0;
        [Tooltip("Lv1 → Lv2 필요 경험치")]
        [Min(1)] public int baseExp = 50;
        [Tooltip("레벨마다 필요 경험치 배율")]
        [Min(1f)] public float growth = 1.5f;
        [Min(1)] public int maxLevel = 20;

        public int ExpToNext(int level) => Mathf.Max(1, Mathf.RoundToInt(baseExp * Mathf.Pow(growth, level - 1)));
    }
}
