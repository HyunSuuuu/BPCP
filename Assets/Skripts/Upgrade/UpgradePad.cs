using BurgerIdle.Economy;
using BurgerIdle.Pickup;
using UnityEngine;

namespace BurgerIdle.Upgrade
{
    /// <summary>
    /// 바닥 업그레이드 패드. 플레이어가 범위 안에 들어오면 업그레이드 패널을 열고, 벗어나면 닫는다.
    /// Categories로 이 패드에서 보여 줄 업그레이드 종류를 고른다 (비우면 전체).
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class UpgradePad : MonoBehaviour
    {
        [Tooltip("패널 제목 (예: 플레이어, 직원, 주방)")]
        [SerializeField] string title = "업그레이드";
        [Tooltip("이 패드에서 보여 줄 업그레이드 종류. 비우면 전체")]
        [SerializeField] UpgradeCategory[] categories;

        InteractionZone zone;
        Transform player;
        UpgradePanel panel;
        UpgradeManager upgrades;
        PlayerLevel playerLevel;
        bool inside;

        void Awake() => zone = GetComponent<InteractionZone>();

        /// <param name="upgrades">이 패드가 속한 지역의 업그레이드</param>
        /// <param name="playerLevel">이 패드가 속한 지역의 레벨</param>
        public void Init(Transform player, UpgradePanel panel, UpgradeManager upgrades, PlayerLevel playerLevel)
        {
            this.player = player;
            this.panel = panel;
            this.upgrades = upgrades;
            this.playerLevel = playerLevel;
        }

        void Update()
        {
            if (player == null || panel == null)
                return;

            var now = zone.Contains(player.position);
            if (now == inside)
                return;

            inside = now;
            if (inside)
                panel.Open(this, categories, title, upgrades, playerLevel);
            else
                panel.Close(this);
        }

        void OnDisable()
        {
            if (inside && panel != null)
                panel.Close(this);
            inside = false;
        }
    }
}
