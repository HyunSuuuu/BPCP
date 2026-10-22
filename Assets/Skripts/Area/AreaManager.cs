using System;
using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Economy;
using BurgerIdle.UI;
using R3;
using UnityEngine;

namespace BurgerIdle.Areas
{
    /// <summary>
    /// 지역(Area) 여러 개를 관리한다. Areas 오브젝트에 붙이고, 자식 Area를 Hierarchy 순서대로 쓴다 (지역 수는 자식 수로 정해짐).
    /// - 플레이어가 있는 지역 하나만 켜 두고, 나머지는 숨겨 멈춘다. 돌아오면 떠날 때 상태 그대로 움직인다.
    /// - 지역 레벨이 최대 레벨에 닿으면 클리어 → 다음 지역을 바로 연다 (자동 이동 없음). 마지막 지역 클리어 = 데모 완료.
    /// - 클리어한 지역은 플레이어가 떠나 있는 동안 지속 수입이 쌓이고, 미니맵에서 수거한다.
    /// </summary>
    public sealed class AreaManager : MonoBehaviour
    {
        [Header("Passive Income")]
        [Tooltip("지속 수입 적립 상한 (분). 분당 수입 × 이 값까지만 쌓인다")]
        [SerializeField, Min(0f)] float incomeCapMinutes = 120f;

        [Header("Messages")]
        [Tooltip("다음 지역이 열릴 때 알림. {0} = 지역 이름")]
        [SerializeField] string openMessageFormat = "새 지역 오픈!\n{0}";
        [Tooltip("마지막 지역을 클리어했을 때 알림")]
        [SerializeField] string demoCompleteMessage = "데모 완료!";

        readonly List<RegionState> regions = new List<RegionState>();
        readonly ReactiveProperty<RegionState> current = new ReactiveProperty<RegionState>();
        readonly ReactiveProperty<bool> isFinished = new ReactiveProperty<bool>(false);

        CurrencyManager currency;
        Hud hud;
        Action<RegionState> onEnter;

        public IReadOnlyList<RegionState> Regions => regions;
        /// <summary>플레이어가 있는 지역</summary>
        public ReadOnlyReactiveProperty<RegionState> Current => current;
        /// <summary>마지막 지역을 클리어했다 = 데모 완료</summary>
        public ReadOnlyReactiveProperty<bool> IsFinished => isFinished;
        public float IncomeCapMinutes => incomeCapMinutes;

        void Awake()
        {
            current.AddTo(this);
            isFinished.AddTo(this);
        }

        /// <summary>자식 Area (Hierarchy 순서). GameBootstrap이 지역 데이터를 만들 때 쓴다.</summary>
        public Area[] FindAreas() => GetComponentsInChildren<Area>(true);

        /// <summary>
        /// 첫 지역만 켜고 나머지는 숨긴다. 다른 초기화(NavMesh 굽기 등)보다 먼저 부른다.
        /// </summary>
        /// <param name="onEnter">다른 지역으로 옮긴 직후 호출 (NavMesh 준비, 플레이어 순간이동 등). 이때 새 지역은 이미 켜져 있다</param>
        public void Init(IReadOnlyList<RegionState> states, CurrencyManager currency, Hud hud, Action<RegionState> onEnter)
        {
            this.currency = currency;
            this.hud = hud;
            this.onEnter = onEnter;

            regions.Clear();
            regions.AddRange(states);
            if (regions.Count == 0)
            {
                Debug.LogError($"[AreaManager] {name}: 자식에 Area가 없습니다.", this);
                return;
            }

            for (var i = 0; i < regions.Count; i++)
            {
                regions[i].Area.gameObject.SetActive(i == 0);

                var index = i;
                regions[i].IsCleared
                    .Where(cleared => cleared)
                    .Take(1)
                    .Subscribe(_ => OnCleared(index))
                    .AddTo(this);
            }

            regions[0].Open();
            ObjectPools.ActiveParent = regions[0].Area.transform;
            current.Value = regions[0];
        }

        void Update()
        {
            // 실제 시간 기준 (미니맵이 열려 게임이 멈춘 동안에도 쌓임)
            var dt = Time.unscaledDeltaTime;
            foreach (var region in regions)
            {
                if (region != current.Value)
                    region.Accrue(dt, incomeCapMinutes);
            }
        }

        void OnCleared(int index)
        {
            Debug.Log($"[AreaManager] {regions[index].DisplayName} 클리어");

            var next = index + 1;
            if (next < regions.Count)
            {
                regions[next].Open();
                Notify(string.Format(openMessageFormat, regions[next].DisplayName));
                return;
            }

            isFinished.Value = true;
            Notify(demoCompleteMessage);
        }

        /// <summary>
        /// 지역으로 이동한다. 떠나는 지역은 숨겨 멈추고, 도착 지역을 켠다. 지금 지역이거나 열리지 않은 지역이면 false.
        /// </summary>
        public bool Enter(RegionState target)
        {
            var from = current.Value;
            if (target == null || target == from || !target.IsOpen.CurrentValue)
                return false;

            from.Area.gameObject.SetActive(false);
            target.Area.gameObject.SetActive(true);
            ObjectPools.ActiveParent = target.Area.transform;

            onEnter?.Invoke(target);
            current.Value = target;
            Debug.Log($"[AreaManager] {target.DisplayName}(으)로 이동");
            return true;
        }

        /// <summary>쌓인 지속 수입을 받는다 (바로 입금). 받은 금액을 반환한다.</summary>
        public int Collect(RegionState region)
        {
            if (region == null || currency == null)
                return 0;

            var amount = region.TakeIncome();
            if (amount > 0)
                currency.Add(CurrencyType.Money, amount);
            return amount;
        }

        void Notify(string message)
        {
            if (!string.IsNullOrEmpty(message) && hud != null && hud.Notifications != null)
                hud.Notifications.Show(message);
        }
    }
}
