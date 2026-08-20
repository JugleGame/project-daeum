using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Flow
{
    /// <summary>
    /// 게임을 켰을 때 가장 먼저 실행되는 씬(Boot)에 놓이는 스크립트다. (spec-015)
    ///
    /// 하는 일은 딱 하나: 항상 살아 있어야 하는 Persistent 씬을 먼저 올리고, 그다음 Title 씬을 올린다.
    ///
    /// 왜 씬을 이렇게 나누나(유니티 처음이라면):
    /// - Persistent: GameManager·플레이어·카메라처럼 게임 내내 하나만 있어야 하는 것들
    /// - Title / Stage01_Base: 상황에 따라 바뀌는 내용
    /// 한 씬에 다 넣으면 스테이지를 바꿀 때마다 플레이어와 매니저가 파괴됐다 다시 생겨서
    /// 진행 상태가 끊긴다. 그래서 "항상 켜져 있는 씬 + 갈아 끼우는 씬" 구조를 쓴다.
    ///
    /// 주의: 이 순서 때문에 Stage01_Base 씬만 열고 Play를 누르면 매니저도 플레이어도 없어 아무것도 동작하지 않는다.
    /// 개발 중 확인은 반드시 Boot 씬에서 시작해야 한다.
    /// </summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private string persistentScene = "Persistent";
        [SerializeField] private string titleScene = "Title";

        /// <summary>
        /// Start를 코루틴(IEnumerator)으로 선언하면 유니티가 자동으로 코루틴으로 실행해 준다.
        /// 씬 적재는 즉시 끝나지 않으므로, 한 줄씩 "끝날 때까지 기다렸다가" 다음으로 넘어간다.
        /// </summary>
        private IEnumerator Start()
        {
            // 이미 열려 있으면 다시 열지 않는다.
            // 에디터에서 Persistent를 미리 열어 둔 채 Play를 누르는 경우를 대비한 검사다.
            if (!SceneManager.GetSceneByName(persistentScene).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Additive);
            }

            if (!SceneManager.GetSceneByName(titleScene).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(titleScene, LoadSceneMode.Additive);
            }

            var title = SceneManager.GetSceneByName(titleScene);
            if (title.IsValid())
            {
                // "활성 씬"은 새로 만든 오브젝트가 들어갈 기본 씬이자 조명 설정의 기준이다.
                // Title을 활성 씬으로 지정해야 이후 UI 생성과 씬 교체가 의도대로 동작한다.
                SceneManager.SetActiveScene(title);
            }
        }
    }
}
