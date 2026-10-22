using BurgerIdle.Economy;
using R3;
using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// HUD 진입점. HUD Canvas 루트에 붙인다. 인스펙터 연결 없이 자식의 화면 요소(MoneyView, LevelView,
    /// NotificationPopup)를 찾아 초기화한다. 화면 요소는 각자 프리팹으로 만들어 HUD Canvas 아래에 배치한다.
    /// 레벨은 지역마다 따로라서, 플레이어가 있는 지역의 레벨을 표시한다.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        /// <summary>알림 팝업. 다른 시스템이 알림을 띄울 때 쓴다 (없으면 null).</summary>
        public NotificationPopup Notifications { get; private set; }

        /// <param name="currentLevel">플레이어가 있는 지역의 레벨. 지역을 옮기면 바뀐다</param>
        public void Init(CurrencyManager currency, ReadOnlyReactiveProperty<PlayerLevel> currentLevel)
        {
            foreach (var view in GetComponentsInChildren<MoneyView>(true))
                view.Bind(currency);
            foreach (var view in GetComponentsInChildren<LevelView>(true))
                view.Bind(currentLevel);

            Notifications = GetComponentInChildren<NotificationPopup>(true);
            if (Notifications == null)
                return;

            // 지역을 옮길 때마다 새로 구독하므로 지역 간 레벨 차이는 레벨업으로 보지 않는다
            currentLevel
                .Select(level => level.Level.Pairwise().Where(pair => pair.Item2 > pair.Item1))
                .Switch()
                .Subscribe(pair => Notifications.Show($"LEVEL UP!\nLv {pair.Item2}"))
                .AddTo(this);
        }
    }
}
