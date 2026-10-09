using System.Collections.Generic;
using UnityEngine;

namespace SAE
{
    // Marque un endroit que la flèche du tutoriel peut montrer (un bouton, la bibliothèque, la porte...).
    // height : à quelle hauteur au-dessus de l'objet la flèche flotte.
    public class TutorialTarget : MonoBehaviour
    {
        public TutorialSpot spot;
        public Level level;                // le niveau où il se trouve : la flèche ne s'y montre que si on y est
        public float height = 0.35f;

        public static readonly List<TutorialTarget> All = new List<TutorialTarget>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static TutorialTarget Find(TutorialSpot spot)
        {
            foreach (var t in All) if (t.spot == spot) return t;
            return null;
        }
    }
}
