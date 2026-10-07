using UnityEngine;

namespace BurgerIdle.Unlock
{
    /// <summary>
    /// 해금 항목 데이터. 해금되는 씬 오브젝트는 UnlockBox에서 연결한다. (ScriptableObject는 씬 오브젝트를 참조할 수 없음)
    /// </summary>
    [CreateAssetMenu(fileName = "Unlock_", menuName = "BurgerIdle/Unlock Definition")]
    public sealed class UnlockDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Min(1)] public int cost = 30;
        [Tooltip("0이면 레벨 조건 없음 (레벨 확인은 P8에서 연결)")]
        [Min(0)] public int requiredPlayerLevel;
    }
}
