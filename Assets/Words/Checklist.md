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
| **P11** | **Zone 2 · 드라이브스루 · 포장 (마지막 Zone) · ZoneEnd 알림** | 6.3, 6.13 | ▶ **작업 중** |
| ~~P12~~ | ~~Zone 3 · 배달~~ | 6.14 | 제외 (2026-10-13 범위 축소: Zone 2에서 마무리) |
| P13 | 진상 손님 · 엔딩 | 6.8, 10장 | 대기 (❓ 종료 조건) |
| P14 | 저장 · 오프라인 보상 | 3장 | 대기 |

---

## P0. 사전 준비 (사용자)

- [x] Unity 6000.3.25f1 + URP 17.3
- [x] Input System 1.20 설치, Active Input Handling = Input System
- [x] R3 1.3.1 설치 (`using R3;`)
- [x] DOTween Pro 설치 및 Setup 완료 (`using DG.Tweening;`, Pro 기능 사용 가능)
- [ ] Player Settings 화면 방향을 Portrait로 변경 (현재 Auto Rotation), Game 뷰 세로 해상도
- [ ] MainScene에 빈 오브젝트 + `GameBootstrap` 부착 (사용자, P1 스크립트 작성 후)
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
  - [ ] "Lv N 필요" 표시·레벨 검사 → P7로 이동
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
- [ ] 테스트용 쓰레기 공급기(`DebugPickupFeeder`) 제거 (씬에서 TrashFactory 삭제 후 스크립트 삭제)
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

결정 (2026-10-13): Zone 2에 도로 추가, 차량 출구 따로, 경로 점 사용자 배치, 확장·시설 가격은 사용자가 조정, 포장 버거 = 버거 판매가 × 4 × 1.5 (포장 추가금), 알바 판매 간격 → 행동 속도(판매·포장 속도 배율), 알바 최대 6명, 종료 = ZoneEnd 해금 시 알림

- [ ] 받는 곳별 받는 종류(Kind) 필터: 일반 카운터(버거, `Counter.Sell Kind`), 포장 테이블(버거), 드라이브스루(포장 버거) — `PickupPile.SetAcceptedKinds`
- [ ] 스택 내려놓기: 받을 수 있는 것 중 가장 위에 있는 것부터 (플레이어·알바 공통)
- [ ] `PickupKind.PackedBurger`, `GameConfig` Packing·Drive-thru 항목 (버거 수 4, 배율 1.5, 플레이어 0.5초, 알바 2초, 포장 버거 프리팹), `PriceOf`/`ExpOf`
- [ ] `PackingTable`: 투입 칸(8), 포장 위치, 출력 칸(4), 버거 4개 → 포장 1개, 자리 비우면 진행도 유지, 월드 게이지(선택)
- [ ] 드라이브스루 카운터 = `Counter` 재사용 (Sell Kind = Packed Burger, 차량 줄)
- [ ] `Vehicle`(QueueMember): 포장 버거 1개 받으면 바로 차량 출구로 퇴장. `CustomerSpawner`·`CustomerQueue` 재사용
- [ ] 알바 행동 속도 (`StatKey.WorkerActionSpeed`, 기본 1배 ~ 최대 5배): 판매 간격 = 1초 ÷ 속도, 포장 간격 = 2초 ÷ 속도
- [ ] 알바 Task: 포장(Pack), 운반을 "모자란 곳(ISupplyTarget) → 그 종류를 내주는 가까운 곳" 방식으로 변경 (카운터·드라이브스루·포장 테이블)
- [ ] 알바 우선순위: 들고 있는 것 > 급한 청소 > 판매(일반·드라이브스루) > 포장 > 운반(많이 모자란 곳 우선) > 일반 청소
- [ ] `UnlockDefinition.unlockMessage`: 해금 시 알림 팝업 (ZoneEnd 데모 완료 알림에 사용)
- [ ] (사용자) 포장 버거 프리팹, 차량 프리팹, 차량용 CustomerConfig, PackingTableSet·DriveThruSet 프리팹
- [ ] (사용자) Zone 2 바닥·도로·벽, 차량 입구·출구(AreaGate), 차량 줄 경로 점, 차량 스포너
- [ ] (사용자) ZoneEnd (Zone + Expansion, Definition의 Unlock Message 입력), 알바 업그레이드 에셋 이름 정리(행동 속도)
- **완료 기준:** 버거 → 포장 → 드라이브스루 판매 → 돈 더미 수거 1바퀴, 포장 버거가 일반 카운터에 들어가지 않음, 알바가 포장·드라이브스루까지 처리, ZoneEnd 해금 시 알림

## ~~P12. Zone 3 · 배달~~ (제외: 2026-10-13 범위 축소, Zone 2에서 마무리. 필요해지면 아래 계획으로 재개)

- [ ] `DeliveryConfig` SO: 주문 품목(버거), 수량 5~10, 제한 시간 90초, 개당 보상 15·경험치 5, 재등장 30초
- [ ] `DeliverySpawner`: 시작 조건(`UnlockZone` 또는 `UnlockBox`), 한 번에 1명, 종료 후 30초 뒤 재등장
- [ ] `DeliveryRider`: 사람 입구 → 고정 배달 지점, 도착 시 주문 시작, 근처에서 0.1초/개 제출 → 배달원에게 쌓임
- [ ] `DeliveryBubble`: "SPECIAL ORDER", 남은 시간(분:초), 품목 아이콘, 제출/필요 수량, 카메라를 향함
- [ ] 성공: 즉시 입금(돈·경험치) → 퇴장 / 실패: 보상 없음, 제출분 소멸 → 퇴장
- [ ] (사용자) 배달원 프리팹, 배달 지점, 말풍선 UI 프리팹
- [ ] Zone 3 확장(300 / Lv 6) → 배달 지점(250), 완료 시 배달원 등장
- [ ] 알바 Task: 배달 제출 (제한 시간 임박 시 우선)
- **완료 기준:** 제한 시간 안에 제출하면 즉시 입금, 실패 시 보상 없이 퇴장, 30초 뒤 재등장

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

## 공통 규칙 (모든 Phase)

- [ ] `.unity` / `.prefab` 직접 편집 금지
- [ ] 밸런스 수치는 ScriptableObject로 분리, 하드코딩 금지
- [ ] 상태는 `ReactiveProperty`, UI는 `Subscribe`, 모든 구독 `.AddTo(this)`
- [ ] 돈·경험치 증감은 `CurrencyManager` 경유만
- [ ] 레거시 `UnityEngine.Input`, UniRx API 사용 금지
- [ ] 풀링 오브젝트 반납 시 `DOKill()` + 초기화
