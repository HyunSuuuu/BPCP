using BurgerIdle.Pickup;
using DG.Tweening;
using UnityEngine;

namespace BurgerIdle.Station
{
    /// <summary>
    /// 쓰레기를 받아 풀에 반납한다. 시작부터 설치되어 있고 용량·업그레이드·보상이 없다.
    /// </summary>
    [RequireComponent(typeof(InteractionZone))]
    public sealed class TrashBin : MonoBehaviour, IPickupReceiver
    {
        [Tooltip("쓰레기가 날아가 들어가는 지점의 높이")]
        [SerializeField] float mouthHeight = 1f;
        [SerializeField] float jumpDuration = 0.25f;

        public InteractionZone Zone { get; private set; }

        void Awake() => Zone = GetComponent<InteractionZone>();

        void OnEnable() => InteractionRegistry.Register(this);

        void OnDisable() => InteractionRegistry.Unregister(this);

        public bool CanReceive(PickupObject item) => item.Category == PickupCategory.Trash;

        public void Receive(PickupObject item)
        {
            var t = item.transform;
            DOTween.Sequence()
                .Append(t.DOJump(transform.position + Vector3.up * mouthHeight, 1f, 1, jumpDuration))
                .Append(t.DOScale(0f, 0.1f))
                .SetTarget(t)
                .OnComplete(item.Release);
        }
    }
}
