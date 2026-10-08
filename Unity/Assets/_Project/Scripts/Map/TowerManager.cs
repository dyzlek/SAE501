using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Garde les singes de la carte synchronisés avec GameState.Placed :
    // un singe posé sur le plateau apparaît tout de suite sur la carte, même pendant une vague.
    public class TowerManager : MonoBehaviour
    {
        public const float TowerSize = 1.2f;

        readonly Dictionary<PlacedMonkey, GameObject> towers = new Dictionary<PlacedMonkey, GameObject>();

        void OnEnable()
        {
            GameState.Changed += Sync;
            Sync();
        }

        void OnDisable() => GameState.Changed -= Sync;

        void Sync()
        {
            // Retirer les singes qui ne sont plus posés (repris ou fusionnés)
            var gone = new List<PlacedMonkey>();
            foreach (var pair in towers)
                if (!GameState.Placed.Contains(pair.Key)) gone.Add(pair.Key);
            foreach (var p in gone)
            {
                Destroy(towers[p]);
                towers.Remove(p);
            }

            // Ajouter les nouveaux
            foreach (var p in GameState.Placed)
            {
                if (towers.ContainsKey(p)) continue;
                var pos = new Vector3(p.pos.x, TowerSize / 2f, p.pos.y);
                var go = Visuals.MonkeyPiece(p.monkey, transform, pos, TowerSize, withLabel: false);   // pas de texte au-dessus : la couleur dit la rareté, la fiche dit le reste
                go.tag = Tags.Singe;
                go.AddComponent<BoxCollider>().size = Vector3.one * TowerSize; // pour le prendre sur la carte
                go.AddComponent<TowerAnimator>().Init(p.monkey.type, TowerSize);   // avant Tower : Tower le cherche dans Init
                go.AddComponent<Tower>().Init(p.monkey);
                towers.Add(p, go);
            }
        }
    }
}
