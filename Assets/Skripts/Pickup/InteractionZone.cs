using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 상호작용 범위. 콜라이더 대신 XZ 평면 거리로 판정한다.
    /// </summary>
    public sealed class InteractionZone : MonoBehaviour
    {
        [Header("Range")]
        [Tooltip("상호작용 반경 (바닥 평면 거리). 선택하면 초록 원으로 표시")]
        [SerializeField] float radius = 1.2f;
        [Tooltip("원의 중심 위치 (이 오브젝트 기준)")]
        [SerializeField] Vector3 offset;

        public float Radius => radius;
        public Vector3 Center => transform.position + transform.rotation * offset;

        public bool Contains(Vector3 position)
        {
            var center = Center;
            var dx = position.x - center.x;
            var dz = position.z - center.z;
            return dx * dx + dz * dz <= radius * radius;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f);
            var center = Center;
            const int segments = 32;
            for (var i = 0; i < segments; i++)
            {
                var a = i / (float)segments * Mathf.PI * 2f;
                var b = (i + 1) / (float)segments * Mathf.PI * 2f;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius,
                    center + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius);
            }
        }
    }
}
