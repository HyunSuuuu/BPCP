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

        bool TryDrop()
        {
            var top = stack.Peek();
            if (top == null)
                return false;

            var position = transform.position;
            foreach (var receiver in InteractionRegistry.Receivers)
            {
                if (!receiver.Zone.Contains(position) || !receiver.CanReceive(top))
                    continue;

                receiver.Receive(stack.Pop());
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
