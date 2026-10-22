using System;
using BurgerIdle.Areas;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 미니맵의 매장(지역) 아이콘 하나. 누르면 매장 UI가 열린다. 잠긴 지역은 어둡게 자물쇠와 함께 보인다.
    /// 아이콘 프리팹 루트에 붙인다. MiniMapView가 지역마다 하나씩 만들고, 위치는 Area의 Map Position으로 정한다.
    /// 연결하지 않은 칸은 무시한다.
    /// </summary>
    public sealed class MiniMapStoreIcon : MonoBehaviour
    {
        [SerializeField] Button button;
        [Tooltip("지역 이름")]
        [SerializeField] TMP_Text nameText;
        [Tooltip("잠긴 지역일 때 켤 표시 (자물쇠)")]
        [SerializeField] GameObject lockedMark;
        [Tooltip("클리어한 지역일 때 켤 표시 (체크, 별 등)")]
        [SerializeField] GameObject clearedMark;
        [Tooltip("플레이어가 지금 있는 지역일 때 켤 표시")]
        [SerializeField] GameObject currentMark;
        [Tooltip("잠긴 지역이면 Locked Color로 어둡게 할 그림 (매장 그림)")]
        [SerializeField] Graphic body;
        [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        Color openColor = Color.white;

        public RegionState Region { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        public void Bind(RegionState region, ReadOnlyReactiveProperty<RegionState> current, Action<MiniMapStoreIcon> onClick)
        {
            Region = region;
            if (nameText != null)
                nameText.text = region.DisplayName;
            if (body != null)
                openColor = body.color;

            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => onClick(this));
            else
                Debug.LogError($"[MiniMapStoreIcon] {name}: Button을 연결하세요.", this);

            Observable.CombineLatest(region.IsOpen, region.IsCleared, current,
                    (open, cleared, now) => (open, cleared, isCurrent: now == region))
                .Subscribe(x => Refresh(x.open, x.cleared, x.isCurrent))
                .AddTo(this);
        }

        void Refresh(bool open, bool cleared, bool isCurrent)
        {
            if (lockedMark != null)
                lockedMark.SetActive(!open);
            if (clearedMark != null)
                clearedMark.SetActive(cleared);
            if (currentMark != null)
                currentMark.SetActive(isCurrent);
            if (body != null)
                body.color = open ? openColor : lockedColor;
        }
    }
}
