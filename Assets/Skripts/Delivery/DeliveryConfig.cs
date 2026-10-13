using BurgerIdle.Pickup;
using UnityEngine;

namespace BurgerIdle.Delivery
{
    /// <summary>배달(특별 주문) 데이터. 주문 품목·수량·제한 시간·보상 배율·재등장 시간.</summary>
    [CreateAssetMenu(fileName = "DeliveryConfig", menuName = "BurgerIdle/Delivery Config")]
    public sealed class DeliveryConfig : ScriptableObject
    {
        [Header("Order")]
        [Tooltip("주문 품목")]
        public PickupKind orderKind = PickupKind.Burger;
        [Tooltip("필요 수량 범위 (최소, 최대)")]
        public Vector2Int quantityRange = new Vector2Int(5, 10);
        [Tooltip("제한 시간 (초). 배달원이 배달 지점에 도착하면 흐르기 시작")]
        public float timeLimit = 90f;
        [Tooltip("보상 = 일반 판매가·경험치 × 수량 × 이 배율. 성공 시 바로 입금 (수익 배율 적용)")]
        public float rewardMultiplier = 2f;

        [Header("Spawn")]
        [Tooltip("배달이 열린 뒤 첫 배달원이 오기까지 (초)")]
        public float firstDelay = 5f;
        [Tooltip("배달이 끝난 뒤(성공·실패) 다음 배달원이 오기까지 (초)")]
        public float respawnDelay = 30f;
        public float moveSpeed = 3f;

        [Header("Worker")]
        [Tooltip("남은 시간이 이 값 이하면 알바가 급한 청소 다음으로 먼저 처리")]
        public float urgentTime = 20f;

        [Header("Display")]
        [Tooltip("말풍선에 표시할 품목 아이콘 (선택)")]
        public Sprite orderIcon;
    }
}
