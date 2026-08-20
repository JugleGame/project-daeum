using UnityEngine;

namespace Daeume.Stage
{
    /// <summary>
    /// "이 씬은 어떤 StageData를 쓰는가"를 씬 안에 표시해 두는 컴포넌트다. (spec-007)
    ///
    /// StageData는 에셋 파일(ScriptableObject)이고, 씬은 그 데이터를 가리키기만 한다.
    /// 이렇게 나눠 두면 레벨 구조를 건드리지 않고 수치·기억 ID·목표 추격 시간만 따로 조정할 수 있다.
    ///
    /// 유니티 처음이라면: ScriptableObject는 "씬에 놓지 않고 프로젝트에 파일로 존재하는 데이터 덩어리"다.
    /// 게임 실행 중에 새로 만들 필요가 없는 설정값을 담기에 적합하고, 여러 씬이 같은 파일을 공유할 수 있다.
    /// </summary>
    public sealed class StageDefinition : MonoBehaviour
    {
        [SerializeField] private StageData data;

        public StageData Data => data;
    }
}
