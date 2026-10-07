using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SAE
{
    // Dans chaque scène : ce qui n'est allumé que quand le joueur est dans ce niveau (« presence » : le joueur VR et PC,
    // le soleil, les réglages d'image). Le reste de la scène (la carte et sa vague, le bananier, les récolteurs)
    // continue de tourner même quand on est dans l'autre niveau.
    // La présence est enregistrée ÉTEINTE dans la scène : c'est ce script qui l'allume si c'est le bon niveau
    // (sinon le joueur de la carte, chargée en arrière-plan, démarrerait et prendrait la place de celui du hub).
    public class LevelPresence : MonoBehaviour
    {
        public Level level;
        public GameObject presence;
        public Transform spawn;          // le point d'arrivée de ce niveau

        public static readonly List<LevelPresence> All = new List<LevelPresence>();

        void Awake()
        {
            Levels.StartIn(level);       // si cette scène est la première lancée, on commence ici
            Refresh();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // Allumée si c'est le niveau en cours ; en arrivant, le joueur est posé au point d'arrivée
        public void Refresh()
        {
            bool here = Levels.Current == level;
            bool wasHere = presence.activeSelf;
            presence.SetActive(here);
            if (!here) return;
            if (gameObject.scene.isLoaded) SceneManager.SetActiveScene(gameObject.scene);
            if (!wasHere && PlayerRig.Local) PlayerRig.Local.TeleportTo(spawn);
        }
    }
}
