# 제작 순서 체크리스트 — Burger Idle

> GDD.md 13장(Phase)을 기준으로 세부 작업을 체크리스트로 정리한 문서.
> 각 Phase가 끝나면 멈추고 사용자 확인(에디터 컴파일·플레이 테스트)을 받은 뒤 다음 Phase로 넘어간다.
> ❓ 표시는 해당 Phase 시작 전에 사용자에게 물어봐야 하는 **[미정]** 항목이다.
> 오브젝트는 사용자가 프리팹으로 만든다. 각 Phase의 오브젝트 항목은 Claude Code가 컴포넌트를 작성하고 사용자가 프리팹에 부착·배치한다.

## 진행 현황

| Phase | 내용 | GDD | 상태 |
| --- | --- | --- | --- |
| P0~P5 | 준비, 이동, 재화, 픽업, 해금 박스, 핵심 루프 | 6.1~6.7, 6.11 | ✅ 완료 |
| P6 | Area · 출입문 | 6.12 | ✅ 완료 |
| P7 | 테이블 · 쓰레기 · 강제 청소 · 주문 수량 · VIP | 6.5, 6.7, 6.9, 6.10 | ✅ 완료 |
| P8 | 레벨업 · 업그레이드 · HUD 분리 | 8장, 11장 | ✅ 완료 |
| P9 | 알바 AI | 9장 | ✅ 완료 |
| P10 | ZoneManager · ZoneStart → Zone 0(매장) → Zone 1(업그레이드 패드) | 10장 | ✅ 완료 |
| P11 | Zone 2 · 드라이브스루 · 포장 · ZoneEnd 알림 | 6.3, 6.13 | ✅ 완료 |
| P12 | Zone 3 · 배달 (재추가, ZoneEnd 앞) | 6.14 | ✅ 완료 |
| P13 | 진상 손님 · 마무리 연출 | 6.8 | 대기 (엔딩은 P11 ZoneEnd 알림으로 완료) |
| P14 | 저장 · 오프라인 보상 | 3장 | 대기 |
| F1~F3 | Firebase 실습: 준비·익명 로그인 → Firestore 클리어 기록 → FCM 푸시 | - | 일시 중지 (F1 완료, F2 1단계부터 재개) |
| **P15** | **지역(Area) 확장: 지역별 레벨·클리어, 미니맵(입장·수거), 지속 수입, 현재 Area 10개 복제 테스트** | 10.1 | ▶ **다음 작업** (2026-10-22 개편, 결정 완료) |

> **현재 상태 (2026-10-21):** P0~P12 완료, 시작부터 엔딩(ZoneEnd)까지 플레이 가능, PC 빌드 준비 완료. Firebase 실습 F1 완료 후 일시 중지. 다음: **P15 지역(Area) 확장** → Firebase F2·F3 재개 → (선택) P13 진상 손님·연출, P14 저장

---

## P0. 사전 준비 (사용자)

- [x] Unity 6000.3.25f1 + URP 17.3
- [x] Input System 1.20 설치, Active Input Handling = Input System
- [x] R3 1.3.1 설치 (`using R3;`)
- [x] DOTween Pro 설치 및 Setup 완료 (`using DG.Tweening;`, Pro 기능 사용 가능)
- [x] Player Settings 화면 방향을 Portrait로 변경, Game 뷰 세로 해상도
- [x] MainScene에 빈 오브젝트 + `GameBootstrap` 부착 (사용자, P1 스크립트 작성 후)
- [x] `Assets/Skripts`(하위 폴더), `Assets/Data` 구성, 네임스페이스 `BurgerIdle.*`, 빈 `GameManager.cs` 삭제
- **완료 기준:** 컴파일 오류 없음

## P1. 플레이어 이동 · 카메라 · 조이스틱

- [x] (사용자) Ground·Player를 프리팹으로 만들어 MainScene에 배치, Player에 `PlayerController` 부착
- [x] `GameBootstrap`: 인스펙터 참조(Player, Ground, Camera)를 연결, 조이스틱 UI만 코드 생성
- [x] `GameConfig` ScriptableObject 골격 (임시값 보관용)
- [x] 고정 쿼터뷰 카메라 (세로 모드), 플레이어 추적
- [x] 가상 조이스틱 (Input System On-Screen Stick, 화면 하단)
- [x] `PlayerController`: 조이스틱 입력 → XZ 이동, 이동 방향 회전
- [x] `PlayerStats`: 이동속도(5) `ReactiveProperty`로 보관
- [x] 정지 상태 판정(입력 없음) 노출 — 해금 박스에서 사용
- **완료 기준:** 조이스틱으로 도형 캐릭터가 이동 ✅

## P2. 재화 · HUD

- [x] `CurrencyManager`: 돈/경험치 `ReactiveProperty`, 외부는 `ReadOnlyReactiveProperty`
  - [x] `Add()`, `TrySpend()`, `SpendUpTo(int max)` (실제 차감액 반환)
  - [x] 음수 방지·최대값 검증은 이 클래스 안에서만
- [x] 시작 돈 100
- [x] (사용자) HUD Canvas 프리팹 제작 (TextMeshPro 돈·경험치 텍스트) → `GameBootstrap`에 연결
- [x] `Hud`: 돈·경험치 표시, `Subscribe` 구독(`.AddTo(this)`), `DOTween.To` 카운트업 (레벨·경험치 바는 P7)
- [x] `DebugCheats`: 에디터 테스트 키 (M/N/B/E)
- **완료 기준:** 돈이 HUD에 표시되고 증감이 반영됨 ✅

## P3. 픽업 오브젝트 · CarryStack · 풀링

- [x] `ObjectPools` / `PooledObject`: 프리팹별 `UnityEngine.Pool.ObjectPool`, 반납 시 `DOKill()` + 크기·회전 초기화
- [x] `PickupObject`: 종류(버거·커피·쓰레기), 스택 한 칸 높이
- [x] (사용자) 버거·쓰레기 프리팹 제작 + `PickupObject` 부착
- [x] `InteractionZone`: XZ 평면 거리 검사 (콜라이더·물리 미사용)
- [x] 공용 부품: `PickupPile`(격자 보관·위로 쌓기), `PickupSource`(집어 가기), `PickupReceiver`(내려놓기), `InteractionRegistry`
- [x] `CarryStack`: 적재 종류 None/Food/Trash, 최대적재량 공유, 머리 위 스택 `DOLocalJump`, 이동 시 흔들림
- [x] `PickupInteractor`: 0.1초마다 1개, 내려놓기 우선 (마지막에 쌓은 것부터)
- [x] `TrashBin`: 쓰레기 받아서 풀 반납 (시작부터 설치)
- [x] `DebugPickupFeeder`: 테스트용 공급기 (P5에서 기계로 대체)
- **완료 기준:** 줍기·내려놓기 동작, 풀 재사용 확인 ✅
## P4. 해금 박스 · Zone 0 해금

- [x] `UnlockDefinition` SO: id, 이름, cost, requiredPlayerLevel (해금 대상 씬 오브젝트는 `UnlockBox`에서 연결)
- [x] `UnlockBox`
  - [x] (사용자) 박스 프리팹: 바닥에 눕힌 World Space Canvas (둥근 사각형 + 흰 테두리 + Filled Image + 비용 텍스트·돈 아이콘 + 잠금 표시), 스프라이트는 `Assets/Art/UI`
  - [x] 박스 안 0.5초 정지 후 투입 시작, 움직이거나 나가면 멈춤
  - [x] `SpendUpTo`로 부분 지불, 투입 금액 `ReactiveProperty`로 보관·재개 가능
  - [x] 투입 속도: 비용 ÷ 1.5 / 초 (정수 단위)
  - [x] Fill: Filled Image의 fillAmount = 투입 비율 (화면 아래→위), 돈이 도착할 때 박스가 살짝 튐
  - [x] 코인(돈 프리팹) 날아가는 DOTween 연출, 풀링
  - [x] 완료 시 박스 축소·제거 → 씬에 미리 배치한 대상 오브젝트가 Scale 트윈으로 등장
  - [x] "Lv N 필요" 표시·레벨 검사 → P8에서 구현 (P10에서 숫자만 표시로 변경)
- [x] `MoneyObject` 최소 구현 (금액, 풀 반납) + (사용자) 돈 프리팹 → `GameConfig`에 연결
- [x] (사용자) 카운터·버거 기계·테이블 해금 박스 배치, 각 30
- [x] 쓰레기통 시작부터 설치 (해금 없음)
- [x] `UnlockZone`: 묶인 박스가 모두 해금되면 `IsCompleted` (손님 스폰 트리거용)
- **완료 기준:** 100원으로 3개 해금 가능, 부분 지불 확인 ✅
## P5. 핵심 루프 1바퀴 (돈 더미 · 손님 · 판매)

- [x] `Machine`: 초당 0.5개 생산, 재고 상한 8, 출력 지점 `PickupPile`에 쌓기 (`DebugPickupFeeder` 대체)
- [x] 카운터: 음식 투입 지점(`PickupReceiver`), 재고 `PickupPile` (상한은 P7에서 결정, 지금은 무제한)
- [x] 돈 오브젝트 / 돈 더미
  - [x] (사용자) 돈 프리팹 1×2×0.2 비율 (P4에서 제작)
  - [x] `MoneyPile`: 4×2 격자, 가득 차면 위층, 상한 10층(초과분은 마지막 오브젝트에 합산), `TotalAmount` `ReactiveProperty`, 놓일 때 `DOLocalJump`
  - [x] `MoneyCollector`(플레이어 전용): 근처 통과 시 0.05초/개, 위층부터, 플레이어를 따라 날아와 도착 시 `CurrencyManager.Add()`
  - [x] (사용자) 카운터 옆 돈 더미 배치
- [x] 손님
  - [x] `CustomerConfig` SO: 이동속도, 입장 간격 3초, 줄 간격 1.0, 최대 8명 (진상 파라미터는 P10)
  - [x] (사용자) 손님 프리팹 + `Customer` 부착
  - [x] `CustomerSpawner`: Zone 0 해금 전 비활성, 입장 간격, 줄 가득 차면 입장 안 함
  - [x] `CustomerQueue`: 씬에 배치한 경로 점을 따라 슬롯 자동 계산, 앞 슬롯 비면 경로 따라 전진, 콜라이더 없음
  - [x] `Customer` 상태: Entering → Queueing → AtCounter → Leaving (테이블은 P6)
- [x] 판매 (`Counter`)
  - [x] 응대 위치 1곳 (플레이어, 알바는 P8)
  - [x] 플레이어 0.1초에 1개 판매, 손님 1명당 1개, 버거가 손님 머리 위로 전달
  - [x] 판매 진행도 게이지 없음 (진행도는 내부적으로 유지·초기화)
  - [x] 완료 시 재고 차감 → 돈 더미에 10 추가 → 경험치 5
- **완료 기준:** 생산 → 운반 → 판매 → 돈 더미 → 수거 1바퀴 ✅
## P6. Area · 출입문

GDD 6.12 기준. 드라이브스루 차량(P11)과 배달원(P12)이 같은 구조를 쓰도록 미리 정리하는 단계다.

- [x] `Area`: 매장 전체 영역, 출입문 목록, 종류로 출입문 찾기 (데모는 Area 1개)
- [x] `AreaGate`: 종류(사람 입구·사람 출구·차량 입구·차량 출구), 바깥 점 → 안쪽 점, 씬 뷰 표시(Gizmo)
- [x] (사용자) Area 오브젝트 + 사람 입구·출구(겸용 허용) + 차량 입구·출구 배치
- [x] `CustomerSpawner`: Entrance·Exit 필드 대신 Area + 입구·출구 종류 참조
- [x] 손님 동선
  - [x] 입장: 입구 바깥 점에서 생성 → 입구 안쪽 점 → 줄 끝 합류
  - [x] 퇴장: 출구 안쪽 점 → 출구 바깥 점 도착 시 풀 반납
- [x] 출입문 경유 이동(여러 점을 차례로 걷기)과 줄 서기 이동을 공용으로 분리 → 차량(P11)·배달원(P12)이 재사용
- [x] `CustomerQueue`는 줄 경로만 담당 (변경 최소화)
- [x] 기존 씬의 Entrance·Exit 오브젝트를 출입문으로 옮기는 안내
- [x] Wiki 갱신 (Area 구조, 조립 레시피)
- **완료 기준:** 손님이 Area 입구로 들어와 줄을 서고 출구로 나감, 벽을 가로지르지 않음 ✅

**P6 테스트 목록 (작업 완료 후 확정)**
1. 손님이 입구 바깥에서 나타나 입구 안쪽 점을 지나 줄 끝에 합류
2. 판매 후 출구 안쪽 점을 지나 바깥 점에서 사라짐
3. 씬 뷰에서 출입문 바깥·안쪽 점과 진행 방향이 보임
4. 기존 P5 루프(판매 → 돈 더미 → 수거)가 그대로 동작

## P7. 테이블 · 좌석 예약 · 쓰레기 · 강제 청소

- [x] `Table`: Seats[2](Sit Point), 가운데 공유 FoodPile·TrashPile, TipPile, SeatedSinceClean(`ReactiveProperty`)
- [x] 줄 맨 앞 손님이 가까운 입장 가능 테이블의 빈 좌석 예약 → 성공해야 판매 진행
- [x] 예약 시 SeatedSinceClean++, 2가 되면 MustClean (식사 중인 손님은 계속)
- [x] 주문 수량: 일반 1~4개, VIP 6~10개. 0.1초에 1개씩 주문 수량만큼 판매, 재고 부족 시 맨 앞에서 대기, 머리 위에 쌓임
- [x] `OrderBubble`: 줄 맨 앞 손님 머리 위 "받은 수/주문 수", 카메라를 향함
- [x] VIP 손님: 다른 색 프리팹(`Customer` Order Range 6~10, Tip Multiplier 3), `CustomerSpawner` Variants 가중치(일반 9 : VIP 1)
- [x] Customer 상태 확장: Served → 좌석까지 직선 이동 → 음식을 테이블 가운데에 내려놓음 → 공유 식사 → 함께 Leaving(Area 출구)
- [x] 공유 식사: 버거당 1.5초마다 음식 더미에서 1개씩 줄어듦(먹는 연출), 더미가 비면 종료. 기본 식사 시간 없음, 합석하면 버거가 더해져 연장
- [x] 식사 종료: 먹은 버거 1개당 쓰레기 1개(가운데 TrashPile), 앉은 손님마다 팁 3(VIP 3배) → 팁 더미, 함께 퇴장
- [x] (사용자) 테이블에 `Table` + 좌석 Sit Point + 가운데 FoodPile·TrashPile(+`InteractionZone`, `PickupSource`) + 팁 더미 배치, `GameConfig`에 Trash Prefab 연결
- [x] (사용자) VIP 손님 프리팹, 손님 프리팹에 주문 말풍선 UI
- [x] `DebugCheats` T 키: 게임 속도 1배 ↔ 3배
- [x] 쓰레기 픽업(음식 소지 시 불가) → 쓰레기통에서 0.1초/개 버리기, 보상 없음
- [x] 초기화: 예약·착석 0명 && TrashCount 0 → SeatedSinceClean = 0
- [x] 팁 수거는 테이블 상태와 독립
- [x] 테스트용 쓰레기 공급기(`DebugPickupFeeder`) 제거 (2026-10-13 스크립트 삭제)
- **완료 기준:** 좌석이 없으면 판매 안 됨 / 주문 수량만큼 판매·말풍선 표시 / 공유 식사 후 함께 퇴장 / 청소해야 테이블이 다시 열림 / 번갈아 입장해도 강제 청소 발동 (GDD 6.9 예시 1·2 검증) / VIP 등장 ✅

## P8. 레벨업 · 업그레이드

- [x] `LevelTable` SO (50 × 1.5^(N-1), 최대 20) + `PlayerLevel`: 누적 경험치 → 레벨·진행도 `ReactiveProperty`
- [x] HUD: 레벨 텍스트, 경험치 바(Filled), 레벨업 팝업 "LEVEL UP! Lv N" (보상 없음)
- [x] `UnlockBox` 레벨 검사: 필요 레벨 미달 시 잠금 표시(자물쇠·Lv N), 투입 안 함, 레벨 오르면 자동 해제
- [x] `UpgradeDefinition` SO: id, 이름, 종류(Category), 스탯(StatKey), 필요 레벨, 최대 레벨, 기본 비용·증가율, 레벨당 변화량
  - [x] 비용 곡선 `round(baseCost × 1.35^level)`, 스탯 = GameConfig 기본값 + 레벨 × 변화량
- [x] `UpgradeManager`: 업그레이드 레벨 `ReactiveProperty<int>`, `Stat(key)` 구독으로 플레이어·기계·테이블에 자동 반영, 구매는 `TryPurchase` 한 곳
- [x] 대상: 플레이어(이동속도, 최대적재량, 수익 배율 x1~x10 반올림), 버거 기계(초당 생산량, 최대 재고), 테이블(버거 1개 먹는 시간, 모든 테이블 공통)
- [x] ~~❓ 테이블 강화 단위~~ → 모든 테이블 공통
- [x] ~~❓ 카운터 업그레이드~~ → 없음, 재고 무제한
- [x] `UpgradePad`: 바닥 패드에 들어가면 패널 열림·벗어나면 닫힘, 패드별 보여 줄 종류 지정(탭 대체)
- [x] `UpgradePanel` + `UpgradeEntryView`: 카드 가로 나열(Horizontal Layout), 제목(패드별), X 닫기, 카드 = 이름·아이콘·업그레이드 횟수 칸·광고 버튼·돈 버튼·레벨 부족 표시, DOTween 등장·퇴장, 구매 시 팝`n- [x] 광고 시뮬레이션: 2초 시청 표시 → 무료 1레벨, 항목별 재사용 대기 30초 (``GameConfig`` Ad Duration·Ad Cooldown)
- [x] (사용자) LevelTable·UpgradeDefinition 에셋, 업그레이드 패드, 패널·항목 UI 프리팹, HUD 레벨 UI
- [x] HUD 분리: `Hud`(진입점) + `MoneyView`, `LevelView`("현재 / 필요량"), `NotificationPopup`(범용 알림), `CountUpText`(공용 숫자 연출), 전체 컴포넌트 인스펙터 Header 정리
- [x] (사용자) 돈·레벨·알림을 요소별 프리팹으로 만들어 HUD Canvas에 배치
- **완료 기준:** 업그레이드 구매 시 스탯이 실제로 변함, 레벨 조건이 업그레이드·해금 박스에 적용됨

## P9. 알바 AI

- [x] ~~❓ 알바 Role 분리 여부~~ → 모든 알바가 모든 일
- [x] `Worker`: NavMeshAgent 이동, 이동속도 3.5, 적재량 3, 돈 수거 안 함, 맡은 대상에서만 줍기·내려놓기, 할 일 없으면 제자리 대기
- [x] 일 고르기 (동적): 들고 있는 것 처리 > 막힌 좌석·강제 청소 > 판매(실제로 팔 수 있을 때) > 운반(재고 < 줄의 남은 주문량일 때만) > 일반 청소, 같으면 가까운 순. 재고 충분 시 운반 안 하고 대기
- [x] `TaskBoard`: 대상(기계 출력·테이블 쓰레기·카운터) 1명 예약
- [x] 가로채기: 이동 중 대상이 비면 포기, 플레이어가 응대 위치에 들어오면 판매 양보 → 다음 일
- [x] `Counter`: 플레이어 우선, 없으면 응대 위치에 도착한 알바가 판매 (알바 간격 1초, 게이지 없음)
- [x] `WorkerManager`: 인원수 스탯이 오르면 직원 대기 지점에서 생성
- [x] NavMesh: 시작 시·해금 후 다시 굽기 (`NavMeshSurface`)
- [x] 알바 업그레이드(Category Worker): 고용(인원수 0~4), 이동속도, 적재량, 판매 간격(하한 0.2초)
- [x] (사용자) 알바 프리팹, 직원 대기 지점, `WorkerManager`, 바닥 `NavMeshSurface`, 알바 업그레이드 에셋·패드
- **완료 기준:** 알바가 자동으로 일하고, 플레이어가 가로채면 다음 작업으로 넘어감
## P10. 구역 확장 · ZoneStart → Zone 0 → Zone 1

구역 구성 (2026-10-13 변경): **ZoneStart**(매장 밖, 시작 위치) → **Zone 0**(매장 구매: 카운터·버거 기계·테이블) → **Zone 1**(플레이어·알바 업그레이드 패드) → **Zone 2**(드라이브스루, P11에서 마무리)

> P10 테스트 완료 (2026-10-13): NavMesh 갱신·플레이어 막힘 해결, 손님 스폰 시점 해결, 레벨 0 시작 확인

- [x] `ZoneManager` + `Zone`: Zones 아래 Zone을 Hierarchy 순서대로, 각 Zone은 자식 Floor·RemoveOnExpand·Expansion·Facilities·Objects로 구성, Expansion 있는 Zone은 시작 시 숨김
- [x] 단계적 등장: 이전 Zone 시설 완료 → 확장 박스 → 확장 시 바닥·벽 솟아오름 + 칸막이 벽 내려감 → 시설 박스 등장
- [x] 시설(Facilities) 없는 Zone: 시작 공간이면 처음부터, 아니면 열리자마자 다음 확장 박스 등장 (ZoneStart용)
- [x] `Zone` 인스펙터 칸마다 연결 대상 툴팁, 확장 박스에 Target이 있으면 경고
- [x] NavMesh: 숨긴 뒤 굽기, 해금 후 제자리 갱신(`UpdateNavMesh`)으로 위에 있던 플레이어·알바 유지
- [x] 플레이어 이동을 NavMesh 안으로 제한 (`NavMeshAgent.Move`, 루트 높이를 Base Offset으로 자동 설정, NavMesh 없으면 바닥 사각형)
- [x] (사용자) `TableSet` 프리팹 (빈 루트 아래 해금 박스 + 테이블 형제 배치, Target = 같은 프리팹의 Table)
- [x] (사용자) ZoneStart 바닥, Zone 0 바닥·벽·출입문 벽, Zone 1 바닥·벽, 확장 Definition(Zone 0 = 40, Zone 1 = 400 / Lv 2)
- [x] (사용자) 확장 박스를 "여는 Zone" 아래로 이동 + Target 비우기, 칸막이 벽을 Zone 1의 Remove On Expand에 연결
- [x] (사용자) CustomerSpawner를 Facilities 밖(Zone 0/Object)으로
- [x] (사용자) Zone 1 Facilities: `PlayerPadSet`, `WorkerPadSet` (해금 박스 200 / Lv 4 + 패드)
- [x] `CustomerSpawner`: Open Zone이 비면 속한 Zone의 Facilities를 사용 (매장 시설 해금 전 손님 입장 버그)
- [x] `NavMeshIgnore`: 손님·픽업 물건·돈·플레이어를 NavMesh 굽기에서 제외 (다시 구울 때 그 자리에 구멍이 생겨 플레이어가 막히던 문제)
- [x] ~~❓ 버거 기계·테이블 업그레이드 패드 위치~~ → 사용자가 추후 직접 배치
- [x] 레벨 0 시작: `LevelTable.startLevel`(0), `PlayerLevel.RaiseTo`, `UnlockDefinition.grantLevel` (Unlock_Zone0 = 1) → 매장 해금 시 Lv 1
- [x] 해금 박스 필요 레벨 텍스트를 숫자만 표시 ("Lv 1" → "1")
- **완료 기준:** 매장 밖에서 시작 → Zone 0 구매 → 시설 3종 해금 시 손님 입장 → Zone 1 확장 → 업그레이드 패드 해금 → Zone 2 확장 박스 등장
## P11. Zone 2 · 드라이브스루 · 포장

> P11 테스트 완료 (2026-10-13): 드라이브스루·포장·알바 처리·ZoneEnd 알림까지 플레이 확인. 추가 수정: 포장 테이블 Input Capacity 0 = 무제한, 알바·플레이어 NavMeshAgent 회피 끔

결정 (2026-10-13): Zone 2에 도로 추가, 차량 출구 따로, 경로 점 사용자 배치, 확장·시설 가격은 사용자가 조정, 포장 버거 = 버거 판매가 × 4 × 1.5 (포장 추가금), 알바 판매 간격 → 행동 속도(판매·포장 속도 배율), 알바 최대 6명, 종료 = ZoneEnd 해금 시 알림

- [x] 받는 곳별 받는 종류(Kind) 필터: 일반 카운터(버거, `Counter.Sell Kind`), 포장 테이블(버거), 드라이브스루(포장 버거) — `PickupPile.SetAcceptedKinds`
- [x] 스택 내려놓기: 받을 수 있는 것 중 가장 위에 있는 것부터 (플레이어·알바 공통)
- [x] `PickupKind.PackedBurger`, `GameConfig` Packing·Drive-thru 항목 (버거 수 4, 배율 1.5, 플레이어 0.5초, 알바 2초, 포장 버거 프리팹), `PriceOf`/`ExpOf`
- [x] `PackingTable`: 투입 칸(8), 포장 위치, 출력 칸(4), 버거 4개 → 포장 1개, 자리 비우면 진행도 유지, 월드 게이지(선택)
- [x] 드라이브스루 카운터 = `Counter` 재사용 (Sell Kind = Packed Burger, 차량 줄)
- [x] `Vehicle`(QueueMember): 포장 버거 1개 받으면 바로 차량 출구로 퇴장. `CustomerSpawner`·`CustomerQueue` 재사용
- [x] 알바 행동 속도 (`StatKey.WorkerActionSpeed`, 기본 1배 ~ 최대 5배): 판매 간격 = 1초 ÷ 속도, 포장 간격 = 2초 ÷ 속도
- [x] 알바 Task: 포장(Pack), 운반을 "모자란 곳(ISupplyTarget) → 그 종류를 내주는 가까운 곳" 방식으로 변경 (카운터·드라이브스루·포장 테이블)
- [x] 알바 우선순위: 들고 있는 것 > 급한 청소 > 판매(일반·드라이브스루) > 포장 > 운반(많이 모자란 곳 우선) > 일반 청소
- [x] 알바·플레이어 NavMeshAgent 회피 끔 (서로 밀어 작업 위치에서 벗어나 멈추던 문제), 작업 중 위치를 벗어나면 다시 이동
- [x] `UnlockDefinition.unlockMessage`: 해금 시 알림 팝업 (ZoneEnd 데모 완료 알림에 사용)
- [x] (사용자) 포장 버거 프리팹, 차량 프리팹, 차량용 CustomerConfig, PackingTableSet·DriveThruSet 프리팹
- [x] (사용자) Zone 2 바닥·도로·벽, 차량 입구·출구(AreaGate), 차량 줄 경로 점, 차량 스포너
- [x] (사용자) ZoneEnd (Zone + Expansion, Definition의 Unlock Message 입력), 알바 업그레이드 에셋 이름 정리(행동 속도)
- **완료 기준:** 버거 → 포장 → 드라이브스루 판매 → 돈 더미 수거 1바퀴, 포장 버거가 일반 카운터에 들어가지 않음, 알바가 포장·드라이브스루까지 처리, ZoneEnd 해금 시 알림

## P12. Zone 3 · 배달 (2026-10-13 재추가)

> P12 테스트 완료 (2026-10-13): 배달 성공·실패·재등장, 알바 제출 확인. 엔딩(ZoneEnd)까지 버그 없이 플레이 완료

결정: Zone 3 = 배달 지점만, 배달원이 와서 기다리는 동안만 배달 지점이 받는 곳(임시 판매대)이 됨, 주문 품목 버거만, 보상 = 일반 판매가 × 2 (경험치도 × 2, 수익 배율 적용, 즉시 입금), 제한 시간 90초, 알바 참여(판매 다음·포장 위, 20초 이하 남으면 급한 청소 다음), 실패 시 제출분 삭제 + "배달 실패" 알림, 지나가며 자동 제출 허용, 도착 시 "SPECIAL ORDER!" 알림

- [x] `DeliveryConfig` SO: 품목(버거), 수량 5~10, 제한 시간 90초, 보상 배율 2, 첫 등장 5초, 재등장 30초, 이동속도, 급한 시간 20초, 품목 아이콘
- [x] `DeliveryPoint`: 배달원이 주문 중일 때만 `IPickupReceiver` + `ISupplyTarget`로 등록 → 플레이어 자동 제출, 알바 운반이 그대로 동작
- [x] `DeliveryRider`: 사람 입구 → 배달 지점, 도착 시 주문 시작(제한 시간), 제출분 머리 위에 쌓임, 성공·실패 → 사람 출구 → 풀 반납
- [x] `DeliverySpawner`: Open Zone(비우면 속한 Zone의 Facilities) 해금 후 시작, 한 번에 1명, 종료 후 30초 뒤 재등장, 성공 보상 즉시 입금, 알림(도착·성공·실패)
- [x] `DeliveryBubble`: SPECIAL ORDER(고정 텍스트), 남은 시간 분:초(급하면 빨강), 품목 아이콘, 제출/필요, 카메라를 향함
- [x] 알바: `SupplyPriority`(Normal·Order·Urgent)로 배달 운반 우선순위 (Urgent 3.5 > 판매 3 > Order 2.75 > 포장 2.5 > 재고 운반 2)
- [x] (사용자) 배달원 프리팹(+ 말풍선 World Space Canvas), DeliveryConfig 에셋, 배달 지점 세트(해금 박스 + DeliveryPoint), 배달 스포너
- [x] (사용자) Zone 3 (ZoneEnd 위 순서), 확장 박스·Definition
- **완료 기준:** 제한 시간 안에 제출하면 즉시 입금, 실패 시 보상 없이 제출분 삭제 후 퇴장, 30초 뒤 재등장, 알바도 제출

## P13. 진상 손님 · 마무리

- [ ] 진상 손님 (빨강 Capsule), `CustomerConfig` 파라미터 사용
  - [ ] spawnChance 0.1, maxConcurrent 1
  - [ ] 줄에서 멈춰 뒤 손님 차단, 앞 손님은 정상 진행
  - [ ] 근처 시 게이지 0.5/초(중첩 없음), 이탈 시 0.25/초 감소
  - [ ] 해결 시 일반 손님 변환(빨강 → 노랑), 줄 한꺼번에 전진, 보상 15 → 카운터 돈 더미
- [ ] 알바 Task에 진상 처리 추가
- [ ] 연출 정리 (트윈 0.15~0.4초, `SetLink`/`DOKill` 점검)
- [ ] ❓ 데모 종료 조건 → 종료 화면 (14장 12번, 후보: Zone 2 완료)
- [ ] ❓ 튜토리얼 화살표, 리워드 광고 버튼 포함 여부 (14장 3~4번)
- **완료 기준:** 시작부터 엔딩까지 플레이 가능

## P14. 저장 · 오프라인 보상

- [ ] ❓ 저장 범위 (돈·경험치·업그레이드·해금만 / 돈 더미·테이블 상태까지)
- [ ] ❓ 오프라인 보상 계산식과 상한 시간
- [ ] `SaveManager`: 저장 데이터 직렬화(JSON), 일시정지·종료 시 저장, 시작 시 복원
- [ ] `OfflineReward`: 종료 시각 기록, 재접속 시 보상 계산 → 지급 UI
- [ ] 각 매니저의 `ReactiveProperty` 상태를 저장 데이터와 양방향 매핑
- **완료 기준:** 재시작 시 진행 상황 복원, 오프라인 보상 지급

---

## 빌드 준비 (2026-10-13)

- [x] Build Settings 씬 목록: MainScene 1개
- [x] 화면 방향 Portrait, PC 창은 모니터 높이의 85%인 9:16 세로 창(`StandaloneWindow`, Resizable 끔, Windowed)
- [ ] (사용자) HUD Canvas Scaler: Scale With Screen Size, 1080×1920, Match 0 (현재 Constant Pixel Size라 PC 창에서 UI가 약 2배로 커짐)
- [x] 디버그 치트(`DebugCheats`)는 에디터·Development Build에서만 동작 (릴리스 빌드 제외)
- [x] 테스트용 `DebugPickupFeeder` 삭제
- [x] 씬·프리팹에 빠진 스크립트(Missing Script) 없음 확인
- [x] Android: IL2CPP, ARM64, Min SDK 25
- [ ] (사용자) Company Name·Product Name·Package Name 변경 (현재 DefaultCompany / BPCP / 템플릿 기본 패키지명)
- [ ] (사용자) Version 정하기 (현재 0.1.0), Android Bundle Version Code
- [ ] (사용자) 아이콘·스플래시 (선택)
- [ ] (사용자) Development Build 끄고 빌드 → 실행 확인 (Windows exe, Android APK)

## P15. 지역(Area) 확장 (2026-10-21 추가, 2026-10-22 개편)

GDD 10.1 기준. 지역마다 레벨이 따로 있고, 레벨업 10회를 하면 지역을 클리어해 다음 지역이 열린다. 클리어한 지역은 분당 지속 수입을 만든다. 지역 이동과 수입 수거는 우하단 나침반 버튼으로 여는 미니맵에서 한다. Area를 복제·삭제·순서 변경만으로 지역 수를 바꿀 수 있게 만들고, 현재 Area를 10개 복제해 테스트한다.

결정 (2026-10-21, 유지): 순간이동(페이드) / 떠난 지역은 숨기고 멈춤 / 플레이어 업그레이드만 공유, 알바·기계·테이블은 지역별 / 알바 지역별 고용 / Area에 비용·수익 배율 칸(기본 1) / 지역 번호 + 이름
결정 (2026-10-22, 변경): 돈만 공유, 경험치·레벨은 지역별 / 지역에서 레벨업 10회 = 클리어 → 다음 지역 해금 (지역마다 Lv 0부터 다시 시작, Zone 0 해금 시 Lv 1, Lv 11 클리어) / 클리어 지역은 지역마다 정한 분당 지속 수입(Area 칸)이 떠나 있을 때만 쌓임(상한 120분치, 배율 적용 안 함) → 미니맵에서 수거 / HUD 우하단 나침반 버튼 → 미니맵(매장 간단 표시, 플레이어 표시 없음, 드래그 이동) → 매장 클릭 시 매장 UI(미클리어: 입장만, 클리어: 입장·수거 + 분당 수입) → UI 밖 클릭·드래그 시 매장 UI 닫힘
폐기 (2026-10-21 계획): ZoneEnd 해금으로 다음 지역 열기·자동 이동, HUD 지역 버튼·지역 목록, 평균 수익(최근 3분 분당 평균, 상한 2시간치)

**결정 (GDD 14장 25~30번, 2026-10-22 모두 결정)**
- [x] ~~❓ 지역 레벨 번호 매기기~~ → 지역마다 처음부터 (Lv 0 → Zone 0 해금 Lv 1 → Lv 11 클리어)
- [x] ~~❓ 클리어 후 레벨~~ → Lv 11에서 멈춤 (MAX 표시, 경험치 안 쌓임, 플레이는 계속 가능)
- [x] ~~❓ 다음 지역 해금~~ → 클리어 즉시 무료 해금 + "새 지역 오픈!" 알림, 자동 이동 없음
- [x] ~~❓ ZoneEnd~~ → 제거, 클리어는 Lv 11 하나로만
- [x] ~~❓ 지속 수입 규칙~~ → 떠나 있을 때만 쌓임, 상한 120분치(데이터), 수익 배율 적용 안 함
- [x] ~~❓ 미니맵 표현·잠긴 지역·게임 정지~~ → UI 지도(ScrollRect), 잠긴 지역 모두 자물쇠, 열려 있는 동안 게임 정지
- [x] ~~❓ 미니맵 위치·닫기~~ → MainScene UI 패널 + 페이드 (별도 씬 안 씀), X 버튼·뒤로가기(Esc)

**지역 관리**
- [ ] `AreaManager`: `Areas` 아래 Area를 Hierarchy 순서대로 사용, 지역 수를 코드에 적지 않음, 열리지 않은 지역 숨김
- [ ] `Area` 설정 칸: Display Name(비우면 "지역 N"), Unlock Cost Multiplier, Income Multiplier, Income Per Minute, Start Point
- [ ] 지역 전환: 떠나는 지역 멈춤(숨김, 그 지역 풀 오브젝트 포함), 도착 지역 다시 움직임(떠날 때 상태 그대로), 페이드 순간이동 (NavMeshAgent Warp)
- [ ] 마지막 지역 클리어 시 데모 완료 알림·Firebase 클리어 기록 (`ZoneManager.IsFinished` 대신 `AreaManager`에서)
- [ ] (사용자) 각 Area에서 ZoneEnd 오브젝트 삭제 (Zone 3이 마지막 Zone)

**지역 레벨과 클리어**
- [ ] 경험치·레벨 지역별: 지역마다 `PlayerLevel`, 돈은 `CurrencyManager` 하나 그대로
- [ ] HUD 레벨·경험치 바는 현재 지역 레벨을 표시
- [ ] 해금 박스·업그레이드 필요 레벨을 그 지역 레벨로 검사
- [ ] 레벨업 10회(데이터) 달성 → 지역 클리어 → 다음 지역 즉시 무료 해금 + "새 지역 오픈! (이름)" 알림 (자동 이동 없음)
- [ ] 클리어한 지역은 레벨 고정(MAX 표시, 경험치 안 쌓임), 플레이는 계속 가능

**지역별·공유 데이터**
- [ ] 지역 단위 초기화: 지역마다 `ZoneManager`·`WorkerManager`·스포너·NavMesh (`GameBootstrap`의 "첫 번째만 사용" 제거, 이동 범위 NavMesh 기준)
- [ ] `UpgradeManager`: 플레이어 항목 공유, 알바·기계·테이블 항목 지역별 레벨 (패드는 자기 지역 레벨 표시)
- [ ] 지역 배율 적용: 해금 비용(`UnlockBox`), 수익(`Counter`·`MoneyPile`·팁·배달 보상)

**지속 수입**
- [ ] 클리어한 지역마다 플레이어가 떠나 있는 동안 Income Per Minute × 경과 시간 적립 (그 지역 안에 있으면 쌓이지 않음)
- [ ] 적립 상한: 분당 수입 × 상한 시간(데이터, 기본 120분), 상한이면 매장 UI에 MAX
- [ ] 수익 배율·지역 Income Multiplier 적용 안 함
- [ ] 수거 시 `CurrencyManager.Add`로 바로 입금, 적립액 0으로

**나침반 버튼 · 미니맵**
- [ ] HUD 우하단 나침반 버튼 → 페이드 → 미니맵(MainScene UI 전체 화면 패널) 열기, 열려 있는 동안 게임 정지
- [ ] 닫기: X 버튼, 뒤로가기(PC Esc) → 페이드 → 원래 지역, 게임 재개 (페이드는 시간 배율 무시)
- [ ] 미니맵(UI 지도): 배경 + 매장 아이콘, 플레이어 표시 없음, 드래그(ScrollRect)로 화면 이동
- [ ] 매장 아이콘은 Area 목록에서 자동 생성, 위치는 Area의 Map Position
- [ ] 잠긴 지역: 어둡게 + 자물쇠, 클릭 시 오픈 조건 안내만 (버튼 비활성)
- [ ] 매장 클릭 → 매장 UI: 입장 버튼(열린 지역), 수거 버튼·분당 수입(클리어 지역만)
- [ ] 매장 UI 밖 클릭·드래그 시 매장 UI 닫힘
- [ ] 입장 → 미니맵 닫힘 + 그 지역으로 순간이동 (같은 페이드 안에서), 지금 지역이면 닫기만
- [ ] (사용자) 나침반 버튼·미니맵·매장 표시·매장 UI 프리팹

**디버그**
- [ ] `DebugCheats`: 현재 지역 레벨 올리기 키 (10개 지역 클리어 테스트용, 에디터·개발 빌드만)

**배치**
- [ ] (사용자) 현재 Area를 프리팹으로 만들고 `Areas` 아래 10개 배치 (지역끼리 겹치지 않게 간격), 지역마다 Income Per Minute·Map Position 다르게

**테스트**
- [ ] 지역마다 레벨이 따로 오르고, 레벨업 10회마다 다음 지역이 미니맵에서 열림
- [ ] 나침반 버튼 → 미니맵 → 매장 클릭 → 입장으로 열린 지역을 오가고, 돌아가면 떠날 때 상태 그대로
- [ ] 미클리어 지역은 입장만, 클리어 지역은 입장·수거와 분당 수입 표시
- [ ] 클리어한 지역에 지속 수입이 쌓이고 수거 버튼으로 받음
- [ ] 매장 UI 밖 클릭·드래그 시 매장 UI 닫힘, 드래그로 미니맵 이동
- [ ] 알바·기계·테이블 업그레이드는 지역마다 따로, 플레이어 업그레이드·돈은 공유
- [ ] 중간 지역 삭제·순서 변경 후에도 코드 수정 없이 이어서 열림
- [ ] 10개 지역에서 프레임 유지 (동작하는 지역은 항상 1개)
- **완료 기준:** 지역마다 레벨업 10회로 1 → 10 지역이 차례로 열리고, 미니맵으로 오가며 지속 수입을 수거하고, Area 복제·삭제·순서 변경만으로 지역 수가 바뀜

## Firebase 실습 (2026-10-18)

결정: 빠르고 간단한 연결 실습만 (저장 기능 없음). Android 12 에뮬레이터(Google Play 이미지), Firestore 서울(asia-northeast3), SDK는 UPM(.tgz), 익명 로그인, 클리어할 때마다 기록 추가(닉네임 없음), 푸시는 콘솔에서 보내는 간단한 알림만, `google-services.json`은 git 제외
코드: `Online/FirebaseService`(정의 기호 `BPCP_FIREBASE`가 있을 때만 동작, 없으면 무시 → PC 빌드 영향 없음), `Core/GameStats`(플레이 시간·판매 수), `ZoneManager.IsFinished`(마지막 Zone 해금)

### F1. 준비 · 익명 로그인
> F1 완료 (2026-10-19): MuMu Player 12에서 준비 완료·익명 로그인·FCM 토큰·토픽 구독 로그 확인

- [x] (사용자) Package Name 변경 → `com.bpcp.burgeridle`
- [x] (사용자) Firebase 프로젝트 생성, Android 앱 등록, `google-services.json`을 Assets에 넣기
- [x] (사용자) Authentication → 익명 로그인 사용 설정
- [x] (사용자) SDK 설치: External Dependency Manager 1.2.190 + Firebase App·Auth·Firestore·Messaging 14.0.0 (.tgz, `Packages/Firebase/`)
- [x] (사용자) 테스트 기기: Android Studio 대신 MuMu Player 12 (APK 끌어다 설치, adb connect 후 Android Logcat)
- [x] (사용자) Android 정의 기호 `BPCP_FIREBASE`, Android Resolver 실행, File → Save Project (Target Architectures는 ARM64 그대로: 이 Unity 버전에는 x86_64 항목이 없고, Android 11+ 에뮬레이터가 ARM 앱을 변환 실행)
- [x] `FirebaseService.Init`: 의존성 확인 → 익명 로그인 → UID 로그
- **완료 기준:** 에뮬레이터 Logcat에 `[Firebase] 익명 로그인: (UID)`, 콘솔 Authentication 사용자 목록에 익명 사용자 1명

### F2. Firestore 클리어 기록
- [ ] (사용자) Firestore 데이터베이스 생성 (서울), 보안 규칙 적용
- [x] 데모 완료(마지막 Zone 해금, P15 이후 마지막 지역 클리어) 시 `clears` 컬렉션에 기록 추가: uid, playSeconds, level, sales, platform, clearedAt(서버 시각)
- [x] 판매 수 집계: 카운터·드라이브스루 판매 + 배달 성공 수량
- **완료 기준:** 클리어 후 Firestore 콘솔 `clears`에 문서 1개 생성

### F3. FCM 푸시
- [x] FCM 토큰 로그, `all` 토픽 구독, 앱이 켜져 있을 때 받은 알림은 게임 알림 팝업으로 표시
- [ ] (사용자) 콘솔 Messaging에서 테스트 메시지(토큰) → 토픽 `all` 캠페인 발송
- **완료 기준:** 앱이 켜져 있으면 게임 알림 팝업, 백그라운드면 Android 알림창에 표시되고 누르면 앱이 열림

## 공통 규칙 (모든 Phase)

- [ ] `.unity` / `.prefab` 직접 편집 금지
- [ ] 밸런스 수치는 ScriptableObject로 분리, 하드코딩 금지
- [ ] 상태는 `ReactiveProperty`, UI는 `Subscribe`, 모든 구독 `.AddTo(this)`
- [ ] 돈·경험치 증감은 `CurrencyManager` 경유만
- [ ] 레거시 `UnityEngine.Input`, UniRx API 사용 금지
- [ ] 풀링 오브젝트 반납 시 `DOKill()` + 초기화
