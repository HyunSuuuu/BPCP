using BurgerIdle.Core;
using BurgerIdle.Pickup;
using R3;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 일정 간격으로 음식을 생산해 출력 지점(PickupPile)에 쌓는다. 재고 상한에 도달하면 멈춘다.
    /// 초당 생산량·최대 재고는 업그레이드 스탯을 받아 매 프레임 반영한다.
    /// (해금 전 비활성 상태에서 Init되므로 구독 대신 현재 값을 읽는다)
    /// </summary>
    [RequireComponent(typeof(PickupPile))]
    public sealed class Machine : MonoBehaviour
    {
        [Header("Production")]
        [Tooltip("생산할 음식 프리팹 (Kind가 PickupPile Category와 맞아야 함)")]
        [SerializeField] PickupObject product;

        ReadOnlyReactiveProperty<float> producePerSecond;
        ReadOnlyReactiveProperty<float> maxStock;
        PickupPile output;
        float timer;

        void Awake() => output = GetComponent<PickupPile>();

        void Start()
        {
            if (product == null)
                Debug.LogWarning($"[Machine] {name}: Product 프리팹이 비어 있습니다.", this);
            else if (product.Category != output.Category)
                Debug.LogWarning($"[Machine] {name}: Product 종류({product.Category})와 PickupPile Category({output.Category})가 다릅니다.", this);
        }

        public void Init(ReadOnlyReactiveProperty<float> producePerSecond, ReadOnlyReactiveProperty<float> maxStock)
        {
            this.producePerSecond = producePerSecond;
            this.maxStock = maxStock;
        }

        void Update()
        {
            if (producePerSecond == null)
                return;

            var capacity = Mathf.RoundToInt(maxStock.CurrentValue);
            if (output.Capacity != capacity)
                output.SetCapacity(capacity);

            var rate = producePerSecond.CurrentValue;
            if (product == null || rate <= 0f || !output.CanAdd(product))
                return;

            timer += Time.deltaTime;
            if (timer < 1f / rate)
                return;

            timer = 0f;
            var item = ObjectPools.Get(product, transform.position + Vector3.up, Quaternion.identity);
            output.Add(item);
        }
    }
}
