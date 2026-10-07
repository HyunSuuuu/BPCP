using UnityEngine;

namespace BurgerIdle.Core
{
    /// <summary>
    /// 밸런스·카메라·UI 임시값 모음. 에셋이 없으면 GameBootstrap이 기본값 인스턴스를 만들어 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BurgerIdle/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Economy")]
        public int startMoney = 100;

        [Header("Player")]
        public float playerMoveSpeed = 5f;
        public int playerMaxCarry = 4;
        [Tooltip("초당 회전 각도")]
        public float playerTurnSpeed = 720f;

        [Header("Pickup")]
        [Tooltip("자동 줍기·내려놓기 간격 (초 / 개)")]
        public float pickupInterval = 0.1f;

        [Header("Sales")]
        public int burgerPrice = 10;
        [Tooltip("판매 1건당 경험치")]
        public int sellExp = 5;
        [Tooltip("플레이어 판매 간격 (초 / 개, 업그레이드 없음)")]
        public float playerSellInterval = 0.1f;
        [Tooltip("알바 판매 간격 기본값 (초 / 개, 업그레이드로 감소) — P9")]
        public float workerSellInterval = 1f;

        [Header("Machine")]
        public float machineProducePerSecond = 0.5f;
        public int machineMaxStock = 8;

        [Header("Money")]
        [Tooltip("돈 더미 수거 간격 (초 / 개)")]
        public float moneyCollectInterval = 0.05f;

        [Header("Unlock")]
        [Tooltip("해금 박스 안에서 이 시간 이상 정지해야 투입 시작 (초)")]
        public float unlockStillTime = 0.5f;
        [Tooltip("비용 전액이 들어가는 데 걸리는 시간 (초). 투입 속도 = 비용 ÷ 이 값")]
        public float unlockPayDuration = 1.5f;

        [Header("Prefabs")]
        [Tooltip("돈 오브젝트 프리팹 (해금 투입 연출, 돈 더미)")]
        public Money.MoneyObject moneyPrefab;

        [Header("Camera")]
        public float cameraPitch = 50f;
        public float cameraYaw = 45f;
        public float cameraDistance = 22f;
        public float cameraFieldOfView = 40f;
        [Tooltip("추적 보간 속도 (클수록 빠르게 따라감)")]
        public float cameraFollowSharpness = 8f;

        [Header("Joystick")]
        [Tooltip("기준 해상도(1080x1920)에서 조이스틱 반경 (px)")]
        public float joystickRadius = 140f;
        [Tooltip("대기 중 조이스틱이 놓이는 화면 하단으로부터의 높이 (px)")]
        public float joystickRestHeight = 380f;
    }
}
