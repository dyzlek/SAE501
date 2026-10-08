using System.Collections;
using UnityEngine;

namespace SAE
{
    // Une porte qui téléporte : la porte de la cabane (vers la bananeraie, juste derrière) et le portail du retour.
    // On l'enfonce avec la main, on la vise et on appuie (RayPress), ou on clique dessus (mode PC).
    // Le battant s'ouvre en pivotant sur sa charnière (on voit ce qu'il y a derrière) ; dès qu'il s'entrouvre, un voile
    // noir descend du haut (ScreenCurtain), le joueur est posé à l'arrivée, puis le voile remonte ; la porte se referme.
    // Le script est sur le battant, dont l'origine est la charnière (voir Blender/cabane.py, door_leaf).
    public class PortalDoor : MonoBehaviour, IPressable
    {
        public Transform arrival;           // où le joueur est posé (et dans quel sens il regarde)
        public float openAngle = -100f;     // en degrés, autour de la verticale : le signe donne le sens (vers dehors)
        public float openDuration = 1f;     // en secondes

        const float CurtainStart = 0.3f;    // le voile démarre à 30 % de l'ouverture (la porte entrouverte d'un tiers)

        Quaternion closed;
        bool opening;

        void Awake() => closed = transform.rotation;

        public void Press()
        {
            if (!opening && !ScreenCurtain.Busy && arrival) StartCoroutine(OpenAndGo());
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
                // Quand elle commence à s'ouvrir (un petit tiers), le voile noir commence à descendre du haut
                if (curtain == null && t >= CurtainStart) curtain = StartCoroutine(ScreenCurtain.Slide(true, ScreenCurtain.Duration));
                yield return null;
            }
            transform.rotation = open;
            if (curtain == null) curtain = StartCoroutine(ScreenCurtain.Slide(true, ScreenCurtain.Duration));
            yield return curtain;                    // l'image est toute noire
            yield return ScreenCurtain.Teleport(() =>
            {
                if (PlayerRig.Local) PlayerRig.Local.TeleportTo(arrival);
                transform.rotation = closed;         // refermée derrière le joueur
            }, coverStarted: true);
            opening = false;
        }
    }
}
