using UnityEngine;

namespace SAE
{
    // Filet de sécurité : si le joueur tombe sous le sol (parti trop loin de la carte, ou passé par un trou),
    // on le remet au point d'arrivée le plus proche de l'endroit où il est tombé (le hub ou l'estrade de la carte).
    // Sans ça, il tombait sans fin et restait bloqué.
    [RequireComponent(typeof(PlayerRig))]
    public class FallGuard : MonoBehaviour
    {
        public const float FallHeight = -5f;   // en mètres : plus bas que tous les sols du jeu (sert aussi aux singes lancés)

        PlayerRig rig;

        void Awake() => rig = GetComponent<PlayerRig>();

        void Update()
        {
            if (transform.position.y > FallHeight) return;

            // Le point d'arrivée le plus proche, vu de dessus (on ne compte pas la hauteur de la chute)
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var spawn in LevelSpawn.All)   // Spawn Hub et Spawn Carte, chacun dans sa scène
            {
                var flat = spawn.transform.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude < bestDistance) { bestDistance = flat.sqrMagnitude; best = spawn.transform; }
            }
            if (best) rig.TeleportTo(best);
        }
    }
}
