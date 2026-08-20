using UnityEngine;

namespace Daeume.Interaction
{
    /// <summary>
    /// 화면에 띄울 프롬프트 정보. 문장이 아니라 "입력 액션 이름 + 문자열 테이블 키"만 담는다.
    /// </summary>
    /// <remarks>
    /// spec-010/013의 핵심 규칙이다. 여기에 "[E] 열기" 같은 완성된 문장을 담으면
    /// 키를 재설정했을 때 표시가 어긋나고 번역도 불가능해진다.
    /// </remarks>
    public readonly struct InteractionPrompt
    {
        public InteractionPrompt(string actionName, string stringTableKey)
        {
            ActionName = actionName;
            StringTableKey = stringTableKey;
        }

        public string ActionName { get; }
        public string StringTableKey { get; }
    }

    /// <summary>
    /// "조사할 수 있는 것"이 지켜야 할 약속. (spec-010)
    ///
    /// 상자·문·회상 매개체·Stage 13의 마지막 행동까지 전부 이 하나의 약속을 따른다.
    /// 그래서 상호작용 담당 코드는 대상이 무엇인지 몰라도 동일하게 처리할 수 있다.
    ///
    /// 유니티 처음이라면: interface는 "이 함수들을 반드시 갖고 있겠다"는 약속이다.
    /// 구현하는 쪽이 자유롭게 내용을 채우되, 사용하는 쪽은 약속된 함수만 호출한다.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>대상을 구분하는 안정 ID. 동률일 때의 선택 순서와 저장에도 쓰인다.</summary>
        string StableId { get; }

        /// <summary>지금 이 대상을 조사할 수 있는가(이미 완료됐거나 상태가 맞지 않으면 false).</summary>
        bool CanInteract(GameObject interactor);

        InteractionPrompt GetPrompt();

        void Interact(GameObject interactor);
    }
}
