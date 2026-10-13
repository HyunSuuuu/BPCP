using System.Collections.Generic;
using BurgerIdle.Pickup;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 물건을 채워 줘야 하는 곳 (카운터 재고, 드라이브스루 재고, 포장 테이블 투입 칸).
    /// 알바가 무엇을 어디로 운반할지 고를 때 쓴다.
    /// </summary>
    public interface ISupplyTarget
    {
        /// <summary>받는 종류</summary>
        PickupKind Kind { get; }
        /// <summary>지금 모자란 개수 (0이면 운반 불필요)</summary>
        int Shortage { get; }
        /// <summary>물건을 내려놓는 곳</summary>
        IPickupReceiver Receiver { get; }
    }

    /// <summary>활성화된 ISupplyTarget 목록.</summary>
    public static class SupplyTargets
    {
        static readonly List<ISupplyTarget> active = new List<ISupplyTarget>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public static IReadOnlyList<ISupplyTarget> Active => active;

        public static void Register(ISupplyTarget target) => active.Add(target);

        public static void Unregister(ISupplyTarget target) => active.Remove(target);

        /// <summary>pile에 내려놓는 PickupReceiver를 owner와 그 자식에서 찾는다.</summary>
        public static IPickupReceiver FindReceiver(Component owner, PickupPile pile)
        {
            foreach (var receiver in owner.GetComponentsInChildren<PickupReceiver>(true))
            {
                if (receiver.Pile == pile)
                    return receiver;
            }

            return null;
        }
    }
}
