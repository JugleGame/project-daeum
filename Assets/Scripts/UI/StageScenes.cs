using UnityEngine.SceneManagement;

namespace Daeume.UI
{
    /// <summary>
    /// "이 씬이 플레이 가능한 스테이지 씬인가"를 판별하는 한 곳. (#12)
    /// </summary>
    /// <remarks>
    /// SceneFlowController.StageSceneName이 만드는 이름 규칙("Stage02_Base")을 반대로 읽는 것뿐이다.
    /// 규칙을 여기 하나로 모아 두는 이유: 예전에는 부트스트랩마다 "Stage01_Base" 문자열을 따로 들고 있어서,
    /// Stage 02를 추가했을 때 조용히 어긋났다. StageVisualBootstrap이 그 이름으로 플레이어 물리를 켜고 껐는데
    /// Stage 02에서는 조건이 영영 참이 되지 않아 플레이어가 Kinematic으로 굳었다
    /// (중력 없음 → 접지 불가 → 점프 무효, 충돌 반응 없음 → 지형 통과).
    ///
    /// 스테이지가 늘어도 여기는 고칠 필요가 없다.
    /// </remarks>
    internal static class StageScenes
    {
        private const string Prefix = "Stage";
        private const string Suffix = "_Base";

        /// <summary>"Stage02_Base" 같은 스테이지 씬 이름이면 true.</summary>
        public static bool IsStageScene(string sceneName) => StageNumber(sceneName) > 0;

        /// <summary>씬 이름에서 스테이지 번호를 뽑는다. 규칙에 맞지 않으면 0.</summary>
        public static int StageNumber(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return 0;
            if (!sceneName.StartsWith(Prefix) || !sceneName.EndsWith(Suffix)) return 0;

            var digits = sceneName.Substring(Prefix.Length, sceneName.Length - Prefix.Length - Suffix.Length);
            return int.TryParse(digits, out var stageId) && stageId > 0 ? stageId : 0;
        }

        /// <summary>
        /// 지금 스테이지 씬이 하나라도 열려 있는지.
        /// </summary>
        /// <remarks>
        /// "방금 뭐가 로드/언로드됐나"가 아니라 "지금 스테이지가 떠 있나"를 물어야 한다.
        /// 오염 오버레이가 별도 씬이던 시절 그 적재/해제마다 콜백이 와서, 방금 로드된 씬 이름만 보면
        /// 오버레이가 뜰 때마다 플레이어가 Kinematic으로 바뀌는 버그가 있었다.
        /// </remarks>
        public static bool AnyLoaded()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && IsStageScene(scene.name)) return true;
            }

            return false;
        }
    }
}
