using UnityEngine;

namespace SAE
{
    // Un personnage du menu principal (les singes de l'estrade, et Quincy) : il sautille, se dandine, fait un tour
    // sur lui-même de temps en temps, et BOUGE LES BRAS : la main droite fait coucou au joueur (bras levé, avant-bras
    // qui balance), la gauche se balance le long du corps. Le menu fige le jeu : tout est en temps réel (unscaledTime).
    // Les bras sont ceux des modèles riggés (os « RightArm », « RightForeArm », « RightHand »…) ; un modèle sans bras
    // (Canon, Tireur) ne fait que sautiller. Chaque image, on repart de la pose de repos, puis on oriente chaque os vers
    // la direction voulue (FromToRotation, comme Visuals.RestPose) : pas d'animation à préparer dans Unity.
    // Le personnage regarde vers -Z local de sa racine (comme tous les modèles du jeu).
    public class MenuMonkey : MonoBehaviour
    {
        public float phase;
        public bool wave = true;        // faux : il ne fait que danser
        public float hopHeight = 0.18f;

        Vector3 basePos;
        Quaternion baseRot;
        Arm right, left;

        class Arm
        {
            public Transform arm, foreArm, hand;
            public Quaternion armRest, foreRest;   // rotations locales de repos
            public float side;                     // +1 si ce bras est du côté +X de la racine, -1 sinon
        }

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            right = FindArm("Right");
            left = FindArm("Left");
        }

        Arm FindArm(string side)
        {
            var a = new Arm();
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (t.name.EndsWith(side + "Arm")) a.arm = t;
                if (t.name.EndsWith(side + "ForeArm")) a.foreArm = t;
                if (t.name.EndsWith(side + "Hand")) a.hand = t;
            }
            if (!a.arm || !a.foreArm || !a.hand) return null;
            a.armRest = a.arm.localRotation;
            a.foreRest = a.foreArm.localRotation;
            // Le personnage regarde vers -Z : son bras « droit » est du côté -X ; on le mesure plutôt que de le deviner
            a.side = Mathf.Sign(Vector3.Dot(a.arm.position - transform.position, transform.right));
            return a;
        }

        void Update()
        {
            float t = Time.unscaledTime * 2.2f + phase;
            float hop = Mathf.Abs(Mathf.Sin(t)) * hopHeight;                  // petits sauts
            float sway = Mathf.Sin(t * 0.5f) * 15f;                            // il se dandine
            float turn = Mathf.Repeat(t / 6f, 1f);
            float spin = turn > 0.9f ? (turn - 0.9f) * 10f : 0f;              // un tour toutes les ~17 s
            transform.localPosition = basePos + Vector3.up * hop;
            transform.localRotation = baseRot * Quaternion.Euler(0f, sway + spin * 360f, 0f);
            AnimateArms(Time.unscaledTime * 6f + phase);
        }

        void AnimateArms(float t)
        {
            var root = transform;
            if (right != null)
            {
                Vector3 armDir, foreDir;
                if (wave)
                {
                    // Bras levé sur le côté, avant-bras qui balance : coucou !
                    armDir = (root.up * 0.55f + root.right * right.side * 0.8f - root.forward * 0.15f).normalized;
                    foreDir = Quaternion.AngleAxis(Mathf.Sin(t) * 35f, root.forward) * root.up;
                }
                else
                {
                    armDir = (-root.up + root.right * right.side * 0.3f - root.forward * (0.15f + 0.3f * Mathf.Sin(t * 0.4f))).normalized;
                    foreDir = armDir;
                }
                Point(right, armDir, foreDir);
            }
            if (left != null)
            {
                // Le bras gauche se balance d'avant en arrière, le long du corps
                var dir = (-root.up + root.right * left.side * 0.3f - root.forward * 0.35f * Mathf.Sin(t * 0.4f)).normalized;
                Point(left, dir, dir);
            }
        }

        // Le bras vers armDir, l'avant-bras vers foreDir (directions dans le monde), à partir de la pose de repos
        static void Point(Arm a, Vector3 armDir, Vector3 foreDir)
        {
            a.arm.localRotation = a.armRest;
            a.foreArm.localRotation = a.foreRest;
            var current = a.foreArm.position - a.arm.position;
            if (current.sqrMagnitude > 1e-8f) a.arm.rotation = Quaternion.FromToRotation(current, armDir) * a.arm.rotation;
            var fore = a.hand.position - a.foreArm.position;
            if (fore.sqrMagnitude > 1e-8f) a.foreArm.rotation = Quaternion.FromToRotation(fore, foreDir) * a.foreArm.rotation;
        }
    }
}
