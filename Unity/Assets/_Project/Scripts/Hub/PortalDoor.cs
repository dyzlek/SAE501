using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // La porte de la cabane, qui téléporte dans les deux sens : depuis la cabane vers la bananeraie (juste derrière, au bout
    // de l'allée), et depuis l'allée vers la cabane.
    // On l'enfonce avec la main, on la vise et on appuie (RayPress), ou on clique dessus (mode PC).
    // Le battant s'ouvre en pivotant sur sa charnière (on voit ce qu'il y a derrière) ; dès qu'il s'entrouvre, un voile
    // noir descend du haut (ScreenCurtain), le joueur est posé à l'arrivée, puis le voile remonte ; la porte se referme.
    // Le script est sur le battant, dont l'origine est la charnière (voir Blender/cabane.py, door_leaf).
    public class PortalDoor : MonoBehaviour, IPressable
    {
        public Transform arrival;           // où le joueur est posé quand il sort (et dans quel sens il regarde)
        public Transform returnArrival;     // où il est posé quand il rentre
        public Vector3 insideCenter;        // le centre de la pièce : à moins de insideRadius, le joueur est dedans
        public float insideRadius = 3.4f;
        public float openAngle = -100f;     // en degrés, autour de la verticale : le signe donne le sens (vers dehors)
        public float openDuration = 1f;     // en secondes

        const float CurtainStart = 0.3f;    // le voile démarre à 30 % de l'ouverture (la porte entrouverte d'un tiers)

        // Visée au rayon, la porte s'entrouvre un peu et frémit : on comprend qu'elle s'ouvre
        public float peekAngle = 8f;        // en degrés, dans le sens de l'ouverture
        public float peekSpeed = 4f;        // vitesse d'entrouverture / de fermeture
        const float WobbleAngle = 1.5f, WobbleSpeed = 7f;

        Quaternion closed;
        bool opening;
        XRSimpleInteractable interactable;
        float peek;                         // 0 = fermée, 1 = entrouverte

        void Awake()
        {
            closed = transform.rotation;
            interactable = GetComponent<XRSimpleInteractable>();
        }

        void Update()
        {
            if (opening) { peek = 0f; return; }   // pendant l'ouverture, c'est OpenAndGo qui la tourne
            bool aimed = interactable && interactable.isHovered;
            float before = peek;
            peek = Mathf.MoveTowards(peek, aimed ? 1f : 0f, peekSpeed * Time.deltaTime);
            if (peek == 0f && before == 0f) return;   // fermée et pas visée : on n'y touche pas
            float wobble = aimed ? Mathf.Sin(Time.time * WobbleSpeed) * WobbleAngle : 0f;
            float angle = Mathf.Sign(openAngle) * (Mathf.SmoothStep(0f, 1f, peek) * peekAngle + wobble * peek);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * closed;
        }

        public void Press()
        {
            var player = PlayerRig.Local;
            if (opening || ScreenCurtain.Busy || !player) return;
            var flat = player.transform.position - insideCenter;
            flat.y = 0f;
            var target = flat.magnitude < insideRadius ? arrival : returnArrival;   // dedans : on sort ; dehors : on rentre
            if (target) StartCoroutine(OpenAndGo(target));
        }

        IEnumerator OpenAndGo(Transform target)
        {
            opening = true;
            var open = Quaternion.AngleAxis(openAngle, Vector3.up) * closed;
            var from = transform.rotation;           // peut-être déjà entrouverte (visée)
            Coroutine curtain = null;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                // Elle démarre doucement et ralentit à la fin, comme une vraie porte qu'on pousse
                transform.rotation = Quaternion.Slerp(from, open, Mathf.SmoothStep(0f, 1f, t));
                // Quand elle commence à s'ouvrir (un petit tiers), le voile noir commence à descendre du haut
                if (curtain == null && t >= CurtainStart) curtain = StartCoroutine(ScreenCurtain.Slide(true, ScreenCurtain.Duration));
                yield return null;
            }
            transform.rotation = open;
            if (curtain == null) curtain = StartCoroutine(ScreenCurtain.Slide(true, ScreenCurtain.Duration));
            yield return curtain;                    // l'image est toute noire
            yield return ScreenCurtain.Teleport(() =>
            {
                if (PlayerRig.Local) PlayerRig.Local.TeleportTo(target);
                transform.rotation = closed;         // refermée derrière le joueur
            }, coverStarted: true);
            opening = false;
        }
    }
}
