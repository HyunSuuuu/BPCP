using BurgerIdle.Economy;
using R3;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// HUD 진입점. HUD Canvas 루트에 붙인다. 인스펙터 연결 없이 자식의 화면 요소(MoneyView, LevelView,
    /// NotificationPopup)를 찾아 초기화한다. 화면 요소는 각자 프리팹으로 만들어 HUD Canvas 아래에 배치한다.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        /// <summary>알림 팝업. 다른 시스템이 알림을 띄울 때 쓴다 (없으면 null).</summary>
        public NotificationPopup Notifications { get; private set; }

        public void Init(CurrencyManager currency, PlayerLevel playerLevel)
        {
            foreach (var view in GetComponentsInChildren<MoneyView>(true))
                view.Bind(currency);
            foreach (var view in GetComponentsInChildren<LevelView>(true))
                view.Bind(playerLevel);

            Notifications = GetComponentInChildren<NotificationPopup>(true);
            if (Notifications == null)
                return;

            playerLevel.Level
                .Pairwise()
                .Where(pair => pair.Item2 > pair.Item1)
                .Subscribe(pair => Notifications.Show($"LEVEL UP!\nLv {pair.Item2}"))
                .AddTo(this);
        }
    }
}
