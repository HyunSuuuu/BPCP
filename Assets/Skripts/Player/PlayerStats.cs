using System;
using R3;

namespace BurgerIdle.Player
{
    /// <summary>
    /// 플레이어 스탯. 업그레이드(P8)가 값을 바꾸면 구독자가 자동 반영한다.
    /// </summary>
    public sealed class PlayerStats : IDisposable
    {
        readonly ReactiveProperty<float> moveSpeed;
        readonly ReactiveProperty<int> maxCarry;

        public ReadOnlyReactiveProperty<float> MoveSpeed => moveSpeed;
        public ReadOnlyReactiveProperty<int> MaxCarry => maxCarry;

        public PlayerStats(float moveSpeed, int maxCarry)
        {
            this.moveSpeed = new ReactiveProperty<float>(moveSpeed);
            this.maxCarry = new ReactiveProperty<int>(maxCarry);
        }

        public void Dispose()
        {
            moveSpeed.Dispose();
            maxCarry.Dispose();
        }
    }
}
