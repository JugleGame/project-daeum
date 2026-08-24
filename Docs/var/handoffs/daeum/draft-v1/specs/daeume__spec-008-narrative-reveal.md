+++
spec_id = "daeume__spec-008-narrative-reveal"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-032", "ELEM-049", "ELEM-048", "GENRE-040"]
dependencies = ["daeume__spec-005-memory-chest", "daeume__spec-007-stage-progression"]
+++

# 서사, 진실과 복선 회수

## Goal
행복한 기억이 실제였음을 끝까지 보존하면서 누락된 배경 사실, 병원 사건, 잔재 정체와 자책의 비인과를 단계적으로 공개한다.

## Build scope
- 8일 슬라이스: **제외**.
- 슬라이스에서 구현한다: Stage 1 기억 1건의 데이터 레코드와 문자열 테이블 키만.
- 슬라이스에서 제외한다: Truth 5종, 공개 순서 검증, 복선 회수표 전체.

## Implementation scope
- 기억 데이터에 MemoryId, 시간, 장소, 사건, 핵심 대사, 행복 요소, ForeshadowingIds, RevealStage, HealthClueDirectness를 둔다.
- `HealthClueDirectness`는 0~2 정수다. 0은 단서 없음, 1은 hindsight에서만 연결되는 흔적, 2는 당시에도 식별 가능한 명확한 건강 단서다. Stage 1~8 전체에서 값 2는 정확히 2건(Stage 5, Stage 7)이다.
- 주인공과 친구에게 고유 이름을 주지 않는다. 모든 원고는 호칭으로만 지칭한다.
- 친구의 의료 사건에 확정 병명을 사용하지 않고 "급성 의료 사건"으로만 지칭한다. 의료진 설명은 시간선과 불가역성만 전달한다.
- 모든 대사와 자막은 문자열 테이블 키로 참조하며 원고를 코드나 씬에 직접 담지 않는다.
- Truth 데이터는 `Truth_HappyMemoriesWereReal`, `Truth_RemnantsReflectProtagonist`, `Truth_HospitalIncident`, `Truth_RemnantsAreMemoryFragments`, `Truth_DelayDidNotCauseLoss`를 포함한다.
- `Truth_DelayDidNotCauseLoss`의 RevealStage는 12다. Stage 13은 이를 재설명하지 않고 `AcceptanceState`를 `Avoiding→Approaching→Accepted`로 전환한다.
- 명확한 친구 건강 단서는 Stage 5 의료 연락, Stage 7의 애매한 호흡·떨림, Stage 9 이후 재맥락화 흔적으로 제한한다.
- 후반 회상은 과거 대사를 거짓으로 바꾸지 않고 당시 프레임 밖의 사물·메시지·머뭇거림을 추가한다.
- Stage 8의 세 “다음에”, Stage 10~12의 시간 기록, Stage 11의 주인공 형상, Stage 13의 파편 진실을 순서대로 회수한다.
- 친구를 악인, 조작자 또는 완벽한 유언을 남기는 장치로 만들지 않는다.

## Out of scope
- 최종 문장별 대본, 성우, 선택지 분기, 복수 엔딩
- 의료 사건의 확정 의학 명칭
- 인물 고유 이름과 외형 설정

## Acceptance criteria
- `Test_Narrative_RequiredTruthsDefined`가 필수 Truth 5개와 RevealStage를 확인한다.
- `Test_Narrative_HappyMemoriesNeverRetconnedFalse`가 기억의 진실성 플래그를 확인한다.
- `Test_Narrative_RevealOrderNeverRegresses`가 공개 단계 순서를 확인한다.
- `Test_Narrative_StageTwelveDisprovesDelayCausation`이 사건·도착 시간의 비인과를 확인한다.
- `Test_Narrative_StageThirteenAddsNoNewCausalTruth`가 Stage 13에서 새 인과 Truth 공개가 0개이고 AcceptanceState만 변경됨을 확인한다.
- `Test_Narrative_EarlyHealthCluesAreLimited`가 Stage 1~8에서 `HealthClueDirectness=2`인 기억이 정확히 2건이고 Stage 5와 Stage 7임을 확인한다.
- `Test_Narrative_NoCharacterProperNames`가 기억·대사 데이터에 인물 고유 이름이 0건임을 확인한다.
- `Test_Narrative_NoConfirmedDiagnosis`가 서사 데이터에 확정 병명이 0건임을 확인한다.
- `Test_Narrative_ForeshadowingHasPayoff`가 모든 ForeshadowingId에 회수 Stage가 있음을 확인한다.

## Verification method
- EditMode 서사 데이터·시간선 테스트
- Stage 8~13 사용자 서사 검수
