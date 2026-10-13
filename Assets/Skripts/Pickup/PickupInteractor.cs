using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 주변의 픽업·내려놓기 지점과 자동으로 상호작용한다. 일정 간격마다 1개씩 처리하며 내려놓기가 우선이다.
    /// 플레이어와 알바가 같이 쓴다.
    /// </summary>
    [RequireComponent(typeof(CarryStack))]
    public sealed class PickupInteractor : MonoBehaviour
    {
        CarryStack stack;
        float interval = 0.1f;
        float cooldown;

        public void Init(float interval) => this.interval = interval;

        void Awake() => stack = GetComponent<CarryStack>();

        void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f)
                return;

            if (TryDrop() || TryPick())
                cooldown = interval;
        }

        /// <summary>범위 안의 받는 곳마다, 받을 수 있는 물건 중 가장 위에 있는 것을 내려놓는다.</summary>
        bool TryDrop()
        {
            if (stack.Peek() == null)
                return false;

            var position = transform.position;
            foreach (var receiver in InteractionRegistry.Receivers)
            {
                if (!receiver.Zone.Contains(position))
                    continue;

                var item = stack.FindFromTop(receiver.CanReceive);
                if (item == null)
                    continue;

                receiver.Receive(stack.Take(item));
                return true;
            }

            return false;
        }

        bool TryPick()
        {
            var position = transform.position;
            foreach (var provider in InteractionRegistry.Providers)
            {
                if (!provider.HasItem || !stack.CanAccept(provider.Category) || !provider.Zone.Contains(position))
                    continue;

                if (provider.TryTake(out var item))
                {
                    stack.Push(item);
                    return true;
                }
            }

            return false;
        }
    }
}
