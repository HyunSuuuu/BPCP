using BurgerIdle.Pickup;
using BurgerIdle.Station;
using UnityEngine;

namespace BurgerIdle.Delivery
{
    /// <summary>
    /// 배달 지점. 평소에는 아무것도 받지 않다가, 배달원이 도착해 주문을 시작하면 그동안만
    /// 받는 곳(IPickupReceiver)이자 채워 줘야 하는 곳(ISupplyTarget)이 된다 (임시 판매대).
    /// 플레이어는 근처에 가면 자동으로 제출하고, 알바는 운반 작업으로 채운다.
    /// 받은 물건은 배달원에게 넘긴다. 범위는 같은 오브젝트의 InteractionZone.
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class DeliveryPoint : MonoBehaviour, IPickupReceiver, ISupplyTarget
    {
        [Tooltip("배달원이 서는 위치·방향. 비우면 이 오브젝트")]
        [SerializeField] Transform standPoint;

        InteractionZone zone;
        DeliveryRider rider;
        bool isOpen;

        public InteractionZone Zone => zone != null ? zone : zone = GetComponent<InteractionZone>();
        Transform Stand => standPoint != null ? standPoint : transform;
        public Vector3 StandPosition => Stand.position;
        public Quaternion StandRotation => Stand.rotation;

        /// <summary>배달원이 오는 중이거나 주문 중</summary>
        public bool IsBusy => rider != null;

        // ISupplyTarget
        public PickupKind Kind => rider != null ? rider.OrderKind : PickupKind.Burger;
        public int Shortage => isOpen && rider != null ? rider.Remaining : 0;
        public IPickupReceiver Receiver => this;
        public SupplyPriority Priority => rider != null && rider.IsUrgent ? SupplyPriority.Urgent : SupplyPriority.Order;

        void OnDisable() => Close();

        /// <summary>스포너가 배달원을 보낼 때 자리를 맡는다.</summary>
        public void Reserve(DeliveryRider newRider) => rider = newRider;

        /// <summary>배달원이 도착해 주문을 시작하면 받기 시작한다.</summary>
        public void Open()
        {
            if (isOpen)
                return;
            isOpen = true;
            InteractionRegistry.Register(this);
            SupplyTargets.Register(this);
        }

        /// <summary>주문이 끝나면(성공·실패) 받기를 멈추고 자리를 비운다.</summary>
        public void Release(DeliveryRider leaving)
        {
            if (rider != leaving)
                return;
            Close();
            rider = null;
        }

        void Close()
        {
            if (!isOpen)
                return;
            isOpen = false;
            InteractionRegistry.Unregister(this);
            SupplyTargets.Unregister(this);
        }

        public bool CanReceive(PickupObject item) => isOpen && rider != null && rider.Accepts(item);

        public void Receive(PickupObject item) => rider.Submit(item);
    }
}
