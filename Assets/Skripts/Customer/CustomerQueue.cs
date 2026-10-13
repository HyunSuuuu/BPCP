using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>
    /// 줄. 씬에 배치한 경로 점을 따라 일정 간격의 슬롯을 만들고, 줄 선 대상(손님·차량)은 순서대로 슬롯에 선다.
    /// 앞이 빠지면 뒤 대상들이 경로를 따라 전진한다. 콜라이더·물리를 쓰지 않는다.
    /// 드라이브스루 차량 줄은 같은 컴포넌트에 차량용 Config를 연결해 쓴다.
    /// </summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        [Header("Path")]
        [Tooltip("줄 경로. 첫 점 = 맨 앞 위치(카운터 앞), 마지막 점 = 줄 끝(입구 쪽). 꺾인 줄 가능")]
        [SerializeField] Transform[] path;

        [Header("Data")]
        [Tooltip("줄 간격·최대 인원·이동속도·입장 간격 (손님용과 차량용 에셋을 따로)")]
        [SerializeField] CustomerConfig config;

        readonly List<QueueMember> members = new List<QueueMember>();

        public CustomerConfig Config => config;
        public bool IsFull => members.Count >= config.maxQueueLength;
        public QueueMember Front => members.Count > 0 ? members[0] : null;

        /// <summary>줄 선 대상 전원의 남은 주문 수량 합 (알바가 운반이 필요한지 판단할 때 사용)</summary>
        public int RemainingDemand
        {
            get
            {
                var total = 0;
                foreach (var member in members)
                    total += member.RemainingOrder;
                return total;
            }
        }

        /// <summary>경로 전체 길이. 입장한 대상은 이 지점(줄 끝)에서 줄에 합류한다.</summary>
        public float PathLength
        {
            get
            {
                var length = 0f;
                for (var i = 1; i < path.Length; i++)
                    length += Vector3.Distance(path[i - 1].position, path[i].position);
                return length;
            }
        }

        public void Join(QueueMember member) => members.Add(member);

        public void Leave(QueueMember member) => members.Remove(member);

        /// <summary>대상이 서야 할 경로상 거리 (맨 앞 = 0).</summary>
        public float SlotDistance(QueueMember member) => members.IndexOf(member) * config.queueSpacing;

        public bool IsFront(QueueMember member) => members.Count > 0 && members[0] == member;

        /// <summary>경로 시작점에서 distance만큼 떨어진 위치. 경로보다 길면 마지막 구간 방향으로 연장한다.</summary>
        public Vector3 Evaluate(float distance)
        {
            if (path.Length == 1)
                return path[0].position;

            for (var i = 1; i < path.Length; i++)
            {
                var a = path[i - 1].position;
                var b = path[i].position;
                var segment = Vector3.Distance(a, b);
                if (distance <= segment || i == path.Length - 1)
                    return a + (b - a).normalized * distance;
                distance -= segment;
            }

            return path[path.Length - 1].position;
        }

        /// <summary>distance 지점에서 줄 앞쪽(카운터 쪽)을 바라보는 방향.</summary>
        public Vector3 ForwardAt(float distance)
        {
            if (path.Length < 2)
                return transform.forward;

            for (var i = 1; i < path.Length; i++)
            {
                var segment = Vector3.Distance(path[i - 1].position, path[i].position);
                if (distance <= segment || i == path.Length - 1)
                    return (path[i - 1].position - path[i].position).normalized;
                distance -= segment;
            }

            return transform.forward;
        }

        void OnDrawGizmos()
        {
            if (path == null || path.Length == 0 || config == null)
                return;

            Gizmos.color = Color.yellow;
            for (var i = 1; i < path.Length; i++)
            {
                if (path[i - 1] != null && path[i] != null)
                    Gizmos.DrawLine(path[i - 1].position, path[i].position);
            }

            foreach (var point in path)
            {
                if (point == null)
                    return;
            }

            for (var i = 0; i < config.maxQueueLength; i++)
                Gizmos.DrawWireSphere(Evaluate(i * config.queueSpacing) + Vector3.up * 0.1f, 0.25f);
        }
    }
}
