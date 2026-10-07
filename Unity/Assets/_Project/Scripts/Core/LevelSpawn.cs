using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Le point d'arrivée de la scène : Spawn Hub (scène Hub) ou Spawn Carte (scène Labyrinthe).
    // Le filet de chute (FallGuard) y remet le joueur s'il tombe dans le vide.
    public class LevelSpawn : MonoBehaviour
    {
        public Level level;

        public static readonly List<LevelSpawn> All = new List<LevelSpawn>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
