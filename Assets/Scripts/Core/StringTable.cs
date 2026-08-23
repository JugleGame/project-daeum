using System;
using System.Collections.Generic;

namespace Daeume.Core
{
    /// <summary>
    /// 화면에 보이는 모든 문장을 한곳에 모아 두는 문자열 테이블이다. (spec-013, spec-008)
    ///
    /// 왜 이렇게 하나:
    /// UI 코드나 씬에 "체력" 같은 글자를 직접 박아 두면, 문장을 고칠 때마다 코드를 뒤져야 하고
    /// 나중에 번역을 붙일 수 없다. 그래서 코드·씬은 "키"만 들고 다니고 실제 문장은 여기서만 관리한다.
    /// spec-013의 검수 항목 Test_UI_NoHardcodedStrings가 바로 이 규칙을 확인한다.
    ///
    /// static 클래스인 이유: 씬에 올릴 필요가 없는 순수한 조회 기능이라, 어디서든 StringTable.Get(...)으로 부른다.
    /// </summary>
    public static class StringTable
    {
        // StringComparer.Ordinal: 키를 "글자 코드 그대로" 비교한다.
        // 문화권 설정(터키어 i 문제 등)에 따라 조회 결과가 달라지는 사고를 막고, 조회 속도도 가장 빠르다.
        private static readonly Dictionary<string, string> Korean = new(StringComparer.Ordinal)
        {
            // 상호작용 프롬프트 — 실제 화면에는 "[키] 문장" 형태로 조합된다(StageHudPresenter 참고).
            ["prompt.memory"] = "기억 살펴보기",
            // Stage 1 전용 InteractionVerb (spec-005: Stage마다 매개체 외형과 동사가 다르다).
            ["prompt.memory.stage01"] = "정류장 보관함 열기",
            // Stage 2 전용 InteractionVerb. 공용 prompt.memory로 되돌리면 spec-005의
            // "Stage마다 다른 동사" 규칙이 깨지므로 Stage02MemoryNarrativeTests가 이 키를 검사한다.
            ["prompt.memory.stage02"] = "교실 책상 서랍 열기",
            // Stage 3 전용 InteractionVerb. 동아리실 스피커를 켜는 외형과 동작을 짝지어 쓴다.
            ["prompt.memory.stage03"] = "동아리실 스피커 켜기",
            ["prompt.continue"] = "계속",
            ["prompt.memory.skip"] = "건너뛰기",

            // HUD 상시 표기
            ["hud.health"] = "체력",
            ["hud.chase"] = "도망치세요",
            ["hud.failed"] = "쓰러졌습니다... 체크포인트에서 다시 시작합니다.",
            ["hud.objective.memory"] = "오른쪽 끝의 기억을 찾아라",

            // Stage 1 튜토리얼 조작 안내. 키 이름은 실제 바인딩에서 읽어 오고, 여기에는 동작 이름만 둔다.
            ["hud.tutorial.hint"] = "조작",

            // 타이틀 화면
            ["title.loading"] = "불러오는 중…",
            ["title.retry"] = "잠시 후 다시 시도해 주세요.",
            ["title.heading"] = "다음에",
            ["title.subtitle"] = "기억은 사라지지 않고, 모양을 바꾼다",
            ["title.new_game"] = "새 게임",
            ["title.continue"] = "이어하기",
            ["title.hint"] = "Enter 또는 클릭으로 선택",
            ["title.settings"] = "설정",

            // 접근성 옵션 화면 (spec-013)
            ["options.heading"] = "접근성 옵션",
            ["options.close"] = "닫기",
            ["options.shake"] = "카메라 흔들림",
            ["options.subtitle_size"] = "자막 크기",
            ["options.chase_assist"] = "추격 속도 저하",
            ["options.rebind.move"] = "이동",
            ["options.rebind.jump"] = "점프",
            ["options.rebind.attack"] = "공격",
            ["options.rebind.grab"] = "붙잡기",
            ["options.rebind.interact"] = "상호작용",
            ["options.rebind.waiting"] = "키 입력 대기 중… (Esc로 취소)",

            // Stage 1 회상 원고 (spec-008: 인물 고유 이름 금지, 확정 병명 금지 규칙을 지킨 문장)
            ["memory.stage01.title"] = "기억의 조각 #1 — 정류장",
            ["memory.stage01.01"] = "보관함 안에 낡은 버스표 한 장. 돌아오는 편도, 여기서 끊었다.",
            ["memory.stage01.02"] = "누군가 이 자리에 오래 서서 나를 기다렸다. 그 온기가 잠깐 돌아온다.",
            ["memory.stage01.03"] = "기억을 손에 쥔 순간, 거리 끝에서 무언가 이쪽으로 걸어오기 시작한다.",

            // Stage 2 — 학교. 둘이 처음 만났던 순간.
            ["memory.stage02.title"] = "기억의 조각 #2 — 학교",
            ["memory.stage02.01"] = "서랍 안쪽에 이름표 두 개가 나란히 붙어 있다. 하나는 내 것이고, 하나는 그때 아직 모르던 이름이었다.",
            ["memory.stage02.02"] = "창가 자리에서 처음 눈이 마주쳤다. 무슨 말을 걸어야 할지 몰라 공책을 한 장 찢어 내밀었다.",
            ["memory.stage02.03"] = "그 이름을 소리 내어 불러 본 순간, 복도 끝의 발소리가 이쪽으로 방향을 튼다.",

            // Stage 3 — 동아리실. 둘이 처음 진짜 가까워지기 시작한 순간.
            ["memory.stage03.title"] = "기억의 조각 #3 — 동아리실",
            ["memory.stage03.01"] = "낡은 스피커의 전원을 켜자, 둘만 알던 노래의 첫 박자가 조용히 흘러나온다.",
            ["memory.stage03.02"] = "서툰 연습이 끝난 뒤에도 우리는 남아 같은 구절을 몇 번이나 다시 들었다.",
            ["memory.stage03.03"] = "그때 처음 아무 말 없이도 편안했다는 걸 떠올리자, 선반 너머의 그림자가 몸을 낮춘다."
        };

        /// <summary>
        /// 키에 해당하는 문장을 돌려준다. 없으면 "[키]" 형태로 돌려준다.
        /// </summary>
        /// <remarks>
        /// 없는 키를 빈 문자열로 처리하면 화면에서 조용히 사라져 누락을 눈치채지 못한다.
        /// 일부러 [키]를 그대로 보여 주어 QA 중에 바로 눈에 띄게 했다. 적합한 선택이다.
        /// </remarks>
        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return Korean.TryGetValue(key, out var value) ? value : $"[{key}]";
        }

        /// <summary>키 존재 여부까지 알아야 할 때 쓰는 조회. 실패해도 [키] 문자열을 만들지 않는다.</summary>
        public static bool TryGet(string key, out string value) => Korean.TryGetValue(key ?? string.Empty, out value);

        /// <summary>
        /// 실행 중에 문장을 추가·교체한다. 테스트에서 임시 문장을 넣을 때 주로 쓴다.
        /// </summary>
        /// <remarks>
        /// 검토 메모: static 사전을 실행 중에 바꾸는 방식이라, 테스트에서 넣은 값이 다음 테스트까지 남는다.
        /// 지금은 테스트가 각자 다른 키를 쓰고 있어 문제가 없지만, 공용 키를 덮어쓰는 테스트가 생기면
        /// 초기화 함수를 추가해야 한다.
        /// </remarks>
        public static void Register(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("String table key is required.", nameof(key));
            Korean[key] = value ?? string.Empty;
        }
    }
}
