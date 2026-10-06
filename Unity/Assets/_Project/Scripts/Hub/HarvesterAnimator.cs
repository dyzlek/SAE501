using UnityEngine;

namespace SAE
{
    // Les animations du singe récolteur, faites en code : le modèle du singe classique a un squelette mais aucune animation.
    // Chaque image, on remet les os dans leur pose de départ, puis on oriente les bras et les jambes vers une direction :
    //  - marche : les jambes se balancent d'avant en arrière, les bras à l'opposé, le corps rebondit, la queue ondule ;
    //  - porter : les deux bras levés au-dessus de la tête, la banane entre les mains ;
    //  - lancer : les bras partent en arrière puis vers l'avant.
    // On vise une direction (« ce bras pointe vers le haut ») plutôt qu'un angle autour d'un axe :
    // ça marche quelle que soit l'orientation des os dans le fichier FBX.
    [RequireComponent(typeof(HarvesterMonkey))]
    public class HarvesterAnimator : MonoBehaviour
    {
        public Transform model;                 // le modèle 3D (enfant), qu'on fait rebondir
        public float stepsPerMeter = 3.5f;      // pas par mètre parcouru
        public float legSwing = 35f;            // en degrés
        public float armSwing = 30f;            // en degrés
        public float bounce = 0.03f;            // en mètres

        HarvesterMonkey monkey;
        Transform leftArm, leftForeArm, rightArm, rightForeArm, leftHand, rightHand;
        Transform leftUpLeg, leftLeg, rightUpLeg, rightLeg, tail;
        Transform[] bones;
        Quaternion[] rest;
        Vector3 modelRest, lastPos;
        float phase, walkBlend, carryBlend;

        void Start()
        {
            monkey = GetComponent<HarvesterMonkey>();
            leftArm = Bone("LeftArm"); leftForeArm = Bone("LeftForeArm"); leftHand = Bone("LeftHand");
            rightArm = Bone("RightArm"); rightForeArm = Bone("RightForeArm"); rightHand = Bone("RightHand");
            leftUpLeg = Bone("LeftUpLeg"); leftLeg = Bone("LeftLeg");
            rightUpLeg = Bone("RightUpLeg"); rightLeg = Bone("RightLeg");
            tail = Bone("Tail1");

            bones = new[] { leftArm, rightArm, leftUpLeg, rightUpLeg, tail };
            rest = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) if (bones[i]) rest[i] = bones[i].localRotation;
            modelRest = model.localPosition;
            lastPos = transform.position;

            // Un Animator sans contrôleur ne sert à rien ici, et pourrait remettre la pose de départ
            var animator = model.GetComponentInChildren<Animator>();
            if (animator) animator.enabled = false;
        }

        Transform Bone(string boneName)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(boneName)) return t;
            Debug.LogWarning($"Singe récolteur : os « {boneName} » introuvable, pas d'animation pour lui.");
            return null;
        }

        void LateUpdate()
        {
            // Distance parcourue depuis l'image d'avant : c'est elle qui fait avancer les pas
            var moved = transform.position - lastPos;
            moved.y = 0f;
            lastPos = transform.position;
            phase += moved.magnitude * stepsPerMeter * Mathf.PI;

            walkBlend = Mathf.MoveTowards(walkBlend, monkey.Walking ? 1f : 0f, Time.deltaTime * 5f);
            carryBlend = Mathf.MoveTowards(carryBlend, monkey.Carried ? 1f : 0f, Time.deltaTime * 6f);

            for (int i = 0; i < bones.Length; i++) if (bones[i]) bones[i].localRotation = rest[i];

            var up = transform.up;
            var forward = transform.forward;
            var right = transform.right;
            float swing = Mathf.Sin(phase) * walkBlend;

            // Jambes : vers le bas, balancées d'avant en arrière, en alternance
            Aim(leftUpLeg, leftLeg, Quaternion.AngleAxis(swing * legSwing, right) * -up);
            Aim(rightUpLeg, rightLeg, Quaternion.AngleAxis(-swing * legSwing, right) * -up);

            AimArm(leftArm, leftForeArm, -swing, up, forward, right);
            AimArm(rightArm, rightForeArm, swing, up, forward, right);

            // Le corps rebondit à chaque pas ; la queue ondule tout le temps
            model.localPosition = modelRest + Vector3.up * (Mathf.Abs(swing) * bounce);
            if (tail) tail.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 3f) * 20f, up) * tail.rotation;

            // La banane portée suit les mains
            if (monkey.Carried)
            {
                var hands = leftHand && rightHand ? (leftHand.position + rightHand.position) / 2f
                                                  : transform.position + up * monkey.Height * 1.1f;
                monkey.Carried.transform.position = hands + up * 0.04f;
            }
        }

        void AimArm(Transform arm, Transform foreArm, float swing, Vector3 up, Vector3 forward, Vector3 right)
        {
            if (!arm) return;
            // De quel côté est ce bras : ses bras pendants s'écartent un peu du corps
            float side = Mathf.Sign(Vector3.Dot(arm.position - transform.position, right));

            var hanging = Quaternion.AngleAxis(swing * armSwing, right) * (-up + right * side * 0.35f).normalized;
            var raised = (up + right * side * 0.2f + forward * 0.15f).normalized;
            var dir = Vector3.Slerp(hanging, raised, carryBlend);

            // Lancer : les bras partent en arrière (1re moitié) puis passent devant (2e moitié)
            float t = monkey.ThrowPhase;
            if (t > 0f)
            {
                var back = (up - forward * 0.7f).normalized;
                var front = (forward + up * 0.3f).normalized;
                dir = t < 0.5f ? Vector3.Slerp(raised, back, t * 2f) : Vector3.Slerp(back, front, (t - 0.5f) * 2f);
            }
            Aim(arm, foreArm, dir);
        }

        // Tourne l'os pour que le segment os → os enfant pointe dans la direction voulue
        static void Aim(Transform bone, Transform child, Vector3 direction)
        {
            if (!bone || !child) return;
            var current = child.position - bone.position;
            if (current.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(current, direction) * bone.rotation;
        }
    }
}
