using System.Collections.Generic;
using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>픽업 오브젝트를 내주는 곳 (기계 출력 지점, 테이블 위 쓰레기 등).</summary>
    public interface IPickupProvider
    {
        InteractionZone Zone { get; }
        PickupCategory Category { get; }
        bool HasItem { get; }
        bool TryTake(out PickupObject item);
    }

    /// <summary>픽업 오브젝트를 받는 곳 (카운터 투입 지점, 쓰레기통 등).</summary>
    public interface IPickupReceiver
    {
        InteractionZone Zone { get; }
        bool CanReceive(PickupObject item);
        void Receive(PickupObject item);
    }

    /// <summary>
    /// 활성화된 픽업 지점·내려놓기 지점 목록. 캐릭터가 주변 지점을 찾을 때 쓴다.
    /// </summary>
    public static class InteractionRegistry
    {
        static readonly List<IPickupProvider> providers = new List<IPickupProvider>();
        static readonly List<IPickupReceiver> receivers = new List<IPickupReceiver>();

        public static IReadOnlyList<IPickupProvider> Providers => providers;
        public static IReadOnlyList<IPickupReceiver> Receivers => receivers;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            providers.Clear();
            receivers.Clear();
        }

        public static void Register(IPickupProvider provider) => providers.Add(provider);
        public static void Unregister(IPickupProvider provider) => providers.Remove(provider);
        public static void Register(IPickupReceiver receiver) => receivers.Add(receiver);
        public static void Unregister(IPickupReceiver receiver) => receivers.Remove(receiver);
    }
}
