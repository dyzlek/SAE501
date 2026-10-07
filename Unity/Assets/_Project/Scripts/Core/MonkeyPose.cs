using UnityEngine;

namespace SAE
{
    // Les modèles des singes arrivent en « T-pose » (bras tendus à l'horizontale, la pose de modélisation) et n'ont
    // aucune animation. On leur donne une pose de repos, une seule fois à leur apparition : bras le long du corps,
    // un peu écartés, avant-bras pliés vers l'avant (ils tiennent leur arme devant eux).
    // Même technique que le singe récolteur (HarvesterAnimator) : on vise une DIRECTION pour chaque os,
    // ce qui marche quelle que soit l'orientation des os dans le fichier.
    public static class MonkeyPose
    {
        // model : le modèle 3D du singe ; facing : la direction où il regarde (en monde)
        public static void Relax(Transform model, Vector3 facing)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator) animator.enabled = false;   // sans contrôleur, il remettrait la pose de départ

            var up = model.up;
            var right = Vector3.Cross(up, facing).normalized;
            foreach (var side in new[] { "Left", "Right" })
            {
                var arm = Bone(model, side + "Arm");
                var foreArm = Bone(model, side + "ForeArm");
                var hand = Bone(model, side + "Hand");
                if (!arm || !foreArm) continue;   // modèle sans squelette (canon, tireur) : rien à faire
                float s = Mathf.Sign(Vector3.Dot(arm.position - model.position, right));
                Aim(arm, foreArm, (-up + right * s * 0.35f + facing * 0.1f).normalized);   // le bras pend, un peu écarté
                if (hand) Aim(foreArm, hand, (-up * 0.4f + facing).normalized);           // l'avant-bras vers l'avant
            }
        }

        // L'os dont le nom finit par 'name' (les noms peuvent avoir un préfixe, ex. « mixamorig:LeftArm »).
        // « LeftArm » ne confond pas « LeftForeArm », qui finit par « ForeArm ».
        static Transform Bone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith(name)) return t;
            return null;
        }

        // Tourne l'os pour que le segment os → os enfant pointe dans la direction voulue
        static void Aim(Transform bone, Transform child, Vector3 direction)
        {
            var current = child.position - bone.position;
            if (current.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(current, direction) * bone.rotation;
        }
    }
}
