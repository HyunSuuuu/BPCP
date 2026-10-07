using System.Linq;
using R3;
using UnityEngine;

namespace BurgerIdle.Unlock
{
    /// <summary>
    /// 해금 박스 묶음(구역). 모든 박스가 해금되면 완료된다. Zone 0 완료 시 손님 입장이 시작된다.
    /// </summary>
    public sealed class UnlockZone : MonoBehaviour
    {
        [Tooltip("비워 두면 자식의 UnlockBox를 모두 사용한다.")]
        [SerializeField] UnlockBox[] boxes;

        readonly ReactiveProperty<bool> isCompleted = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> IsCompleted => isCompleted;

        void Awake()
        {
            isCompleted.AddTo(this);
            if (boxes == null || boxes.Length == 0)
                boxes = GetComponentsInChildren<UnlockBox>(true);
        }

        void Start()
        {
            if (boxes.Length == 0)
            {
                Debug.LogWarning($"[UnlockZone] {name}: 해금 박스가 없습니다.", this);
                return;
            }

            Observable.CombineLatest(boxes.Select(box => box.IsUnlocked).ToArray())
                .Select(states => states.All(unlocked => unlocked))
                .Subscribe(completed => isCompleted.Value = completed)
                .AddTo(this);

            isCompleted.Where(completed => completed)
                .Take(1)
                .Subscribe(_ => Debug.Log($"[UnlockZone] {name} 해금 완료"))
                .AddTo(this);
        }
    }
}
