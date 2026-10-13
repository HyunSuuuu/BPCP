using System.Collections.Generic;
using BurgerIdle.Economy;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerIdle.Upgrade
{
    /// <summary>
    /// 업그레이드 카드 한 장. 이름, 아이콘, 업그레이드 횟수 칸, 광고 버튼, 돈 버튼을 표시한다.
    /// 사용자가 만든 카드 프리팹 루트에 붙인다. 패널이 열릴 때 생성되고 닫힐 때 제거된다.
    /// 연결하지 않은 칸은 무시한다.
    /// </summary>
    public sealed class UpgradeEntryView : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image iconImage;
        [Tooltip("\"현재 → 다음 값\" (선택)")]
        [SerializeField] TMP_Text valueText;
        [Tooltip("레벨 부족 시 \"Lv N 필요\" (선택)")]
        [SerializeField] TMP_Text lockText;

        [Header("Level Pips")]
        [Tooltip("업그레이드 횟수 칸이 생성될 부모 (Horizontal Layout Group 권장)")]
        [SerializeField] Transform pipContainer;
        [Tooltip("칸 하나 (작은 사각형 Image 프리팹). 최대 레벨 개수만큼 생성")]
        [SerializeField] Image pipPrefab;
        [SerializeField] Color pipFilled = new Color(1f, 0.6f, 0.2f);
        [SerializeField] Color pipEmpty = new Color(0.75f, 0.75f, 0.75f);

        [Header("Ad Button")]
        [SerializeField] Button adButton;
        [Tooltip("\"무료\" / \"광고 중 N\" / 대기 \"N초\"")]
        [SerializeField] TMP_Text adText;

        [Header("Money Button")]
        [SerializeField] Button moneyButton;
        [SerializeField] TMP_Text costText;

        readonly List<Image> pips = new List<Image>();

        UpgradeDefinition definition;
        UpgradeManager manager;
        int money;
        bool watchingAd;
        float adEndTime;

        public void Bind(UpgradeDefinition definition, UpgradeManager manager, CurrencyManager currency, PlayerLevel playerLevel)
        {
            this.definition = definition;
            this.manager = manager;

            if (nameText != null)
                nameText.text = definition.displayName;
            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = definition.icon != null;
            }

            CreatePips();

            Observable.CombineLatest(manager.LevelOf(definition), currency.Money, playerLevel.Level,
                    (level, money, playerLv) => (level, money))
                .Subscribe(x =>
                {
                    money = x.money;
                    Refresh(x.level);
                })
                .AddTo(this);

            if (moneyButton != null)
                moneyButton.onClick.AddListener(OnMoneyClicked);
            if (adButton != null)
                adButton.onClick.AddListener(OnAdClicked);
        }

        void Update()
        {
            // 광고 시청·대기 시간 표시는 시간에 따라 바뀌므로 매 프레임 갱신
            if (definition != null && adButton != null)
                RefreshAdButton();
        }

        void OnMoneyClicked()
        {
            if (manager.TryPurchase(definition))
                Punch();
        }

        void OnAdClicked()
        {
            if (watchingAd || !manager.CanUpgradeByAd(definition))
                return;

            watchingAd = true;
            adEndTime = Time.unscaledTime + manager.AdDuration;
            // 시청 중에 패널이 닫혀도(카드가 사라져도) 보상은 지급한다
            var target = definition;
            var upgrades = manager;
            DOVirtual.DelayedCall(upgrades.AdDuration, () =>
            {
                var upgraded = upgrades.TryUpgradeByAd(target);
                if (this == null)
                    return;
                watchingAd = false;
                if (upgraded)
                    Punch();
            }, ignoreTimeScale: true);
        }

        void CreatePips()
        {
            if (pipContainer == null || pipPrefab == null)
                return;

            for (var i = 0; i < definition.maxLevel; i++)
            {
                var pip = Instantiate(pipPrefab, pipContainer);
                pip.gameObject.SetActive(true);
                pips.Add(pip);
            }
        }

        void Refresh(int level)
        {
            var maxed = manager.IsMaxed(definition);
            var locked = manager.IsLocked(definition);
            var cost = manager.CurrentCost(definition);

            for (var i = 0; i < pips.Count; i++)
                pips[i].color = i < level ? pipFilled : pipEmpty;

            if (valueText != null)
            {
                var current = manager.ValueAt(definition, level).ToString(definition.valueFormat);
                valueText.text = maxed ? current : $"{current} → {manager.ValueAt(definition, level + 1).ToString(definition.valueFormat)}";
            }

            if (lockText != null)
            {
                lockText.gameObject.SetActive(locked && !maxed);
                lockText.text = $"Lv {definition.requiredPlayerLevel} 필요";
            }

            if (costText != null)
                costText.text = maxed ? "MAX" : cost.ToString("N0");
            if (moneyButton != null)
                moneyButton.interactable = !maxed && !locked && money >= cost;

            RefreshAdButton();
        }

        void RefreshAdButton()
        {
            if (adButton == null)
                return;

            var cooldown = manager.AdCooldownRemaining(definition);
            adButton.interactable = !watchingAd && manager.CanUpgradeByAd(definition);

            if (adText == null)
                return;

            if (watchingAd)
                adText.text = $"광고 중 {Mathf.CeilToInt(adEndTime - Time.unscaledTime)}";
            else if (manager.IsMaxed(definition))
                adText.text = "MAX";
            else if (cooldown > 0f)
                adText.text = $"{Mathf.CeilToInt(cooldown)}초";
            else
                adText.text = "무료";
        }

        void Punch()
        {
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.5f).SetLink(gameObject);
        }
    }
}
