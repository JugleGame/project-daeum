using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    public sealed class TraumaContactSource : MonoBehaviour, IDamageable
    {
        public DamageTargetKind TargetKind => DamageTargetKind.Trauma;
        public DamageResult ApplyDamage(DamageRequest request) => new(false, 0);
    }
}
