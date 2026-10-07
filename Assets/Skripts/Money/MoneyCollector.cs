using BurgerIdle.Economy;
using DG.Tweening;
using UnityEngine;

namespace BurgerIdle.Money
{
    /// <summary>
    /// 플레이어 전용. 돈 더미 범위를 지나가면 위층부터 하나씩 끌어당겨 수거하고, 도착 시 소지금에 더한다.
    /// </summary>
    public sealed class MoneyCollector : MonoBehaviour
    {
        const float FlyDuration = 0.25f;
        const float ArcHeight = 1f;
        static readonly Vector3 TargetOffset = Vector3.up * 1.2f;

        CurrencyManager currency;
        float interval = 0.05f;
        float cooldown;

        public void Init(CurrencyManager currency, float interval)
        {
            this.currency = currency;
            this.interval = interval;
        }

        void Update()
        {
            if (currency == null)
                return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f)
                return;

            var position = transform.position;
            foreach (var pile in MoneyPile.Active)
            {
                if (!pile.HasMoney || !pile.Zone.Contains(position) || !pile.TryTakeTop(out var money))
                    continue;

                Fly(money);
                cooldown = interval;
                return;
            }
        }

        void Fly(MoneyObject money)
        {
            var t = money.transform;
            var start = t.position;
            var amount = money.Amount;

            // 이동 중인 플레이어를 따라가도록 매 프레임 목표 위치를 다시 계산한다
            DOTween.To(() => 0f, progress =>
                {
                    var end = transform.position + TargetOffset;
                    t.position = Vector3.Lerp(start, end, progress) + Vector3.up * (ArcHeight * Mathf.Sin(progress * Mathf.PI));
                }, 1f, FlyDuration)
                .SetEase(Ease.InQuad)
                .SetTarget(t)
                .OnComplete(() =>
                {
                    currency.Add(CurrencyType.Money, amount);
                    money.Release();
                });
        }
    }
}
