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
        [Tooltip("필요한 지역 레벨. 0이면 레벨 조건 없음")]
        [Min(0)] public int requiredPlayerLevel;
        [Tooltip("해금하면 그 지역 레벨을 이 값까지 올림. 0이면 없음 (예: 매장 Zone0 = 1)")]
        [Min(0)] public int grantLevel;
        [Tooltip("해금하면 알림 팝업으로 띄울 문구. 비우면 없음")]
        [TextArea(1, 3)] public string unlockMessage;
    }
}
