using System;
using BurgerIdle.Areas;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 미니맵에서 매장을 눌렀을 때 뜨는 매장 UI.
    /// - 열린 지역: 입장 버튼 활성
    /// - 클리어한 지역: 입장·수거 버튼 활성 + 분당 수입·쌓인 금액 표시
    /// - 잠긴 지역: 버튼 비활성 + 오픈 조건 안내
    /// 미니맵 화면 안의 매장 UI 오브젝트에 붙인다. 연결하지 않은 칸은 무시한다.
    /// </summary>
    public sealed class MiniMapStorePopup : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] TMP_Text nameText;
        [Tooltip("분당 수입·쌓인 금액 묶음. 클리어한 지역만 표시")]
        [SerializeField] GameObject incomeGroup;
        [SerializeField] TMP_Text incomeText;
        [Tooltip("{0} = 분당 수입")]
        [SerializeField] string incomeFormat = "+{0:N0} / 분";
        [Tooltip("쌓인 지속 수입. 상한이면 MAX")]
        [SerializeField] TMP_Text storedText;
        [Tooltip("잠긴 지역 안내. 잠긴 지역만 표시")]
        [SerializeField] TMP_Text lockText;
        [Tooltip("{0} = 이전 지역 이름, {1} = 클리어 레벨")]
        [SerializeField] string lockFormat = "{0} Lv {1} 달성 시 오픈";

        [Header("Buttons")]
        [SerializeField] Button enterButton;
        [SerializeField] Button collectButton;

        readonly ReactiveProperty<RegionState> shown = new ReactiveProperty<RegionState>();

        AreaManager areas;

        public RectTransform Rect => (RectTransform)transform;
        public bool IsShown => gameObject.activeSelf;

        /// <param name="onEnter">입장 버튼을 눌렀을 때 (미니맵이 닫히고 그 지역으로 이동)</param>
        public void Init(AreaManager areas, Action<RegionState> onEnter)
        {
            this.areas = areas;
            shown.AddTo(this);

            if (enterButton != null)
                enterButton.onClick.AddListener(() =>
                {
                    if (shown.Value != null)
                        onEnter(shown.Value);
                });
            if (collectButton != null)
                collectButton.onClick.AddListener(Collect);

            // 보고 있는 지역이 바뀌면 그 지역의 상태를 새로 구독한다
            shown.Where(region => region != null)
                .Select(region => Observable.CombineLatest(region.IsOpen, region.IsCleared, region.StoredIncome,
                    (open, cleared, stored) => (region, open, cleared, stored)))
                .Switch()
                .Subscribe(x => Refresh(x.region, x.open, x.cleared, x.stored))
                .AddTo(this);

            gameObject.SetActive(false);
        }

        public void Show(RegionState region)
        {
            shown.Value = region;
            gameObject.SetActive(true);

            transform.DOKill();
            transform.localScale = Vector3.one * 0.85f;
            transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            transform.DOKill();
            gameObject.SetActive(false);
        }

        void Collect()
        {
            var amount = areas.Collect(shown.Value);
            if (amount <= 0 || storedText == null)
                return;

            storedText.transform.DOKill(true);
            storedText.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.5f).SetUpdate(true).SetLink(storedText.gameObject);
        }

        void Refresh(RegionState region, bool open, bool cleared, int stored)
        {
            if (nameText != null)
                nameText.text = region.DisplayName;

            if (enterButton != null)
                enterButton.interactable = open;
            if (collectButton != null)
                collectButton.interactable = cleared;

            if (incomeGroup != null)
                incomeGroup.SetActive(cleared);
            if (incomeText != null)
                incomeText.text = string.Format(incomeFormat, region.IncomePerMinute);
            if (storedText != null)
            {
                storedText.gameObject.SetActive(cleared);
                storedText.text = stored >= region.IncomeCap(areas.IncomeCapMinutes) && stored > 0
                    ? $"{stored:N0} (MAX)"
                    : stored.ToString("N0");
            }

            if (lockText != null)
            {
                lockText.gameObject.SetActive(!open);
                var previous = region.Index > 0 ? areas.Regions[region.Index - 1].DisplayName : "";
                lockText.text = string.Format(lockFormat, previous, region.Level.MaxLevel);
            }
        }
    }
}
