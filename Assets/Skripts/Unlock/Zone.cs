using UnityEngine;

namespace BurgerIdle.Unlock
{
    /// <summary>
    /// 구역 하나. Zones(ZoneManager) 아래 각 Zone 루트에 붙인다. 칸을 비워 두면 같은 이름의 자식을 자동으로 찾는다.
    ///
    ///   Zone1                 ← Zone
    ///   ├─ Floor              ← 이 Zone을 열면 솟아오를 바닥·벽·장식
    ///   ├─ Expansion          ← 이 Zone을 여는 확장 해금 박스 (UnlockBox, Target 비움)
    ///   ├─ Facilities         ← 확장 뒤 나타날 시설 해금 박스 묶음 (UnlockZone)
    ///   └─ Objects            ← 시설 본체 (해금 박스의 Target). Zone은 관여하지 않음
    ///
    /// 확장 박스는 "이 Zone을 여는 박스"다. 박스가 이전 Zone 바닥 위에 놓여 있어도 Hierarchy는 이 Zone 아래에 둔다.
    /// Expansion이 없는 Zone(시작 공간)은 처음부터 모두 보인다.
    /// </summary>
    public sealed class Zone : MonoBehaviour
    {
        public const string FloorName = "Floor";
        public const string RemoveName = "RemoveOnExpand";
        public const string ExpansionName = "Expansion";
        public const string FacilitiesName = "Facilities";

        [Header("비워 두면 같은 이름의 자식을 자동으로 찾음")]
        [Tooltip("이 Zone을 열 때 아래에서 솟아오를 묶음 (바닥, 벽, 쓰레기통·업그레이드 패드처럼 해금 박스 없이 같이 나타날 것).\n" +
                 "시작 전에는 숨겨진다. 시작 공간은 숨기지 않는다.\n" +
                 "비우면 자식 \"Floor\"")]
        [SerializeField] GameObject floor;

        [Tooltip("이 Zone을 열 때 내려가며 사라질 오브젝트 (이전 Zone과의 칸막이 벽).\n" +
                 "보통 이전 Zone의 Floor 안에 있는 벽을 여기에 직접 끌어 넣는다.\n" +
                 "비우면 자식 \"RemoveOnExpand\", 없으면 사라지는 것 없음")]
        [SerializeField] GameObject removeOnExpand;

        [Tooltip("이 Zone을 여는 매장 확장 해금 박스 (UnlockBox).\n" +
                 "박스의 Definition = 가격·필요 레벨, Target = 비움 (Zone 표시는 ZoneManager가 함).\n" +
                 "이전 Zone의 시설 해금이 모두 끝나면 나타난다.\n" +
                 "시작 공간은 비운다. 비우면 자식 \"Expansion\"(또는 그 아래)의 UnlockBox")]
        [SerializeField] UnlockBox expansion;

        [Tooltip("확장 뒤 나타날 시설 해금 박스 묶음 (UnlockZone). 아래에 UnlockBox들을 둔다.\n" +
                 "모두 해금되면 다음 Zone의 확장 박스가 나타난다. 없으면 이 Zone이 열리자마자 다음 확장 박스가 나타난다.\n" +
                 "손님 스포너의 Open Zone에 연결하면 이 시설이 모두 열린 뒤 손님이 온다.\n" +
                 "비우면 자식 \"Facilities\"의 UnlockZone")]
        [SerializeField] UnlockZone facilities;

        bool resolved;

        public GameObject Floor { get { Resolve(); return floor; } }
        public GameObject RemoveOnExpand { get { Resolve(); return removeOnExpand; } }
        public UnlockBox Expansion { get { Resolve(); return expansion; } }
        public UnlockZone Facilities { get { Resolve(); return facilities; } }

        /// <summary>확장 박스가 없으면 시작 공간</summary>
        public bool IsStartZone => Expansion == null;

        /// <summary>Facilities 아래에 해금 박스가 하나라도 있는지</summary>
        public bool HasFacilities => Facilities != null && Facilities.GetComponentsInChildren<UnlockBox>(true).Length > 0;

        void Resolve()
        {
            if (resolved)
                return;
            resolved = true;

            if (floor == null)
                floor = FindChild(FloorName)?.gameObject;
            if (removeOnExpand == null)
                removeOnExpand = FindChild(RemoveName)?.gameObject;
            if (expansion == null)
                expansion = FindChild(ExpansionName)?.GetComponentInChildren<UnlockBox>(true);
            if (facilities == null)
                facilities = FindChild(FacilitiesName)?.GetComponent<UnlockZone>();

            if (expansion != null && expansion.Target != null)
                Debug.LogWarning($"[Zone] {name}: 확장 박스 {expansion.name}의 Target({expansion.Target.name})을 비워 주세요. Zone 표시는 ZoneManager가 합니다.", expansion);
        }

        Transform FindChild(string childName)
        {
            foreach (Transform child in transform)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
