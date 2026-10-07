using UnityEngine;

namespace BurgerIdle.Areas
{
    public enum GateType
    {
        PeopleEntrance,
        PeopleExit,
        VehicleEntrance,
        VehicleExit,
    }

    /// <summary>
    /// Area 출입문. 바깥 점과 안쪽 점 두 개로 이루어진다.
    /// 들어올 때는 바깥 점에서 생성되어 안쪽 점을 지나고, 나갈 때는 안쪽 점을 지나 바깥 점에서 사라진다.
    /// 한 문을 입구·출구로 겸용할 수 있다.
    /// </summary>
    public sealed class AreaGate : MonoBehaviour
    {
        [Header("Usage")]
        [SerializeField] bool peopleEntrance = true;
        [SerializeField] bool peopleExit = true;
        [SerializeField] bool vehicleEntrance;
        [SerializeField] bool vehicleExit;

        [Header("Points")]
        [Tooltip("매장 밖 지점. 생성·소멸 위치")]
        [SerializeField] Transform outside;
        [Tooltip("문 안쪽 지점. 매장 안으로 들어서는 위치")]
        [SerializeField] Transform inside;

        public bool IsValid => outside != null && inside != null;
        public Vector3 OutsidePoint => outside.position;
        public Vector3 InsidePoint => inside.position;

        /// <summary>매장 안에서 밖으로 나가는 경로 (안쪽 점 → 바깥 점).</summary>
        public Vector3[] ExitRoute => new[] { InsidePoint, OutsidePoint };

        /// <summary>바깥 점에서 안쪽을 바라보는 회전. 생성 시 사용.</summary>
        public Quaternion EnterRotation
        {
            get
            {
                var direction = InsidePoint - OutsidePoint;
                direction.y = 0f;
                return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            }
        }

        public bool Supports(GateType type) => type switch
        {
            GateType.PeopleEntrance => peopleEntrance,
            GateType.PeopleExit => peopleExit,
            GateType.VehicleEntrance => vehicleEntrance,
            GateType.VehicleExit => vehicleExit,
            _ => false,
        };

        void OnDrawGizmos()
        {
            if (!IsValid)
                return;

            var forPeople = peopleEntrance || peopleExit;
            Gizmos.color = forPeople ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.3f, 0.7f, 1f);

            var from = OutsidePoint + Vector3.up * 0.1f;
            var to = InsidePoint + Vector3.up * 0.1f;
            Gizmos.DrawWireSphere(from, 0.3f);
            Gizmos.DrawSphere(to, 0.2f);
            Gizmos.DrawLine(from, to);

            // 안쪽 방향 화살표
            var direction = (to - from).normalized;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            var side = Vector3.Cross(Vector3.up, direction) * 0.25f;
            var head = to - direction * 0.4f;
            Gizmos.DrawLine(to, head + side);
            Gizmos.DrawLine(to, head - side);
        }
    }
}
