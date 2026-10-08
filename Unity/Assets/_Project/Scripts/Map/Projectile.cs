using System;
using UnityEngine;

namespace SAE
{
    // Ce que lance un singe (fléchette, boomerang, bombe, balle, punaise, boule de neige, goutte de colle) :
    // le vrai modèle 3D vole de la main du singe jusqu'au ballon visé, et c'est en arrivant qu'il le touche (onHit).
    // Il suit le ballon pendant le vol ; si le ballon a déjà éclaté, il finit sa course et disparaît sans rien toucher.
    // Chaque type a son vol : droit et rapide (fléchette, balle), en courbe sur le côté (boomerang), en cloche (bombe).
    public class Projectile : MonoBehaviour
    {
        // Réglages de vol par type, dans l'ordre de MonkeyType.
        //                                         Classique  Boomerang  Canon  Sniper  Punaise  Glace  Colle
        static readonly float[] Sizes =          { 0.35f,     0.4f,      0.35f, 0.15f,  0.18f,   0.3f,  0.25f };   // en mètres
        static readonly float[] Speeds =         { 18f,       10f,       9f,    60f,    16f,     12f,   12f };     // en m/s
        static readonly float[] Arcs =           { 0f,        0f,        1.2f,  0f,     0f,      0.5f,  0.5f };    // hauteur de la cloche, en mètres
        static readonly float[] Curves =         { 0f,        1.5f,      0f,    0f,     0f,      0f,    0f };      // écart sur le côté (boomerang)
        static readonly float[] Spins =          { 0f,        1080f,     360f,  0f,     0f,      360f,  0f };      // tours sur lui-même, en degrés/s

        MonkeyType type;
        Vector3 from, lastTarget;
        Balloon target;
        Action onHit;
        float duration, age;

        // Lance le projectile du type de singe, de from vers le ballon. Renvoie false s'il n'y a pas de modèle :
        // l'appelant touche alors le ballon tout de suite.
        public static bool Launch(MonkeyType type, Vector3 from, Balloon target, Action onHit)
        {
            var asset = MonkeyVisuals.Projectile(type);
            if (!asset || !target) return false;

            var go = new GameObject($"Projectile {type}");
            go.transform.position = from;
            Visuals.FitModel(asset, 0f, go.transform, Sizes[(int)type], out _);
            var p = go.AddComponent<Projectile>();
            p.type = type;
            p.from = from;
            p.target = target;
            p.lastTarget = target.transform.position;
            p.onHit = onHit;
            p.duration = Mathf.Max(0.05f, Vector3.Distance(from, p.lastTarget) / Speeds[(int)type]);
            return true;
        }

        void Update()
        {
            if (target && target.isActiveAndEnabled) lastTarget = target.transform.position;   // il suit le ballon
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            int i = (int)type;

            var straight = Vector3.Lerp(from, lastTarget, t);
            var side = Vector3.Cross(Vector3.up, (lastTarget - from).normalized);
            float bump = Mathf.Sin(t * Mathf.PI);   // 0 au départ et à l'arrivée, 1 au milieu
            var next = straight + Vector3.up * Arcs[i] * bump + side * Curves[i] * bump;

            // Il regarde vers où il va (fléchette, balle, punaise) ou tourne sur lui-même (boomerang, bombe, boule)
            if (Spins[i] > 0f) transform.Rotate(Vector3.up, Spins[i] * Time.deltaTime, Space.World);
            else if (next != transform.position) transform.rotation = Quaternion.LookRotation(next - transform.position);
            transform.position = next;

            if (t < 1f) return;
            if (target && target.isActiveAndEnabled) onHit?.Invoke();
            Destroy(gameObject);
        }
    }
}
