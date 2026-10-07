using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Areas
{
    /// <summary>
    /// 매장 전체 영역. 출입문을 모아 관리하고, 스포너는 출입문 종류로 문을 찾는다. (데모는 Area 1개)
    /// Zone 단위 확장은 P10에서 연결한다.
    /// </summary>
    public sealed class Area : MonoBehaviour
    {
        [Tooltip("비워 두면 자식의 AreaGate를 모두 사용한다.")]
        [SerializeField] AreaGate[] gates;

        readonly List<AreaGate> candidates = new List<AreaGate>();

        AreaGate[] Gates
        {
            get
            {
                if (gates == null || gates.Length == 0)
                    gates = GetComponentsInChildren<AreaGate>(true);
                return gates;
            }
        }

        /// <summary>해당 종류를 지원하는 출입문을 찾는다. 여러 개면 무작위로 고른다.</summary>
        public bool TryGetGate(GateType type, out AreaGate gate)
        {
            candidates.Clear();
            foreach (var g in Gates)
            {
                if (g != null && g.IsValid && g.Supports(type))
                    candidates.Add(g);
            }

            gate = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
            return gate != null;
        }
    }
}
