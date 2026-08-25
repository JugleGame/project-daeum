using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// 스테이지에 처음 들어선 순간 화면 가운데에 독백을 한 줄씩 띄웠다가 지운다.
    /// </summary>
    /// <remarks>
    /// 왜 필요한가: 평범한 거리에서 아무 설명 없이 잔재와 트라우마가 나오면, 이야기를 모르는
    /// 플레이어에게는 "갑자기 괴물이 나온다"로만 읽힌다. 첫 독백이 "지금 상황이 이상하다"는 것을
    /// 먼저 알려 주어야 그 뒤의 전투가 개연성을 얻는다. 두 번째 줄은 진행 방향(오른쪽)을 안내해,
    /// 튜토리얼 문구 없이도 플레이어가 스스로 움직이게 한다.
    ///
    /// 원고는 코드가 아니라 StringTable에 둔다(spec-013 규칙). 키는 stage.opening.stageNN.01부터
    /// 번호순으로 찾고, 없는 번호가 나오면 멈춘다. 스테이지마다 줄 수가 달라도 코드를 고칠 일이 없고,
    /// 키를 하나도 넣지 않은 스테이지는 자연히 아무것도 띄우지 않는다.
    ///
    /// 씬 로드에 직접 반응하는 이유: HUD는 DontDestroyOnLoad라 스테이지가 바뀌어도 Start가 다시
    /// 오지 않는다. StageStateChanged(Explore)는 회상 종료·실패 복귀 때도 오기 때문에 "처음 들어옴"의
    /// 신호로 쓸 수 없다.
    /// </remarks>
    public sealed class StageOpeningLine : MonoBehaviour
    {
        [SerializeField] private Text lineText;
        [SerializeField] private GameObject lineRoot;

        [SerializeField, Min(0f)] private float fadeSeconds = 0.6f;
        [SerializeField, Min(0f)] private float holdSeconds = 2.2f;

        /// <summary>줄과 줄 사이의 빈 시간. 두 줄이 이어 붙어 한 문장처럼 읽히는 것을 막는다.</summary>
        [SerializeField, Min(0f)] private float gapSeconds = 0.35f;

        // 이미 독백을 보여 준 스테이지 번호. 같은 스테이지 씬이 다시 로드돼도 두 번 틀지 않는다.
        private readonly HashSet<int> shownStages = new();
        private Coroutine playing;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Hide();
            TryPlayLoadedStage();
        }

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryPlay(StageScenes.StageNumber(scene.name));

        /// <summary>이 오브젝트가 만들어지기 전에 이미 스테이지 씬이 열려 있던 경우를 처리한다.</summary>
        private void TryPlayLoadedStage()
        {
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded) TryPlay(StageScenes.StageNumber(scene.name));
            }
        }

        private void TryPlay(int stageId)
        {
            if (stageId <= 0 || !shownStages.Add(stageId)) return;

            var lines = CollectLines(stageId);
            if (lines.Count == 0) return;

            if (playing != null) StopCoroutine(playing);
            playing = StartCoroutine(Play(lines));
        }

        private static List<string> CollectLines(int stageId)
        {
            var lines = new List<string>();
            for (var order = 1; StringTable.TryGet($"stage.opening.stage{stageId:00}.{order:00}", out var line); order++)
            {
                lines.Add(line);
            }
            return lines;
        }

        private IEnumerator Play(List<string> lines)
        {
            if (lineRoot != null) lineRoot.SetActive(true);

            foreach (var line in lines)
            {
                if (lineText != null) lineText.text = line;

                yield return Fade(0f, 1f);
                // 실시간이 아니라 게임 시간을 쓴다. 회상 등으로 게임이 멈추면 독백도 함께 멈춰야 한다.
                yield return new WaitForSeconds(holdSeconds);
                yield return Fade(1f, 0f);
                yield return new WaitForSeconds(gapSeconds);
            }

            Hide();
            playing = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (var elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.deltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, elapsed / fadeSeconds));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (lineText == null) return;
            var color = lineText.color;
            color.a = alpha;
            lineText.color = color;
        }

        private void Hide()
        {
            SetAlpha(0f);
            if (lineRoot != null) lineRoot.SetActive(false);
        }
    }
}
