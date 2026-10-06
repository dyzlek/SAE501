using UnityEngine;

namespace SAE
{
    // L'arc de Quincy (prefab Prefabs/Arc.prefab, modèle Art/Quincy/FBX/Quincy_Bow.fbx).
    // Il ne lit aucune touche : ce sont VRArcher (casque) et DesktopArcher (souris) qui le commandent.
    //   Nock()        : une flèche apparaît, encochée sur la corde ;
    //   SetDraw(t)    : la corde recule de t × maxDraw mètres, les branches plient, la flèche suit la corde ;
    //   Release(dir)  : la flèche part dans la direction dir, d'autant plus vite que l'arc était tendu.
    // Les os du modèle sont importés à l'échelle ×100 : on les déplace donc toujours en coordonnées monde (en mètres).
    public class Bow : MonoBehaviour
    {
        public Transform stringBone;     // l'os « String » : le milieu de la corde
        public Transform limbTop;        // les deux branches, qui plient quand on tire
        public Transform limbBottom;
        public Transform grip;           // la poignée : c'est là que l'arc est tenu
        public Arrow arrowPrefab;

        public float maxDraw = 0.4f;     // recul maximum de la corde, en mètres
        public float minSpeed = 8f;      // vitesse de la flèche à peine tendue, en m/s
        public float maxSpeed = 30f;     // vitesse de la flèche à pleine tension, en m/s
        public float limbBend = 12f;     // angle des branches à pleine tension, en degrés

        public float Draw { get; private set; }   // tension actuelle, de 0 (repos) à 1 (pleine tension)
        public bool HasArrow => nocked;

        Vector3 stringRestLocal;          // position de la corde au repos, dans le repère de l'arc
        Vector3 shootLocal;               // sens du tir, dans le repère de l'arc (de la corde vers la poignée)
        Vector3 limbAxisLocal;            // axe des branches (de bas en haut), dans le repère de l'arc
        Quaternion topRest, bottomRest;
        Arrow nocked;

        public Vector3 ShootDirection => transform.TransformDirection(shootLocal);
        public Vector3 NockPoint => stringBone.position;
        Vector3 StringRest => transform.TransformPoint(stringRestLocal);

        void Awake() => Measure();

        // Relève la forme de l'arc au repos (corde, sens du tir, axe des branches) dans son propre repère.
        // Appelé au lancement, et aussi par le générateur de scène hors Play (où Awake ne tourne pas).
        void Measure()
        {
            stringRestLocal = transform.InverseTransformPoint(stringBone.position);
            shootLocal = transform.InverseTransformDirection(grip.position - stringBone.position).normalized;
            limbAxisLocal = transform.InverseTransformDirection(limbTop.position - limbBottom.position).normalized;
            topRest = limbTop.localRotation;
            bottomRest = limbBottom.localRotation;
        }

        // Tient l'arc dans 'hand' : la poignée à localOffset dans la main, le tir vers l'avant de la main,
        // les branches vers le haut.
        public void HoldIn(Transform hand, Vector3 localOffset)
        {
            Measure();
            transform.SetParent(hand, false);
            transform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(shootLocal, limbAxisLocal));
            transform.localPosition = Vector3.zero;
            transform.localPosition = localOffset - hand.InverseTransformPoint(grip.position);
        }

        public void Nock()
        {
            if (nocked || !arrowPrefab) return;
            nocked = Instantiate(arrowPrefab, NockPoint, Quaternion.LookRotation(ShootDirection), transform);
            nocked.Hold();
        }

        // Tension (0 à 1) quand la main qui tire est en handPosition : de combien elle a reculé
        // derrière la corde au repos, dans le sens du tir.
        public float TensionFor(Vector3 handPosition) =>
            Mathf.Clamp01(Vector3.Dot(StringRest - handPosition, ShootDirection) / maxDraw);

        public void SetDraw(float amount)
        {
            Draw = Mathf.Clamp01(amount);
            var shoot = ShootDirection;
            stringBone.position = StringRest - shoot * (Draw * maxDraw);

            // Les branches plient vers l'archer, autour de l'axe perpendiculaire au tir et aux branches
            var bendAxis = transform.TransformDirection(Vector3.Cross(shootLocal, limbAxisLocal));
            float angle = Draw * limbBend;
            limbTop.localRotation = topRest;
            limbBottom.localRotation = bottomRest;
            limbTop.Rotate(bendAxis, angle, Space.World);
            limbBottom.Rotate(bendAxis, -angle, Space.World);

            if (nocked) nocked.transform.SetPositionAndRotation(NockPoint, Quaternion.LookRotation(shoot));
        }

        // Lâche la corde : la flèche part (si l'arc était assez tendu), la corde revient au repos.
        public void Release(Vector3 direction)
        {
            if (nocked)
            {
                if (Draw > 0.1f)
                {
                    nocked.transform.SetParent(null, true);
                    nocked.Launch(direction.normalized * Mathf.Lerp(minSpeed, maxSpeed, Draw));
                }
                else Destroy(nocked.gameObject);   // à peine tirée : on range la flèche
                nocked = null;
            }
            SetDraw(0f);
        }
    }
}
