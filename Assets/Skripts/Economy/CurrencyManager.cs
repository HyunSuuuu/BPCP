using System;
using R3;
using UnityEngine;

namespace BurgerIdle.Economy
{
    /// <summary>경험치는 지역마다 따로라서 재화가 아니라 지역의 PlayerLevel이 가진다.</summary>
    public enum CurrencyType
    {
        Money,
    }

    /// <summary>
    /// 돈의 단일 진입점. 모든 지역이 지갑 하나를 공유한다. 증감과 음수·최대값 검증은 모두 이 클래스 안에서만 처리한다.
    /// </summary>
    public sealed class CurrencyManager : IDisposable
    {
        readonly ReactiveProperty<int> money;
        readonly int maxValue;

        public ReadOnlyReactiveProperty<int> Money => money;

        public CurrencyManager(int startMoney, int maxValue = int.MaxValue)
        {
            this.maxValue = maxValue;
            money = new ReactiveProperty<int>(Mathf.Clamp(startMoney, 0, maxValue));
        }

        public ReadOnlyReactiveProperty<int> Get(CurrencyType type) => Property(type);

        /// <summary>수익 배율을 곱해 반올림한다 (0.5는 올림). 예: 3 × 1.5 = 4.5 → 5</summary>
        public static int ApplyMultiplier(int amount, float multiplier) =>
            (int)Math.Round(amount * (double)multiplier, MidpointRounding.AwayFromZero);

        public void Add(CurrencyType type, int amount)
        {
            if (!IsValidAmount(amount))
                return;

            var property = Property(type);
            // long으로 더해 오버플로를 막는다
            property.Value = (int)Math.Min((long)property.Value + amount, maxValue);
        }

        /// <summary>전액을 낼 수 있을 때만 차감한다.</summary>
        public bool TrySpend(CurrencyType type, int amount)
        {
            if (!IsValidAmount(amount))
                return false;

            var property = Property(type);
            if (property.Value < amount)
                return false;

            property.Value -= amount;
            return true;
        }

        /// <summary>가진 돈 범위 안에서 최대 max까지 차감하고, 실제 차감한 금액을 반환한다. (해금 박스 부분 지불용)</summary>
        public int SpendUpTo(int max)
        {
            if (!IsValidAmount(max))
                return 0;

            var spent = Mathf.Min(money.Value, max);
            money.Value -= spent;
            return spent;
        }

        public void Dispose() => money.Dispose();

        ReactiveProperty<int> Property(CurrencyType type) => type switch
        {
            CurrencyType.Money => money,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        static bool IsValidAmount(int amount)
        {
            if (amount >= 0)
                return true;

            Debug.LogWarning($"[CurrencyManager] 음수 금액은 처리하지 않습니다: {amount}");
            return false;
        }
    }
}
