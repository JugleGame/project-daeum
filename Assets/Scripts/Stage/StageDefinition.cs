using UnityEngine;

namespace Daeume.Stage
{
    public sealed class StageDefinition : MonoBehaviour
    {
        [SerializeField] private StageData data;

        public StageData Data => data;
    }
}
