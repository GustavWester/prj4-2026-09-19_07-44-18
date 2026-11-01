using System.Collections.Generic;
using UnityEngine;

namespace Tests.Netcode
{
    /// <summary>
    /// Spell der ikke flyver, men husker hvem der skød den, så testene kan tælle skud pr. maskine
    /// og kalde PlayerSpell.DealDamage direkte.
    /// </summary>
    public class TestSpell : PlayerSpell
    {
        public static readonly List<TestSpell> Launched = new();

        public PlayerManager Shooter => owner;

        public override void Launch(Vector2 dir, PlayerManager shooter)
        {
            owner = shooter;
            Launched.Add(this);
        }

        public void Hit(Collider2D other) => DealDamage(other);
    }
}
