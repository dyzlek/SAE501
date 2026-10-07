using UnityEngine;

namespace SAE
{
    // L'arc n'est dans les mains que sur la carte : au hub, on a juste les mains (pour prendre les singes,
    // les bananes, appuyer sur les boutons). Il apparaît quand on se téléporte sur la carte (bouton JOUER)
    // et disparaît au retour au hub. On réagit à l'événement PlayerRig.Teleported : rien à vérifier en boucle.
    public class BowHolster : MonoBehaviour
    {
        public Bow bow;

        void Start() => bow.gameObject.SetActive(false);   // on commence au hub

        void OnEnable() => PlayerRig.Teleported += OnTeleported;
        void OnDisable() => PlayerRig.Teleported -= OnTeleported;

        // L'arc sort quand on arrive sur la carte (le point d'arrivée de la scène Labyrinthe)
        void OnTeleported(Transform spot) =>
            bow.gameObject.SetActive(spot.TryGetComponent<LevelSpawn>(out var arrival) && arrival.level == Level.Carte);
    }
}
