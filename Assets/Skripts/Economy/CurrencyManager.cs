using System;
using R3;
using UnityEngine;

namespace BurgerIdle.Economy
{
    public enum CurrencyType
    {
        Money,
        Exp,
    }

    /// <summary>
    /// 돈·경험치의 단일 진입점. 증감과 음수·최대값 검증은 모두 이 클래스 안에서만 처리한다.
    /// </summary>
    public sealed class CurrencyManager : IDisposable
    {
        readonly ReactiveProperty<int> money;
        readonly ReactiveProperty<int> exp;
        readonly int maxValue;

        public ReadOnlyReactiveProperty<int> Money => money;
        public ReadOnlyReactiveProperty<int> Exp => exp;

        public CurrencyManager(int startMoney, int maxValue = int.MaxValue)
        {
            this.maxValue = maxValue;
            money = new ReactiveProperty<int>(Mathf.Clamp(startMoney, 0, maxValue));
            exp = new ReactiveProperty<int>(0);
        }

        public ReadOnlyReactiveProperty<int> Get(CurrencyType type) => Property(type);

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

        public void Dispose()
        {
            money.Dispose();
            exp.Dispose();
        }

        ReactiveProperty<int> Property(CurrencyType type) => type switch
        {
            CurrencyType.Money => money,
            CurrencyType.Exp => exp,
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
