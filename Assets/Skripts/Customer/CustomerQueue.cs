using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Customers
{
    /// <summary>
    /// 손님 줄. 씬에 배치한 경로 점을 따라 일정 간격의 슬롯을 만들고, 손님은 줄 순서대로 슬롯에 선다.
    /// 앞 손님이 빠지면 뒤 손님들이 경로를 따라 전진한다. 콜라이더·물리를 쓰지 않는다.
    /// </summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        [Tooltip("줄 경로. 첫 점 = 맨 앞 손님 위치(카운터 앞), 마지막 점 = 줄 끝(입구 쪽). 꺾인 줄 가능")]
        [SerializeField] Transform[] path;
        [SerializeField] CustomerConfig config;

        readonly List<Customer> customers = new List<Customer>();

        public CustomerConfig Config => config;
        public bool IsFull => customers.Count >= config.maxQueueLength;
        public Customer Front => customers.Count > 0 ? customers[0] : null;

        /// <summary>경로 전체 길이. 입장한 손님은 이 지점에서 줄에 합류한다.</summary>
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

        public void Join(Customer customer) => customers.Add(customer);

        public void Leave(Customer customer) => customers.Remove(customer);

        /// <summary>손님이 서야 할 경로상 거리 (맨 앞 = 0).</summary>
        public float SlotDistance(Customer customer) => customers.IndexOf(customer) * config.queueSpacing;

        public bool IsFront(Customer customer) => customers.Count > 0 && customers[0] == customer;

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
