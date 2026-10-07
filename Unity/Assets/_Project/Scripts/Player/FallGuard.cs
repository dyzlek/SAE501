using UnityEngine;

namespace SAE
{
    // Filet de sécurité : si le joueur tombe sous le sol (parti trop loin de la carte, ou passé par un trou),
    // on le remet au point d'arrivée le plus proche de l'endroit où il est tombé (le hub ou l'estrade de la carte).
    // Sans ça, il tombait sans fin et restait bloqué.
    [RequireComponent(typeof(PlayerRig))]
    public class FallGuard : MonoBehaviour
    {
        public Transform[] spawns;          // les points où le remettre (Spawn Hub, Spawn Carte)
        public float fallHeight = -5f;      // en mètres : plus bas que tous les sols du jeu

        PlayerRig rig;

        void Awake() => rig = GetComponent<PlayerRig>();

        void Update()
        {
            if (transform.position.y > fallHeight) return;

            // Le point d'arrivée le plus proche, vu de dessus (on ne compte pas la hauteur de la chute)
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var spawn in spawns)
            {
                if (!spawn) continue;
                var flat = spawn.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude < bestDistance) { bestDistance = flat.sqrMagnitude; best = spawn; }
            }
            if (best) rig.TeleportTo(best);
        }
    }
}
