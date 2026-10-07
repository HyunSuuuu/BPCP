using System.Collections.Generic;
using BurgerIdle.Core;
using BurgerIdle.Pickup;
using DG.Tweening;
using R3;
using UnityEngine;

namespace BurgerIdle.Money
{
    /// <summary>
    /// 돈 더미. 판매 수익·팁이 돈 오브젝트로 쌓이고, 플레이어가 지나가면 수거된다.
    /// 한 층은 columns × rows 격자이고, 가득 차면 위층으로 쌓는다.
    /// 표시 상한을 넘는 돈은 마지막 오브젝트의 금액에 합산한다.
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class MoneyPile : MonoBehaviour
    {
        static readonly List<MoneyPile> active = new List<MoneyPile>();

        public static IReadOnlyList<MoneyPile> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        [Tooltip("첫 칸의 위치 (이 오브젝트 기준)")]
        [SerializeField] Vector3 origin = new Vector3(0f, 0.03f, 0f);
        [Tooltip("가로 칸 수 (X)")]
        [SerializeField] int columns = 4;
        [Tooltip("세로 칸 수 (Z, 돈의 긴 쪽)")]
        [SerializeField] int rows = 2;
        [SerializeField] Vector2 cellSize = new Vector2(0.26f, 0.52f);
        [SerializeField] float layerHeight = 0.06f;
        [Tooltip("표시할 최대 층 수. 넘는 금액은 마지막 오브젝트에 합산")]
        [SerializeField] int maxLayers = 10;

        readonly List<MoneyObject> stack = new List<MoneyObject>();
        readonly ReactiveProperty<int> totalAmount = new ReactiveProperty<int>(0);

        MoneyObject prefab;

        public InteractionZone Zone { get; private set; }
        public ReadOnlyReactiveProperty<int> TotalAmount => totalAmount;
        public bool HasMoney => stack.Count > 0;

        void Awake()
        {
            totalAmount.AddTo(this);
            Zone = GetComponent<InteractionZone>();
        }

        void OnEnable() => active.Add(this);

        void OnDisable() => active.Remove(this);

        public void Init(MoneyObject prefab) => this.prefab = prefab;

        /// <summary>금액을 돈 오브젝트로 쌓는다. from에서 튀어나와 자리에 놓인다.</summary>
        public void Add(int amount, Vector3 from)
        {
            if (amount <= 0)
                return;

            totalAmount.Value += amount;

            if (stack.Count >= columns * rows * maxLayers)
            {
                stack[stack.Count - 1].Amount += amount;
                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[MoneyPile] {name}: 돈 프리팹이 없습니다. GameConfig의 Money Prefab을 확인하세요.", this);
                return;
            }

            var money = ObjectPools.Get(prefab, from, Quaternion.identity);
            money.Amount = amount;

            var t = money.transform;
            t.SetParent(transform, true);
            t.DOLocalJump(SlotPosition(stack.Count), 1f, 1, 0.3f);
            t.DOLocalRotateQuaternion(Quaternion.identity, 0.3f);
            stack.Add(money);
        }

        /// <summary>맨 위(마지막에 놓인) 돈을 꺼낸다.</summary>
        public bool TryTakeTop(out MoneyObject money)
        {
            if (stack.Count == 0)
            {
                money = null;
                return false;
            }

            money = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            totalAmount.Value -= money.Amount;

            money.transform.DOKill();
            money.transform.SetParent(null, true);
            return true;
        }

        Vector3 SlotPosition(int index)
        {
            var perLayer = columns * rows;
            var layer = index / perLayer;
            var cell = index % perLayer;
            var column = cell % columns;
            var row = cell / columns;

            return origin + new Vector3(
                (column - (columns - 1) * 0.5f) * cellSize.x,
                layer * layerHeight,
                (row - (rows - 1) * 0.5f) * cellSize.y);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(origin, new Vector3(columns * cellSize.x, 0.01f, rows * cellSize.y));
        }
    }
}
