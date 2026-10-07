using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>손님 관련 임시값. 진상 손님 파라미터는 P13에서 추가한다.</summary>
    [CreateAssetMenu(fileName = "CustomerConfig", menuName = "BurgerIdle/Customer Config")]
    public sealed class CustomerConfig : ScriptableObject
    {
        public float moveSpeed = 3f;
        [Tooltip("손님 입장 간격 (초)")]
        public float spawnInterval = 3f;
        [Tooltip("줄 간격 (유닛)")]
        public float queueSpacing = 1f;
        [Tooltip("최대 줄 길이. 가득 차면 입장하지 않는다")]
        public int maxQueueLength = 8;
    }
}
