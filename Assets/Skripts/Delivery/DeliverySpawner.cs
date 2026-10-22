using BurgerIdle.Areas;
using BurgerIdle.Core;
using BurgerIdle.Economy;
using BurgerIdle.UI;
using BurgerIdle.Unlock;
using R3;
using UnityEngine;

namespace BurgerIdle.Delivery
{
    /// <summary>
    /// 배달원을 보낸다. 지정한 시설 묶음이 모두 해금되면 시작하고, 한 번에 1명.
    /// 배달이 끝나면(성공·실패) Respawn Delay 뒤에 다음 배달원이 온다.
    /// 성공 보상(일반 판매가·경험치 × 수량 × 보상 배율, 돈은 수익 배율 적용)은 돈 더미 없이 바로 입금한다.
    /// </summary>
    public sealed class DeliverySpawner : MonoBehaviour
    {
        [Header("Spawn")]
        [Tooltip("배달원 프리팹 (DeliveryRider)")]
        [SerializeField] DeliveryRider prefab;
        [Tooltip("주문 품목·수량·제한 시간·보상 배율·재등장 시간")]
        [SerializeField] DeliveryConfig config;
        [Tooltip("배달원이 찾아갈 배달 지점")]
        [SerializeField] DeliveryPoint point;

        [Header("Route")]
        [SerializeField] Area area;
        [SerializeField] GateType entranceType = GateType.PeopleEntrance;
        [SerializeField] GateType exitType = GateType.PeopleExit;

        [Header("Open Condition")]
        [Tooltip("이 시설 묶음(UnlockZone)이 모두 해금되면 배달 시작. 보통 배달 지점이 있는 Zone의 Facilities.\n" +
                 "비워 두면 이 스포너가 속한 Zone의 Facilities, 그것도 없으면 바로 시작")]
        [SerializeField] UnlockZone openZone;

        CurrencyManager currency;
        PlayerLevel level;
        GameConfig gameConfig;
        ReadOnlyReactiveProperty<float> incomeMultiplier;
        NotificationPopup notifications;
        DeliveryRider current;
        bool isOpen;
        float timer;

        /// <param name="level">이 스포너가 속한 지역의 레벨 (배달 성공 경험치를 받음)</param>
        /// <param name="incomeMultiplier">수익 배율 (플레이어 업그레이드 × 지역 배율)</param>
        public void Init(CurrencyManager currency, PlayerLevel level, GameConfig gameConfig, ReadOnlyReactiveProperty<float> incomeMultiplier,
            NotificationPopup notifications)
        {
            if (prefab == null || config == null || point == null || area == null)
            {
                Debug.LogError($"[DeliverySpawner] {name}: Prefab, Config, Point, Area를 연결하세요.", this);
                enabled = false;
                return;
            }

            if (!area.TryGetGate(entranceType, out _) || !area.TryGetGate(exitType, out _))
            {
                Debug.LogError($"[DeliverySpawner] {name}: Area에 {entranceType}·{exitType} 출입문이 없습니다.", this);
                enabled = false;
                return;
            }

            this.currency = currency;
            this.level = level;
            this.gameConfig = gameConfig;
            this.incomeMultiplier = incomeMultiplier;
            this.notifications = notifications;
            timer = config.firstDelay;

            if (openZone == null)
                openZone = GetComponentInParent<Zone>(true)?.Facilities;

            if (openZone == null)
            {
                Debug.LogWarning($"[DeliverySpawner] {name}: Open Zone이 없어 바로 배달을 시작합니다.", this);
                isOpen = true;
            }
            else
                openZone.IsCompleted.Subscribe(completed => isOpen = completed).AddTo(this);
        }

        void Update()
        {
            if (currency == null || !isOpen || current != null || point.IsBusy || !point.isActiveAndEnabled)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            Spawn();
        }

        void Spawn()
        {
            area.TryGetGate(entranceType, out var entrance);
            area.TryGetGate(exitType, out var exit);

            current = ObjectPools.Get(prefab, entrance.OutsidePoint, entrance.EnterRotation);
            point.Reserve(current);
            var rider = current;
            rider.Begin(config, point, entrance.InsidePoint, exit.ExitRoute,
                () => notifications?.Show($"SPECIAL ORDER!\n{rider.Required.CurrentValue}개 배달"),
                OnFinished);
        }

        void OnFinished(bool success, int quantity)
        {
            current = null;
            timer = config.respawnDelay;

            if (!success)
            {
                notifications?.Show("배달 실패");
                return;
            }

            var money = Mathf.RoundToInt(gameConfig.PriceOf(config.orderKind) * quantity * config.rewardMultiplier);
            if (incomeMultiplier != null)
                money = CurrencyManager.ApplyMultiplier(money, incomeMultiplier.CurrentValue);
            var exp = Mathf.RoundToInt(gameConfig.ExpOf(config.orderKind) * quantity * config.rewardMultiplier);

            currency.Add(CurrencyType.Money, money);
            level.AddExp(exp);
            GameStats.AddSales(quantity);
            notifications?.Show($"배달 성공!\n+{money:N0}");
        }
    }
}
