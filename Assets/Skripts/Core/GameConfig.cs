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
        [Tooltip("경험치 → 레벨 곡선. 비우면 기본값(50, ×1.5, 최대 20) 사용")]
        public Economy.LevelTable levelTable;

        [Header("Upgrade")]
        [Tooltip("업그레이드 항목 목록. 패널에 이 순서대로 표시")]
        public Upgrade.UpgradeDefinition[] upgrades;
        [Tooltip("광고 시뮬레이션 시청 시간 (초)")]
        public float adDuration = 2f;
        [Tooltip("광고로 업그레이드한 항목을 다시 광고로 올릴 수 있기까지 (초)")]
        public float adCooldown = 30f;

        [Header("Player")]
        public float playerMoveSpeed = 5f;
        public int playerMaxCarry = 4;
        [Tooltip("초당 회전 각도")]
        public float playerTurnSpeed = 720f;
        [Tooltip("수익 배율 최소·최대 (업그레이드로 오름, 기본값 = 최소)")]
        public Vector2 incomeMultiplierRange = new Vector2(1f, 10f);

        [Header("Pickup")]
        [Tooltip("자동 줍기·내려놓기 간격 (초 / 개)")]
        public float pickupInterval = 0.1f;

        [Header("Sales")]
        public int burgerPrice = 10;
        [Tooltip("판매 1건당 경험치")]
        public int sellExp = 5;
        [Tooltip("플레이어 판매 간격 (초 / 개, 업그레이드 없음)")]
        public float playerSellInterval = 0.1f;
        [Tooltip("알바 판매 간격 기본값 (초 / 개). 실제 간격 = 이 값 ÷ 알바 행동 속도")]
        public float workerSellInterval = 1f;

        [Header("Packing · Drive-thru")]
        [Tooltip("포장 버거 1개에 들어가는 버거 수")]
        [Min(1)] public int burgersPerPack = 4;
        [Tooltip("포장 버거 판매가·경험치 = 버거 판매가·경험치 × 버거 수 × 이 배율 (포장 노동 추가금)")]
        public float packedPriceMultiplier = 1.5f;
        [Tooltip("플레이어 포장 간격 (초 / 개, 업그레이드 없음)")]
        public float playerPackInterval = 0.5f;
        [Tooltip("알바 포장 간격 기본값 (초 / 개). 실제 간격 = 이 값 ÷ 알바 행동 속도")]
        public float workerPackInterval = 2f;
        [Tooltip("포장 버거 프리팹 (PickupObject, Kind = Packed Burger)")]
        public Pickup.PickupObject packedBurgerPrefab;

        [Header("Worker")]
        public float workerMoveSpeed = 3.5f;
        public int workerMaxCarry = 3;
        [Tooltip("알바 인원수 최소·최대. 최소가 시작 인원 (0이면 고용 전에는 알바 없음)")]
        public Vector2Int workerCountRange = new Vector2Int(0, 6);
        [Tooltip("알바 행동 속도(판매·포장 속도 배율) 최대값. 기본 1배에서 업그레이드로 오름")]
        public float workerActionSpeedMax = 5f;

        /// <summary>종류별 판매가 (포장 버거 = 버거 × 버거 수 × 포장 배율, 반올림)</summary>
        public int PriceOf(Pickup.PickupKind kind) => kind == Pickup.PickupKind.PackedBurger
            ? Mathf.RoundToInt(burgerPrice * burgersPerPack * packedPriceMultiplier)
            : burgerPrice;

        /// <summary>종류별 판매 경험치 (포장 버거는 판매가와 같은 비율)</summary>
        public int ExpOf(Pickup.PickupKind kind) => kind == Pickup.PickupKind.PackedBurger
            ? Mathf.RoundToInt(sellExp * burgersPerPack * packedPriceMultiplier)
            : sellExp;

        [Header("Machine")]
        public float machineProducePerSecond = 0.5f;
        public int machineMaxStock = 8;

        [Header("Table")]
        [Tooltip("버거 1개를 먹는 시간 (초). 식사 시간 = 버거 수 × 이 값. 테이블 업그레이드로 감소 — P8")]
        public float tableEatPerItem = 1.5f;
        [Tooltip("손님 1명당 팁 (VIP는 Customer의 Tip Multiplier 배)")]
        public int tipAmount = 3;
        [Tooltip("청소 후 누적 예약·착석 수가 이 값이 되면 강제 청소 상태")]
        public int tableMustCleanCount = 2;

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
        [Tooltip("식사 후 테이블에 남는 쓰레기 프리팹 (PickupObject, Kind = Trash)")]
        public Pickup.PickupObject trashPrefab;

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
        [Tooltip("터치하지 않을 때 조이스틱을 숨긴다. 끄면 화면 하단 중앙에 대기 표시")]
        public bool joystickHideWhenIdle = true;
        [Tooltip("대기 표시할 때 화면 하단으로부터의 높이 (px). Hide When Idle을 끈 경우에만 사용")]
        public float joystickRestHeight = 380f;
    }
}
