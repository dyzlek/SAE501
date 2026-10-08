using UnityEngine;

namespace SAE
{
    // Les animations d'un singe posé, faites en code (les modèles ont un squelette mais aucune animation),
    // sur le même principe que le singe récolteur (HarvesterAnimator) : chaque image, on remet les os dans leur pose
    // de départ, puis on oriente les bras vers une direction (« ce bras pointe vers l'avant »).
    //  - au repos : il respire, regarde autour de lui, la queue ondule, les bras pendent ;
    //  - il se tourne vers le ballon qu'il vise ;
    //  - il tire, chacun à sa façon : le Classique et la Colle lancent par-dessus l'épaule, le Boomerang lance de côté,
    //    le Sniper épaule et recule au coup de feu, la Glace lève les bras et frappe le sol ;
    //    le Canon et le Tireur de punaises (des machines, sans os) reculent ou tournent sur eux-mêmes.
    // Le projectile part au moment où le bras passe devant (ReleaseDelay) : Tower attend ce moment pour le lancer.
    public class TowerAnimator : MonoBehaviour
    {
        public const float ReleaseDelay = 0.15f;   // secondes entre le début du geste et le départ du projectile
        const float AttackDuration = 0.35f;       // durée du geste de tir
        const float TurnSpeed = 360f;              // degrés par seconde pour se tourner vers la cible

        MonkeyType type;
        float size;
        Transform visual;                          // le modèle 3D (ou le cube) : rebondit, recule, s'écrase
        Transform leftArm, leftForeArm, rightArm, rightForeArm, rightHand, head, tail;
        Transform[] bones;
        Quaternion[] rest;
        Vector3 restPosition, restScale;
        Vector3 aimPoint;
        bool hasAim;
        float attackAge = -1f;                     // -1 : pas de tir en cours
        float seed;

        public void Init(MonkeyType monkeyType, float monkeySize)
        {
            type = monkeyType;
            size = monkeySize;
        }

        void Start()
        {
            var view = GetComponent<MonkeyView>();
            visual = view.model ? view.model.transform : view.body.transform;
            restPosition = visual.localPosition;
            restScale = visual.localScale;
            seed = Random.value * 10f;

            leftArm = Bone("LeftArm"); leftForeArm = Bone("LeftForeArm");
            rightArm = Bone("RightArm"); rightForeArm = Bone("RightForeArm"); rightHand = Bone("RightHand");
            head = Bone("Head"); tail = Bone("Tail1");
            bones = new[] { leftArm, rightArm, head, tail };
            rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) if (bones[i]) rest[i] = bones[i].localRotation;

            // Un Animator sans contrôleur pourrait remettre la pose de départ par-dessus la nôtre
            var animator = visual.GetComponentInChildren<Animator>();
            if (animator) animator.enabled = false;
        }

        // Un os du modèle, ou null (Canon et Tireur n'en ont pas : on anime alors le modèle en entier).
        Transform Bone(string boneName)
        {
            foreach (var t in visual.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(boneName)) return t;
            return null;
        }

        // D'où part le projectile : la main droite si le singe en a une, sinon le haut du modèle, un peu devant.
        public Vector3 LaunchPoint => rightHand ? rightHand.position : transform.position + Vector3.up * size * 0.3f + Forward * size * 0.3f;

        // Le modèle regarde vers -Z (voir Visuals.MonkeyPiece) : son « devant » et sa « droite » sont donc inversés.
        Vector3 Forward => -transform.forward;
        Vector3 Right => -transform.right;

        // Appelé par Tower quand il tire : le singe se tourne vers ce point et fait son geste.
        public void Attack(Vector3 target)
        {
            aimPoint = target;
            hasAim = true;
            attackAge = 0f;
        }

        void LateUpdate()
        {
            if (attackAge >= 0f) attackAge += Time.deltaTime;
            if (attackAge > AttackDuration) attackAge = -1f;
            float k = attackAge >= 0f ? attackAge / AttackDuration : -1f;   // avancement du geste (0 → 1), -1 au repos
            float bump = k >= 0f ? Mathf.Sin(k * Mathf.PI) : 0f;           // 0 → 1 → 0 pendant le geste

            for (int i = 0; i < bones.Length; i++) if (bones[i]) bones[i].localRotation = rest[i];
            visual.localPosition = restPosition;
            visual.localScale = restScale;

            FaceTarget(k);

            // Au repos : il respire (le corps monte et descend un peu)
            visual.localPosition += Vector3.up * Mathf.Sin(Time.time * 2f + seed) * 0.012f * size;

            var up = Vector3.up;
            switch (type)
            {
                case MonkeyType.Classique:
                case MonkeyType.Colle:
                    Overhead(k);
                    break;
                case MonkeyType.Boomerang:
                    Sidearm(k);
                    break;
                case MonkeyType.Sniper:
                    // Il tient toujours son fusil épaulé ; au coup de feu, tout le corps recule
                    AimArm(rightArm, rightForeArm, (Forward + up * 0.1f).normalized);
                    AimArm(leftArm, leftForeArm, (Forward - Right * 0.2f + up * 0.05f).normalized);
                    visual.localPosition += transform.InverseTransformDirection(-Forward) * bump * 0.12f * size;
                    break;
                case MonkeyType.Glace:
                    // Les deux bras se lèvent, puis frappent vers le sol ; le corps s'écrase un peu au moment du coup
                    var raised = (up + Forward * 0.2f).normalized;
                    var slam = (Forward - up * 0.6f).normalized;
                    var arms = k < 0f ? Hanging(0f) : (k < 0.5f ? Vector3.Slerp(Hanging(0f), raised, k * 2f) : Vector3.Slerp(raised, slam, (k - 0.5f) * 2f));
                    AimArm(leftArm, leftForeArm, Side(arms, leftArm));
                    AimArm(rightArm, rightForeArm, Side(arms, rightArm));
                    if (k > 0.5f) visual.localScale = Vector3.Scale(restScale, new Vector3(1f + bump * 0.1f, 1f - bump * 0.15f, 1f + bump * 0.1f));
                    break;
                case MonkeyType.Canon:
                    // Le canon recule d'un coup puis revient, et s'écrase un peu
                    visual.localPosition += transform.InverseTransformDirection(-Forward) * bump * 0.15f * size;
                    visual.localScale = Vector3.Scale(restScale, new Vector3(1f + bump * 0.08f, 1f - bump * 0.08f, 1f + bump * 0.08f));
                    break;
                case MonkeyType.Punaise:
                    // Le tireur de punaises gonfle et tourne sur lui-même quand il crache ses punaises
                    visual.localScale = restScale * (1f + bump * 0.15f);
                    break;
            }

            // La tête regarde un peu à droite, un peu à gauche (au repos) ; la queue ondule tout le temps
            if (head && k < 0f) head.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 0.7f + seed) * 20f, up) * head.rotation;
            if (tail) tail.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 3f + seed) * 20f, up) * tail.rotation;
        }

        // Se tourner vers la cible (sauf le Tireur, qui tire tout autour de lui : il tourne sur lui-même en tirant).
        void FaceTarget(float k)
        {
            if (type == MonkeyType.Punaise)
            {
                if (k >= 0f) transform.Rotate(0f, 720f * Time.deltaTime, 0f);
                return;
            }
            if (!hasAim) return;
            var away = transform.position - aimPoint;   // le modèle regarde vers -Z : on tourne le dos du repère vers la cible
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(away), TurnSpeed * Time.deltaTime);
        }

        // Lancer par-dessus l'épaule : le bras droit part en arrière puis passe devant ; le gauche pend.
        void Overhead(float k)
        {
            AimArm(leftArm, leftForeArm, Side(Hanging(0f), leftArm));
            var up = Vector3.up;
            var back = (up - Forward * 0.7f).normalized;
            var front = (Forward + up * 0.3f).normalized;
            var dir = k < 0f ? Side(Hanging(Mathf.Sin(Time.time * 2f + seed) * 0.15f), rightArm)
                     : (k < 0.45f ? Vector3.Slerp(Hanging(0f), back, k / 0.45f) : Vector3.Slerp(back, front, (k - 0.45f) / 0.55f));
            AimArm(rightArm, rightForeArm, dir);
        }

        // Lancer de côté (boomerang) : le bras droit s'ouvre sur le côté puis balaie vers l'avant.
        void Sidearm(float k)
        {
            AimArm(leftArm, leftForeArm, Side(Hanging(0f), leftArm));
            var open = (Right - Forward * 0.4f + Vector3.up * 0.2f).normalized;
            var sweep = (Forward - Right * 0.3f + Vector3.up * 0.1f).normalized;
            var dir = k < 0f ? Side(Hanging(0f), rightArm)
                     : (k < 0.45f ? Vector3.Slerp(Hanging(0f), open, k / 0.45f) : Vector3.Slerp(open, sweep, (k - 0.45f) / 0.55f));
            AimArm(rightArm, rightForeArm, dir);
        }

        // Bras pendant le long du corps (swing = petit balancement d'avant en arrière).
        Vector3 Hanging(float swing) => Quaternion.AngleAxis(swing * 30f, Right) * -Vector3.up;

        // Écarte un peu une direction du corps, du côté de ce bras (les bras ne rentrent pas dans le ventre).
        Vector3 Side(Vector3 dir, Transform arm)
        {
            if (!arm) return dir;
            float side = Mathf.Sign(Vector3.Dot(arm.position - transform.position, Right));
            return (dir + Right * side * 0.35f).normalized;
        }

        // Tourne l'os pour que le segment bras → avant-bras pointe dans la direction voulue.
        static void AimArm(Transform bone, Transform child, Vector3 direction)
        {
            if (!bone || !child) return;
            var current = child.position - bone.position;
            if (current.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(current, direction) * bone.rotation;
        }
    }
}
