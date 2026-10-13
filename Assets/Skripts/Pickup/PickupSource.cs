using UnityEngine;

namespace BurgerIdle.Pickup
{
    /// <summary>
    /// 범위 안의 캐릭터가 PickupPile에서 물건을 집어 가게 한다. (기계 출력 지점, 테이블 위 쓰레기)
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class PickupSource : MonoBehaviour, IPickupProvider
    {
        [Tooltip("비워 두면 같은 오브젝트의 PickupPile을 사용한다.")]
        [SerializeField] PickupPile pile;

        public InteractionZone Zone { get; private set; }
        public PickupCategory Category => pile.Category;
        public bool HasItem => pile.Count.CurrentValue > 0;
        public bool HasKind(PickupKind kind) => pile.TopKind == kind;

        void Awake()
        {
            Zone = GetComponent<InteractionZone>();
            if (pile == null)
                pile = GetComponent<PickupPile>();
        }

        void OnEnable() => InteractionRegistry.Register(this);

        void OnDisable() => InteractionRegistry.Unregister(this);

        public bool TryTake(out PickupObject item) => pile.TryTakeTop(out item);
    }
}
