using System.Collections.Generic;
using BurgerIdle.Economy;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.Upgrade
{
    /// <summary>
    /// 업그레이드 패널. 업그레이드 패드에 들어가면 그 패드의 종류에 해당하는 항목으로 열리고, 벗어나면 닫힌다.
    /// 사용자가 만든 HUD Canvas 안의 패널 오브젝트에 붙인다. 카드는 Entry Prefab으로 Content 아래에 가로로 생성한다.
    /// 패널은 모든 지역이 하나를 같이 쓰고, 열 때 패드가 자기 지역의 업그레이드·레벨을 넘긴다.
    /// </summary>
    public sealed class UpgradePanel : MonoBehaviour
    {
        const float TweenDuration = 0.2f;

        [Header("Layout")]
        [Tooltip("열고 닫을 창. 이 컴포넌트가 붙은 오브젝트와 다르게(자식으로) 둔다")]
        [SerializeField] RectTransform window;
        [SerializeField] CanvasGroup canvasGroup;
        [Tooltip("카드가 생성될 부모 (Horizontal Layout Group)")]
        [SerializeField] Transform content;
        [Tooltip("패드의 Title을 표시 (선택)")]
        [SerializeField] TMP_Text titleText;
        [Tooltip("X 버튼. 누르면 닫히고, 패드를 벗어났다 다시 들어오면 다시 열림 (선택)")]
        [SerializeField] Button closeButton;

        [Header("Prefab")]
        [Tooltip("업그레이드 카드 프리팹 (UpgradeEntryView)")]
        [SerializeField] UpgradeEntryView entryPrefab;

        readonly List<UpgradeEntryView> entries = new List<UpgradeEntryView>();

        UpgradeManager manager;
        CurrencyManager currency;
        PlayerLevel playerLevel;
        object owner;

        public void Init(CurrencyManager currency)
        {
            this.currency = currency;

            if (window == null || canvasGroup == null || content == null || entryPrefab == null)
            {
                Debug.LogError($"[UpgradePanel] {name}: Window, Canvas Group, Content, Entry Prefab을 연결하세요.", this);
                enabled = false;
                return;
            }

            window.gameObject.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(() => Close(owner));
        }

        /// <summary>categories가 비어 있으면 모든 항목을 보여 준다.</summary>
        /// <param name="manager">요청한 패드가 속한 지역의 업그레이드</param>
        /// <param name="playerLevel">요청한 패드가 속한 지역의 레벨</param>
        public void Open(object requester, IReadOnlyCollection<UpgradeCategory> categories, string title,
            UpgradeManager manager, PlayerLevel playerLevel)
        {
            if (!enabled || currency == null || manager == null)
                return;

            this.manager = manager;
            this.playerLevel = playerLevel;

            // 편집 중 꺼 둔 상태로 저장했어도 열리도록 패널 루트를 켠다
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            owner = requester;
            if (titleText != null)
                titleText.text = title;
            Rebuild(categories);

            window.DOKill();
            canvasGroup.DOKill();
            window.gameObject.SetActive(true);
            window.localScale = Vector3.one * 0.85f;
            canvasGroup.alpha = 0f;
            window.DOScale(1f, TweenDuration).SetEase(Ease.OutBack).SetLink(gameObject);
            canvasGroup.DOFade(1f, TweenDuration).SetLink(gameObject);
        }

        /// <summary>패널을 연 패드만 닫을 수 있다.</summary>
        public void Close(object requester)
        {
            if (!enabled || owner != requester)
                return;

            owner = null;
            window.DOKill();
            canvasGroup.DOKill();
            window.DOScale(0.85f, TweenDuration).SetEase(Ease.InBack).SetLink(gameObject);
            canvasGroup.DOFade(0f, TweenDuration).SetLink(gameObject)
                .OnComplete(() => window.gameObject.SetActive(false));
        }

        void Rebuild(IReadOnlyCollection<UpgradeCategory> categories)
        {
            foreach (var entry in entries)
                Destroy(entry.gameObject);
            entries.Clear();

            foreach (var definition in manager.Definitions)
            {
                if (categories != null && categories.Count > 0 && !Contains(categories, definition.category))
                    continue;

                var entry = Instantiate(entryPrefab, content);
                entry.Bind(definition, manager, currency, playerLevel);
                entries.Add(entry);
            }
        }

        static bool Contains(IReadOnlyCollection<UpgradeCategory> categories, UpgradeCategory category)
        {
            foreach (var c in categories)
            {
                if (c == category)
                    return true;
            }

            return false;
        }
    }
}
