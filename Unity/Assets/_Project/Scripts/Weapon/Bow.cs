using UnityEngine;

namespace SAE
{
    // L'arc de Quincy (prefab Prefabs/Arc.prefab, modèle Art/Quincy/FBX/Quincy_Bow.fbx).
    // Il ne lit aucune touche : ce sont VRArcher (casque) et DesktopArcher (souris) qui le commandent.
    //   Nock()        : une flèche apparaît, encochée sur la corde ;
    //   SetDraw(t)    : la corde recule de t × maxDraw mètres, les branches plient, la flèche suit la corde ;
    //   Release(dir)  : la flèche part dans la direction dir, d'autant plus vite que l'arc était tendu
    //                   (avec le tir triple, deux flèches de plus partent de chaque côté, voir BowUpgrades).
    // Pendant la tension, une ligne montre la courbe que suivra la flèche, jusqu'au premier obstacle : on voit où l'on vise.
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
        public Transform aim;            // mode PC : la caméra, la flèche part vers le centre de l'écran ; vide en VR (sens de l'arc)
        public float reloadTime = 0.6f;  // secondes entre deux tirs : on ne peut plus « spammer » l'arc, comme un vrai arc qu'on recharge

        const float MinDraw = 0.25f;         // en dessous, la flèche tombe au lieu de partir : il faut vraiment tendre l'arc
        const int PreviewPoints = 50;        // points de la courbe de visée
        const float PreviewStep = 0.03f;     // secondes de vol entre deux points : 1,5 s de vol en tout

        public float Draw { get; private set; }   // tension actuelle, de 0 (repos) à 1 (pleine tension)
        public bool HasArrow => nocked;

        Vector3 stringRestLocal;          // position de la corde au repos, dans le repère de l'arc
        Vector3 shootLocal;               // sens du tir, dans le repère de l'arc (de la corde vers la poignée)
        Vector3 limbAxisLocal;            // axe des branches (de bas en haut), dans le repère de l'arc
        Quaternion topRest, bottomRest;
        Arrow nocked;
        float nextNock;                   // l'heure (Time.time) à partir de laquelle on peut encocher la flèche suivante
        LineRenderer preview;
        readonly Vector3[] previewPoints = new Vector3[PreviewPoints];

        public Vector3 ShootDirection => transform.TransformDirection(shootLocal);
        public Vector3 NockPoint => stringBone.position;
        Vector3 StringRest => transform.TransformPoint(stringRestLocal);
        Vector3 AimDirection => aim ? aim.forward : ShootDirection;
        float ShotSpeed => Mathf.Lerp(minSpeed, maxSpeed, Draw);

        void Awake()
        {
            Measure();
            preview = AddPreview();
        }

        void LateUpdate() => UpdatePreview();   // après les archers, qui ont réglé la tension pendant Update

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

        public bool CanNock => !nocked && Time.time >= nextNock;

        public void Nock()
        {
            if (!CanNock || !arrowPrefab) return;
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
                if (Draw > MinDraw)
                {
                    var velocity = direction.normalized * ShotSpeed;
                    nocked.transform.SetParent(null, true);
                    nocked.Launch(velocity);
                    nextNock = Time.time + reloadTime;   // on recharge : pas de nouvelle flèche avant reloadTime
                    if (BowUpgrades.TripleShot)
                    {
                        ExtraArrow(-BowUpgrades.TripleSpread, velocity, nocked.transform.localScale);
                        ExtraArrow(BowUpgrades.TripleSpread, velocity, nocked.transform.localScale);
                    }
                }
                else Destroy(nocked.gameObject);   // à peine tirée : on range la flèche
                nocked = null;
            }
            SetDraw(0f);
        }

        // Tir triple : une flèche de plus, tournée de 'angle' degrés autour de la verticale (à l'horizontale,
        // même si l'arc est penché). Même taille que la flèche encochée (l'arc du mode PC est réduit).
        void ExtraArrow(float angle, Vector3 velocity, Vector3 scale)
        {
            var v = Quaternion.AngleAxis(angle, Vector3.up) * velocity;
            var arrow = Instantiate(arrowPrefab, NockPoint, Quaternion.LookRotation(v));
            arrow.transform.localScale = scale;
            arrow.Launch(v);
        }

        // La courbe de visée : on simule le vol de la flèche (même vitesse, même gravité) par petits pas,
        // et on s'arrête au premier obstacle (Linecast ignore les triggers et le joueur, sur « Ignore Raycast »).
        void UpdatePreview()
        {
            preview.enabled = nocked && Draw > MinDraw;
            if (!preview.enabled) return;

            var position = NockPoint;
            var velocity = AimDirection * ShotSpeed;
            int count = 0;
            previewPoints[count++] = position;
            while (count < PreviewPoints)
            {
                var next = position + velocity * PreviewStep + 0.5f * PreviewStep * PreviewStep * Physics.gravity;
                velocity += Physics.gravity * PreviewStep;
                if (Physics.Linecast(position, next, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    previewPoints[count++] = hit.point;
                    break;
                }
                previewPoints[count++] = next;
                position = next;
            }
            preview.positionCount = count;
            preview.SetPositions(previewPoints);
        }

        // Ligne fine et dorée (le blanc se perdait dans le ciel), qui s'efface vers le bout : on voit la trajectoire
        // qu'aura la flèche, sans cacher la cible.
        LineRenderer AddPreview()
        {
            var go = new GameObject("Trajectoire");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = Visuals.LineMaterial;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.015f;
            line.startColor = new Color(1f, 0.75f, 0.2f, 0.9f);
            line.endColor = new Color(1f, 0.45f, 0.1f, 0.1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
            return line;
        }
    }
}
