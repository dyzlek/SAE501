using System.Collections;
using UnityEngine;

namespace SAE
{
    // Une porte qui mène à un autre niveau : la porte de la cabane (vers la bananeraie) et le portail du retour.
    // On l'enfonce avec la main, on la vise et on appuie (RayPress), ou on clique dessus (mode PC).
    // Le battant s'ouvre en pivotant sur sa charnière, puis le joueur est envoyé dans l'autre niveau (Levels.Go) ;
    // la porte se referme derrière lui. Le script est sur le battant, dont l'origine est la charnière
    // (voir Blender/cabane.py, door_leaf).
    public class PortalDoor : MonoBehaviour, IPressable
    {
        public Level destination;
        public float openAngle = -100f;     // en degrés, autour de la verticale : le signe donne le sens (vers dehors)
        public float openDuration = 0.7f;   // en secondes

        Quaternion closed;
        bool opening;

        void Awake() => closed = transform.rotation;

        public void Press()
        {
            if (!opening) StartCoroutine(OpenAndGo());
        }

        IEnumerator OpenAndGo()
        {
            opening = true;
            var open = Quaternion.AngleAxis(openAngle, Vector3.up) * closed;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                // Elle démarre doucement et ralentit à la fin, comme une vraie porte qu'on pousse
                transform.rotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.rotation = open;
            yield return new WaitForSeconds(0.2f);   // le temps de voir dehors
            Levels.Go(destination);
            transform.rotation = closed;             // refermée derrière le joueur
            opening = false;
        }
    }
}
