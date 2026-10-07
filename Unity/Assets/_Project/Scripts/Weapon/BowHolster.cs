using UnityEngine;

namespace SAE
{
    // L'arc n'est dans les mains que sur la carte : au hub, on a juste les mains (pour prendre les singes,
    // les bananes, appuyer sur les boutons). Il apparaît quand on arrive sur la carte (SE TP) et se range
    // au retour au hub. On réagit à l'événement Levels.Changed : rien à vérifier en boucle.
    public class BowHolster : MonoBehaviour
    {
        public Bow bow;

        void OnEnable() => Levels.Changed += Show;
        void OnDisable() => Levels.Changed -= Show;
        void Start() => Show(Levels.Current);

        void Show(Level level) => bow.gameObject.SetActive(level == Level.Carte);
    }
}
