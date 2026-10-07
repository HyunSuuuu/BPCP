using BurgerIdle.Core;
using BurgerIdle.Pickup;
using R3;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 일정 간격으로 음식을 생산해 출력 지점(PickupPile)에 쌓는다. 재고 상한에 도달하면 멈춘다.
    /// 초당 생산량·최대 재고는 업그레이드(P8)로 바뀐다.
    /// </summary>
    [RequireComponent(typeof(PickupPile))]
    public sealed class Machine : MonoBehaviour
    {
        [SerializeField] PickupObject product;

        readonly ReactiveProperty<float> producePerSecond = new ReactiveProperty<float>(0.5f);
        readonly ReactiveProperty<int> maxStock = new ReactiveProperty<int>(8);

        PickupPile output;
        float timer;

        public ReadOnlyReactiveProperty<float> ProducePerSecond => producePerSecond;
        public ReadOnlyReactiveProperty<int> MaxStock => maxStock;

        void Awake()
        {
            producePerSecond.AddTo(this);
            maxStock.AddTo(this);
            output = GetComponent<PickupPile>();
            maxStock.Subscribe(value => output.SetCapacity(value)).AddTo(this);
        }

        void Start()
        {
            if (product == null)
                Debug.LogWarning($"[Machine] {name}: Product 프리팹이 비어 있습니다.", this);
            else if (product.Category != output.Category)
                Debug.LogWarning($"[Machine] {name}: Product 종류({product.Category})와 PickupPile Category({output.Category})가 다릅니다.", this);
        }

        public void Init(float producePerSecond, int maxStock)
        {
            this.producePerSecond.Value = producePerSecond;
            this.maxStock.Value = maxStock;
        }

        void Update()
        {
            if (product == null || producePerSecond.Value <= 0f || !output.CanAdd(product))
                return;

            timer += Time.deltaTime;
            if (timer < 1f / producePerSecond.Value)
                return;

            timer = 0f;
            var item = ObjectPools.Get(product, transform.position + Vector3.up, Quaternion.identity);
            output.Add(item);
        }
    }
}
