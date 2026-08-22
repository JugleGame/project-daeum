using UnityEngine;

namespace Game.Gameplay
{
    public sealed class PlayerBody : MonoBehaviour
    {
        private void Awake() { gameObject.AddComponent<Rigidbody2D>(); }
    }
}
