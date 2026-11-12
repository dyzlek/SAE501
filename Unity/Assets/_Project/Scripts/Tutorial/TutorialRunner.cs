using UnityEngine;

namespace SAE
{
    // Fait avancer le tutoriel (Tutorial.Tick) et montre ce qu'il faut toucher. Un seul, dans la scène Hub
    // (toujours chargée : c'est celle du joueur). Trois indications, pour qu'on ne puisse pas les rater :
    //   - la flèche dorée (un chevron pointé vers le bas) au-dessus de la cible : elle rebondit, tourne et pulse ;
    //   - un rayon de lumière dorée qui monte de la cible vers le ciel : on le voit de loin, même derrière soi en se tournant ;
    //   - une flèche au sol, à nos pieds, qui montre la direction de la cible quand elle est loin (plus de 2,5 m).
    //     Au sol plutôt que devant les yeux : rien n'est collé au visage (règles de confort).
    // La flèche montre la cible la plus proche du joueur dans le niveau où il est (il y a un tableau au hub et un dans la
    // bananeraie), ou, pour le premier singe, sa case dans la bibliothèque (LibrarySlot).
    public class TutorialRunner : MonoBehaviour
    {
        public Transform pointer;          // la flèche (construite par PrototypeGenerator)

        const float BounceHeight = 0.12f, BounceSpeed = 4f, SpinSpeed = 90f;
        const float Pulse = 0.15f;         // la flèche grossit et rétrécit de 15 %
        const float SlotHeight = 0.3f;     // au-dessus de la case du singe
        const float BeamHeight = 8f;       // en mètres : le rayon au-dessus de la cible
        const float GuideDistance = 0.7f;  // en mètres : la flèche au sol, devant les pieds, vers la cible
        const float GuideMin = 2.5f;       // en mètres : plus près que ça, on voit la cible, pas besoin de la flèche au sol
        static readonly Color Gold = new Color(1f, 0.8f, 0.2f);

        LineRenderer beam;
        Transform guide;                   // la flèche au sol
        Vector3 pointerScale;

        void Start()
        {
            if (!pointer) return;
            pointerScale = pointer.localScale;
            beam = BuildBeam();
            guide = BuildGuide();
        }

        void Update()
        {
            Tutorial.Tick();
            if (!pointer || !beam) return;

            var position = Vector3.zero;
            bool show = Tutorial.Showing && Where(Tutorial.Spot(Tutorial.Current), out position);
            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            if (beam.gameObject.activeSelf != show) beam.gameObject.SetActive(show);
            ShowGuide(show, position);
            if (!show) return;

            float bounce = Mathf.Abs(Mathf.Sin(Time.time * BounceSpeed)) * BounceHeight;
            pointer.position = position + Vector3.up * bounce;
            pointer.rotation = Quaternion.Euler(0f, Time.time * SpinSpeed, 0f);
            pointer.localScale = pointerScale * (1f + Pulse * Mathf.Sin(Time.time * BounceSpeed * 2f));
            beam.SetPosition(0, position + Vector3.up * 0.25f);
            beam.SetPosition(1, position + Vector3.up * BeamHeight);
        }

        // La flèche au sol : à nos pieds, tournée vers la cible, seulement si elle est loin
        void ShowGuide(bool show, Vector3 target)
        {
            var rig = PlayerRig.Local;
            bool far = false;
            var to = Vector3.zero;
            if (show && rig)
            {
                to = target - rig.HeadPosition;
                to.y = 0f;
                far = to.magnitude > GuideMin;
            }
            if (guide.gameObject.activeSelf != far) guide.gameObject.SetActive(far);
            if (!far) return;
            var head = rig.HeadPosition;
            var feet = new Vector3(head.x, rig.transform.position.y + 0.03f, head.z);   // un peu au-dessus du sol
            var dir = to.normalized;
            float slide = Mathf.Repeat(Time.time * 0.6f, 1f) * 0.25f;   // elle avance un peu, puis recommence : « par là ! »
            guide.SetPositionAndRotation(feet + dir * (GuideDistance + slide), Quaternion.LookRotation(dir));
        }

        // Le rayon : une ligne dorée verticale, plus large en bas et qui s'efface en montant
        LineRenderer BuildBeam()
        {
            var line = new GameObject("Rayon du tutoriel").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = Visuals.LineMaterial;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.08f;
            line.endWidth = 0.02f;
            line.startColor = Gold;
            line.endColor = new Color(Gold.r, Gold.g, Gold.b, 0f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.gameObject.SetActive(false);
            return line;
        }

        // La flèche au sol : un chevron doré à plat (deux branches), la pointe vers +Z
        Transform BuildGuide()
        {
            var root = new GameObject("Flèche au sol du tutoriel").transform;
            root.SetParent(transform, false);
            foreach (int side in new[] { -1, 1 })
            {
                var bar = Visuals.Box("Branche", root, new Vector3(side * 0.07f, 0f, 0f), new Vector3(0.05f, 0.015f, 0.24f), Gold);
                bar.transform.localRotation = Quaternion.Euler(0f, -side * 40f, 0f);
                foreach (var c in bar.GetComponentsInChildren<Collider>()) Destroy(c);   // on ne marche pas dessus, la souris la traverse
            }
            root.gameObject.SetActive(false);
            return root;
        }

        // Où poser la flèche pour cet endroit (false : rien à montrer dans le niveau où est le joueur)
        static bool Where(TutorialSpot spot, out Vector3 position)
        {
            position = default;
            if (spot == TutorialSpot.None) return false;
            if (spot == TutorialSpot.FirstMonkey)
            {
                var slot = LibrarySlot.Find(new Monkey(MonkeyType.Classique, Rarity.Gris));
                if (!slot || Levels.Current != Level.Hub) return false;
                position = slot.transform.position + Vector3.up * SlotHeight;
                return true;
            }
            var target = TutorialTarget.Nearest(spot, Levels.Current, PlayerRig.Local ? PlayerRig.Local.HeadPosition : Vector3.zero);
            if (!target) return false;
            position = target.transform.position + Vector3.up * target.height;
            return true;
        }
    }
}
