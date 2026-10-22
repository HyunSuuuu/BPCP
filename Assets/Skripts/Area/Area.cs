using System.Collections.Generic;
using BurgerIdle.Unlock;
using UnityEngine;

namespace BurgerIdle.Areas
{
    /// <summary>
    /// 지역(매장) 하나의 전체 영역. 출입문을 모아 관리하고, 스포너는 출입문 종류로 문을 찾는다.
    /// 여러 지역은 Areas(AreaManager) 아래에 Hierarchy 순서대로 둔다. 지역 설정(이름·배율·지속 수입·시작 위치·미니맵 위치)도 여기에 둔다.
    /// </summary>
    public sealed class Area : MonoBehaviour
    {
        [Tooltip("비워 두면 자식의 AreaGate를 모두 사용한다.")]
        [SerializeField] AreaGate[] gates;

        [Header("Region")]
        [Tooltip("지역 이름. 비우면 \"지역 N\" (N = Areas 아래 순서)")]
        [SerializeField] string displayName;
        [Tooltip("이 지역 모든 해금 박스 비용 배율")]
        [SerializeField, Min(0.01f)] float unlockCostMultiplier = 1f;
        [Tooltip("이 지역 판매·팁·배달 수익 배율 (플레이어 수익 배율과 곱함). 지속 수입에는 적용 안 함")]
        [SerializeField, Min(0f)] float incomeMultiplier = 1f;
        [Tooltip("클리어 후 분당 지속 수입. 플레이어가 이 지역을 떠나 있을 때만 쌓이고, 미니맵에서 수거한다")]
        [SerializeField, Min(0)] int incomePerMinute;
        [Tooltip("이 지역에 입장할 때 순간이동할 위치. 비우면 시작 공간(Expansion 없는 Zone) 바닥 중앙")]
        [SerializeField] Transform startPoint;
        [Tooltip("미니맵에서 매장 아이콘 위치 (미니맵 Content 기준 UI 좌표)")]
        [SerializeField] Vector2 mapPosition;

        readonly List<AreaGate> candidates = new List<AreaGate>();

        public string DisplayName => displayName;
        public float UnlockCostMultiplier => unlockCostMultiplier;
        public float IncomeMultiplier => incomeMultiplier;
        public int IncomePerMinute => incomePerMinute;
        public Vector2 MapPosition => mapPosition;

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

        /// <summary>입장 위치. Start Point가 없으면 시작 공간 바닥(렌더러) 중앙, 그것도 없으면 이 오브젝트 위치.</summary>
        public Vector3 GetStartPosition()
        {
            if (startPoint != null)
                return startPoint.position;

            foreach (var zone in GetComponentsInChildren<Zone>(true))
            {
                if (!zone.IsStartZone || zone.Floor == null)
                    continue;

                var renderers = zone.Floor.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    return zone.Floor.transform.position;

                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                // 높이는 순간이동 때 NavMesh 위로 맞춘다
                return bounds.center;
            }

            return transform.position;
        }
    }
}
