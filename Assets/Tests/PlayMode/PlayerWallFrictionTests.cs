using System.Collections;
using Daeume.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    /// <summary>
    /// Issue #9 QA 확장: 벽에 막혀 멈춘 플레이어가 중력 없이 그대로 붙어 있던 버그 회귀 테스트.
    /// 그랩(IsGrabbing)과 무관하게, 이동 입력을 계속 벽 쪽으로 넣어도 낙하는 멈추면 안 된다.
    /// </summary>
    public sealed class PlayerWallFrictionTests
    {
        [UnityTest]
        public IEnumerator Test_Player_KeepsFallingWhilePushedIntoSolidWall()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            var player = Object.Instantiate(prefab);
            var controller = player.GetComponent<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = new Vector3(0f, 5f, 0f);

            // 오른쪽에 통과 불가능한 고체 벽을 붙여 둔다(트리거 아님 = GrabWall_Solid와 동일 조건).
            var wall = new GameObject("SolidWall");
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 10f);
            wall.transform.position = new Vector3(0.75f, 0f, 0f);

            Physics2D.SyncTransforms();
            var startY = body.position.y;

            // 계속 벽 쪽으로 이동 입력을 넣는다 - 예전 버그는 이 상태에서 낙하까지 멈췄다.
            for (var frame = 0; frame < 30; frame++)
            {
                controller.SetMoveInput(1f);
                yield return new WaitForFixedUpdate();
            }

            Assert.That(controller.IsGrabbing, Is.False, "This scenario must never enter the grab state.");
            Assert.That(body.position.y, Is.LessThan(startY - 0.3f), "Player must keep falling while pushed into a solid wall.");

            Object.Destroy(wall);
            Object.Destroy(player);
        }
    }
}
