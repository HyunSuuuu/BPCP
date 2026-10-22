using System;
using R3;
using UnityEngine;

namespace BurgerIdle.Economy
{
    /// <summary>
    /// 지역 하나의 경험치와 레벨. 지역(RegionState)마다 하나씩 있고, 그 지역에서 얻은 경험치만 쌓인다.
    /// 업그레이드·해금 박스의 레벨 조건과 HUD가 구독한다.
    /// 레벨 = max(경험치 레벨, 해금으로 받은 레벨). 받은 레벨이 1 미만이면(시작 레벨 0) 경험치와 관계없이 0.
    /// 최대 레벨(= 지역 클리어)에 닿으면 경험치가 더 쌓이지 않는다.
    /// </summary>
    public sealed class PlayerLevel : IDisposable
    {
        readonly LevelTable table;
        readonly ReactiveProperty<int> exp = new ReactiveProperty<int>(0);
        readonly ReactiveProperty<int> level = new ReactiveProperty<int>(1);
        readonly ReactiveProperty<int> expInLevel = new ReactiveProperty<int>(0);
        readonly ReactiveProperty<int> expToNext = new ReactiveProperty<int>(1);
        readonly ReactiveProperty<float> progress = new ReactiveProperty<float>(0f);
        readonly IDisposable subscription;
        int grantedLevel;

        /// <summary>누적 경험치</summary>
        public ReadOnlyReactiveProperty<int> Exp => exp;
        public ReadOnlyReactiveProperty<int> Level => level;
        /// <summary>현재 레벨에서 모은 경험치</summary>
        public ReadOnlyReactiveProperty<int> ExpInLevel => expInLevel;
        /// <summary>다음 레벨까지 필요한 경험치 (최대 레벨이면 0)</summary>
        public ReadOnlyReactiveProperty<int> ExpToNext => expToNext;
        /// <summary>현재 레벨에서 다음 레벨까지의 진행도 (0~1). 최대 레벨이면 1.</summary>
        public ReadOnlyReactiveProperty<float> Progress => progress;
        public int MaxLevel => table.maxLevel;
        public bool IsMaxLevel => level.Value >= table.maxLevel;

        public PlayerLevel(LevelTable table)
        {
            this.table = table;
            grantedLevel = table.startLevel;
            subscription = exp.Subscribe(Recalculate);
        }

        /// <summary>경험치를 더한다. 최대 레벨이면 무시.</summary>
        public void AddExp(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[PlayerLevel] 음수 경험치는 처리하지 않습니다: {amount}");
                return;
            }

            if (amount == 0 || IsMaxLevel)
                return;

            // long으로 더해 오버플로를 막는다
            exp.Value = (int)Math.Min((long)exp.Value + amount, int.MaxValue);
        }

        /// <summary>레벨을 최소 target까지 올린다 (해금 보상). 이미 높으면 무시.</summary>
        public void RaiseTo(int target)
        {
            if (target <= grantedLevel)
                return;

            grantedLevel = Math.Min(target, table.maxLevel);
            Recalculate(exp.Value);
        }

        void Recalculate(int totalExp)
        {
            // 레벨 0: 매장을 열기 전. 경험치는 쌓아 두고 레벨에는 반영하지 않는다
            if (grantedLevel < 1)
            {
                expToNext.Value = table.ExpToNext(1);
                expInLevel.Value = 0;
                progress.Value = 0f;
                level.Value = 0;
                return;
            }

            var current = 1;
            var remaining = totalExp;
            while (current < table.maxLevel)
            {
                var need = table.ExpToNext(current);
                if (remaining < need)
                    break;
                remaining -= need;
                current++;
            }

            // 해금으로 받은 레벨이 경험치 레벨보다 높으면 그 레벨의 처음부터 센다
            if (grantedLevel > current)
            {
                current = grantedLevel;
                remaining = 0;
            }

            var maxed = current >= table.maxLevel;
            // 레벨을 먼저 바꾸면 구독자가 새 레벨과 이전 진행도를 함께 보게 되므로 진행 값부터 갱신한다
            expToNext.Value = maxed ? 0 : table.ExpToNext(current);
            expInLevel.Value = maxed ? 0 : remaining;
            progress.Value = maxed ? 1f : remaining / (float)expToNext.Value;
            level.Value = current;
        }

        public void Dispose()
        {
            subscription.Dispose();
            exp.Dispose();
            level.Dispose();
            expInLevel.Dispose();
            expToNext.Dispose();
            progress.Dispose();
        }
    }
}
