using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 범위 안의 캐릭터가 들고 있는 물건을 PickupPile에 내려놓게 한다. (카운터 투입 지점)
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class PickupReceiver : MonoBehaviour, IPickupReceiver
    {
        [Tooltip("비워 두면 같은 오브젝트의 PickupPile을 사용한다.")]
        [SerializeField] PickupPile pile;

        public InteractionZone Zone { get; private set; }
        public PickupPile Pile => pile != null ? pile : pile = GetComponent<PickupPile>();

        void Awake()
        {
            Zone = GetComponent<InteractionZone>();
            if (pile == null)
                pile = GetComponent<PickupPile>();
        }

        void OnEnable() => InteractionRegistry.Register(this);

        void OnDisable() => InteractionRegistry.Unregister(this);

        public bool CanReceive(PickupObject item) => pile.CanAdd(item);

        public void Receive(PickupObject item) => pile.Add(item);
    }
}
