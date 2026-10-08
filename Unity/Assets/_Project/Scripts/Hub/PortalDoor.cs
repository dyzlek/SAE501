using System.Collections;
using UnityEngine;

namespace SAE
{
    // Une porte qui mène à un autre niveau : la porte de la cabane (vers la bananeraie) et le portail du retour.
    // On l'enfonce avec la main, on la vise et on appuie (RayPress), ou on clique dessus (mode PC).
    // Le battant s'ouvre en pivotant sur sa charnière ; à mi-ouverture, un voile noir descend du haut (ScreenCurtain),
    // le joueur est envoyé dans l'autre niveau (Levels.Go), puis le voile remonte ; la porte se referme derrière lui. Le script est sur le battant, dont l'origine est la charnière
    // (voir Blender/cabane.py, door_leaf).
    public class PortalDoor : MonoBehaviour, IPressable
    {
        public Level destination;
        public float openAngle = -100f;     // en degrés, autour de la verticale : le signe donne le sens (vers dehors)
        public float openDuration = 1f;     // en secondes
        public float curtainDuration = 1.2f;   // le voile noir, en secondes, dans chaque sens

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
            Coroutine curtain = null;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                // Elle démarre doucement et ralentit à la fin, comme une vraie porte qu'on pousse
                transform.rotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t));
                // À mi-ouverture, le voile noir commence à descendre du haut
                if (curtain == null && t >= 0.5f) curtain = StartCoroutine(ScreenCurtain.Slide(true, curtainDuration));
                yield return null;
            }
            transform.rotation = open;
            if (curtain == null) curtain = StartCoroutine(ScreenCurtain.Slide(true, curtainDuration));
            yield return curtain;                            // l'image est toute noire
            yield return new WaitForSeconds(0.15f);
            Levels.Go(destination);
            transform.rotation = closed;                     // refermée derrière le joueur
            yield return null;                               // le joueur est bien arrivé
            yield return ScreenCurtain.Slide(false, curtainDuration);   // le voile remonte : on découvre l'autre endroit
            opening = false;
        }
    }
}
