using UnityEngine;

namespace SAE
{
    // L'arc n'est dans les mains que sur la carte : au hub, on a juste les mains (pour prendre les singes,
    // les bananes, appuyer sur les boutons). Il apparaît quand on se téléporte sur la carte (bouton JOUER)
    // et disparaît au retour au hub. On réagit à l'événement PlayerRig.Teleported : rien à vérifier en boucle.
    public class BowHolster : MonoBehaviour
    {
        public Bow bow;
        public Transform mapSpawn;   // le point d'arrivée sur la carte

        void Start() => bow.gameObject.SetActive(false);   // on commence au hub

        void OnEnable() => PlayerRig.Teleported += OnTeleported;
        void OnDisable() => PlayerRig.Teleported -= OnTeleported;

        void OnTeleported(Transform spot) => bow.gameObject.SetActive(spot == mapSpawn);
    }
}
