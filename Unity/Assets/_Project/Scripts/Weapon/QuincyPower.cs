using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Le pouvoir de Quincy (#135) : une PLUIE DE FLÈCHES. Ajouté à l'arc par Bow.Awake.
    //   Se charger : chaque ballon touché par une flèche remplit la jauge (ChargeNeeded ballons).
    //   Prêt : l'arc s'entoure d'étincelles dorées, un « ding » et une vibration le disent.
    //   Lancer (VR) : LEVER L'ARC vers le ciel, au-dessus de la tête, et le tenir HoldSeconds. Le geste fait le jeu :
    //   impossible à la souris (test de l'écran). En mode PC, touche R pour tester.
    // Les flèches tombent du ciel sur le ballon le plus avancé du chemin (le plus dangereux), sur un disque de RainRadius.
    // Ce sont de vraies flèches (Arrow) : elles profitent des améliorations de l'arc (perforation, explosion…).
    public class QuincyPower : MonoBehaviour
    {
        const int ChargeNeeded = 40;
        const float HoldSeconds = 0.6f;
        const float RainArrows = 30f;
        const float RainSeconds = 2f;
        const float RainRadius = 3f;       // en mètres
        const float RainHeight = 14f;      // les flèches partent de 14 m au-dessus de la cible
        const float RainSpeed = 25f;

        static int charge;
        static bool raining;               // les flèches de la pluie ne rechargent pas la jauge
        public static bool Ready => charge >= ChargeNeeded;

        Bow bow;
        float raisedFor;
        ParticleSystem sparkle;

        // Sans rechargement du domaine (Enter Play Mode rapide), la jauge repart de zéro
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCharge() { charge = 0; raining = false; }
        public static void Reset() => ResetCharge();   // nouvelle partie

        // Une flèche vient de toucher un ballon
        public static void AddCharge()
        {
            if (Ready || raining) return;
            charge++;
            if (Ready) Sfx.Play(Sound.Ding, null, 0.8f, 1.5f);
        }

        void Awake()
        {
            bow = GetComponent<Bow>();
            sparkle = BuildSparkle();
        }

        // L'arc est rangé (retour au hub) pendant la pluie : la coroutine s'arrête, la jauge doit pouvoir se recharger
        void OnDisable() => raining = false;

        void Update()
        {
            var emission = sparkle.emission;
            emission.enabled = Ready && !raining;
            if (!Ready || raining) { raisedFor = 0f; return; }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) { Fire(); return; }   // mode PC

            // L'arc pointe vers le ciel (moins de ~30° de la verticale) et il est au-dessus de la tête, sans flèche tendue
            var player = PlayerRig.Local;
            bool raised = player && !bow.HasArrow && bow.ShootDirection.y > 0.85f && transform.position.y > player.HeadPosition.y;
            raisedFor = raised ? raisedFor + Time.deltaTime : 0f;
            if (raised) PlayerRig.Buzz(transform, 0.15f + 0.5f * raisedFor / HoldSeconds, 0.02f);   // ça monte dans la main
            if (raisedFor >= HoldSeconds) Fire();
        }

        void Fire()
        {
            raisedFor = 0f;
            Balloon target = null;
            foreach (var b in Balloon.All) if (!target || b.Progress > target.Progress) target = b;
            if (!target) return;   // aucune vague en cours : la jauge est gardée
            charge = 0;
            PlayerRig.Buzz(transform, 1f, 0.3f);
            Sfx.Play(Sound.Victory, null, 0.6f, 1.3f);
            StartCoroutine(Rain(target));
        }

        // Les flèches tombent une par une sur la cible ; si elle éclate, la pluie continue là où elle était
        IEnumerator Rain(Balloon target)
        {
            raining = true;
            var center = target.transform.position;
            for (int i = 0; i < RainArrows; i++)
            {
                if (target) center = target.transform.position;
                var disc = Random.insideUnitCircle * RainRadius;
                var spot = center + new Vector3(disc.x, 0f, disc.y);
                var arrow = Instantiate(bow.arrowPrefab, spot + Vector3.up * RainHeight, Quaternion.LookRotation(Vector3.down));
                arrow.Launch(Vector3.down * RainSpeed);
                yield return new WaitForSeconds(RainSeconds / RainArrows);
            }
            raining = false;
        }

        // Des étincelles dorées autour de l'arc quand le pouvoir est prêt (l'objet « brille » : on comprend qu'il y a quelque chose à faire)
        ParticleSystem BuildSparkle()
        {
            var ps = new GameObject("Pouvoir prêt").AddComponent<ParticleSystem>();
            ps.transform.SetParent(transform, false);
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 0.2f;
            main.startSize = 0.02f;
            main.startColor = new Color(1f, 0.85f, 0.3f);
            main.maxParticles = 40;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            var emission = ps.emission;
            emission.rateOverTime = 40f;
            emission.enabled = false;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Visuals.LineMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }
}
