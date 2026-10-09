using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Marque un endroit que la flèche du tutoriel peut montrer (un bouton, la bibliothèque, la porte, un tableau...).
    // height : à quelle hauteur au-dessus de l'objet la flèche flotte.
    public class TutorialTarget : MonoBehaviour
    {
        public TutorialSpot spot;
        public Level level;                // le niveau où il se trouve : la flèche ne s'y montre que si on y est
        public float height = 0.35f;

        public static readonly List<TutorialTarget> All = new List<TutorialTarget>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // La cible de cet endroit dans ce niveau, la plus proche de from (null s'il n'y en a pas)
        public static TutorialTarget Nearest(TutorialSpot spot, Level level, Vector3 from)
        {
            TutorialTarget best = null;
            float bestSqr = float.MaxValue;
            foreach (var t in All)
            {
                if (t.spot != spot || t.level != level) continue;
                float d = (t.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = t; }
            }
            return best;
        }
    }
}
