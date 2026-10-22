using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Unlock
{
    /// <summary>
    /// 구역(Zone)을 순서대로 연다. Zones 오브젝트에 붙이고, 자식 Zone들을 Hierarchy 순서대로 쓴다.
    /// 이전 Zone의 시설이 모두 해금되면 → 다음 Zone의 확장 박스가 나타나고 →
    /// 확장 박스를 해금하면 Floor가 솟아오르고 RemoveOnExpand가 내려가며 사라진 뒤 → Facilities의 시설 박스가 나타난다.
    /// 초기화는 GameBootstrap이 NavMesh를 굽기 전에 한다.
    /// </summary>
    public sealed class ZoneManager : MonoBehaviour
    {
        const float TweenDuration = 0.4f;

        [Tooltip("솟아오르기·내려가기 이동 거리")]
        [SerializeField] float moveDistance = 3f;

        readonly ReactiveProperty<bool> isFinished = new ReactiveProperty<bool>(false);
        Zone[] zones;

        /// <summary>마지막 Zone의 확장 박스를 해금했다 (모든 Zone이 열림). 지역 클리어·데모 완료는 레벨 기준이라 AreaManager가 따로 판단한다</summary>
        public ReadOnlyReactiveProperty<bool> IsFinished => isFinished;

        void Awake() => isFinished.AddTo(this);

        /// <summary>시작 상태로 숨기고 단계 완료를 구독한다. 시작 공간(Expansion 없는 Zone)은 그대로 둔다.</summary>
        public void Init()
        {
            zones = GetComponentsInChildren<Zone>(true);
            if (zones.Length == 0)
            {
                Debug.LogWarning($"[ZoneManager] {name}: 자식에 Zone이 없습니다.", this);
                return;
            }

            foreach (var zone in zones)
            {
                if (zone.IsStartZone)
                    continue;

                SetActive(zone.Expansion, false);
                SetActive(zone.Facilities, false);
                if (zone.Floor != null)
                    zone.Floor.SetActive(false);
            }

            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                var next = i + 1 < zones.Length ? zones[i + 1] : null;

                // 확장 박스 해금 → 공간 확장 → 시설 박스 등장 (시설이 없으면 바로 다음 확장 박스)
                if (!zone.IsStartZone)
                {
                    zone.Expansion.IsUnlocked
                        .Where(unlocked => unlocked)
                        .Take(1)
                        .Subscribe(_ =>
                        {
                            Expand(zone);
                            if (!zone.HasFacilities && next != null)
                                ShowExpansion(next);
                            if (next == null)
                                isFinished.Value = true;
                        })
                        .AddTo(this);
                }

                if (next == null)
                    continue;

                // 이 Zone 시설 완료 → 다음 Zone 확장 박스 등장
                if (zone.HasFacilities)
                {
                    zone.Facilities.IsCompleted
                        .Where(completed => completed)
                        .Take(1)
                        .Subscribe(_ => ShowExpansion(next))
                        .AddTo(this);
                }
                else if (zone.IsStartZone)
                {
                    // 시설 없는 시작 공간 (예: 매장 밖 ZoneStart) → 처음부터 다음 확장 박스
                    ShowExpansion(next);
                }
            }

            // 첫 Zone이 시작 공간이 아니면 처음부터 확장 박스를 보여 준다
            if (!zones[0].IsStartZone)
                ShowExpansion(zones[0]);
        }

        void ShowExpansion(Zone zone)
        {
            if (zone.IsStartZone)
                return;

            Pop(zone.Expansion.transform);
            Debug.Log($"[ZoneManager] {zone.name} 확장 박스 등장");
        }

        void Expand(Zone zone)
        {
            var floor = zone.Floor;
            if (floor != null)
            {
                floor.SetActive(true);
                var t = floor.transform;
                var target = t.localPosition;
                t.localPosition = target + Vector3.down * moveDistance;
                t.DOLocalMove(target, TweenDuration).SetEase(Ease.OutBack).SetLink(floor);
            }

            var remove = zone.RemoveOnExpand;
            if (remove != null && remove.activeSelf)
            {
                remove.transform.DOLocalMove(remove.transform.localPosition + Vector3.down * moveDistance, TweenDuration)
                    .SetEase(Ease.InBack)
                    .SetLink(remove)
                    .OnComplete(() => remove.SetActive(false));
            }

            if (zone.HasFacilities)
            {
                zone.Facilities.gameObject.SetActive(true);
                foreach (var box in zone.Facilities.GetComponentsInChildren<UnlockBox>())
                    Pop(box.transform);
            }

            Debug.Log($"[ZoneManager] {zone.name} 확장");
        }

        static void Pop(Transform t)
        {
            t.gameObject.SetActive(true);
            var scale = t.localScale;
            t.localScale = Vector3.zero;
            t.DOScale(scale, 0.3f).SetEase(Ease.OutBack).SetLink(t.gameObject);
        }

        static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }
    }
}
