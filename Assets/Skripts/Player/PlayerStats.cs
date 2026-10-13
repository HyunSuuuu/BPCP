using System;
using R3;
using UnityEngine;

namespace BurgerIdle.Player
{
    /// <summary>
    /// 플레이어 스탯. 업그레이드 스탯(UpgradeManager.Stat)을 그대로 받아, 업그레이드하면 구독자가 자동 반영한다.
    /// </summary>
    public sealed class PlayerStats : IDisposable
    {
        readonly ReadOnlyReactiveProperty<int> maxCarry;

        public ReadOnlyReactiveProperty<float> MoveSpeed { get; }
        public ReadOnlyReactiveProperty<int> MaxCarry => maxCarry;

        public PlayerStats(ReadOnlyReactiveProperty<float> moveSpeed, ReadOnlyReactiveProperty<float> maxCarry)
        {
            MoveSpeed = moveSpeed;
            this.maxCarry = maxCarry.Select(Mathf.RoundToInt).ToReadOnlyReactiveProperty();
        }

        public void Dispose() => maxCarry.Dispose();
    }
}
