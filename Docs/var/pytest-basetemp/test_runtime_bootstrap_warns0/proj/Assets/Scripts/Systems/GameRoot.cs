using UnityEngine;

namespace Game.Gameplay
{
    public sealed class GameRoot : MonoBehaviour
    {
        private void Awake()
        {
            var go = new GameObject("Spawner");
            go.AddComponent<EnemySpawner>();
        }
    }
}
