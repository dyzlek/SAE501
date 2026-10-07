using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    public enum Level { Hub, Carte }

    // Un point d'arrivée : Spawn Hub (scène Hub) ou Spawn Carte (scène Labyrinthe).
    // Les deux niveaux sont dans deux scènes, et Unity ne peut pas enregistrer un lien d'une scène vers l'autre :
    // les boutons de téléportation, l'arc et le filet de chute retrouvent donc ces points par leur niveau, au moment voulu.
    public class LevelSpawn : MonoBehaviour
    {
        public Level level;

        public static readonly List<LevelSpawn> All = new List<LevelSpawn>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // Le point d'arrivée de ce niveau (null si sa scène n'est pas encore chargée)
        public static Transform Of(Level level)
        {
            foreach (var spawn in All)
                if (spawn.level == level) return spawn.transform;
            return null;
        }
    }
}
