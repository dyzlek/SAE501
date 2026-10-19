using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SAE
{
    // Une flèche (prefab Prefabs/Fleche.prefab : l'origine est l'encoche, la pointe vers +Z).
    // Encochée, elle suit la corde sans physique. Tirée, elle vole avec la gravité et s'oriente dans le sens
    // de sa vitesse (elle pique du nez en fin de course), avec une traînée dorée qui montre sa vitesse.
    // Deux améliorations décident de ce qu'elle fait aux ballons :
    //   perforation (BowUpgrades.Pierce)        : les couches percées sur chaque ballon touché (ses « dégâts ») ;
    //   transperçante (BowUpgrades.PassThrough) : le nombre de ballons qu'elle traverse (tir collatéral).
    // Sans amélioration : 1 couche sur 1 ballon, puis elle disparaît. Si elle touche autre chose qu'un ballon, elle s'y plante.
    // Explosive (BowUpgrades.Explosive) : à chaque ballon touché, une onde perce aussi les ballons voisins.
    // Rebond (BowUpgrades.Bounces) : au lieu de disparaître après son dernier ballon, elle file tout droit vers le ballon
    // le plus proche, autant de fois que de rebonds. Givrante (BowUpgrades.Frost) : chaque ballon touché est ralenti.
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : MonoBehaviour
    {
        public float lifetime = 4f;      // secondes avant de disparaître une fois tirée (comme les balles du cours)
        public float stuckTime = 2f;     // secondes avant de disparaître une fois plantée

        Rigidbody body;
        TrailRenderer trail;
        bool flying;
        int balloonsLeft;
        int bouncesLeft;
        readonly HashSet<Balloon> touched = new HashSet<Balloon>();   // un ballon n'est touché qu'une fois par flèche

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // rapide : sans ça, elle traverserait les ballons
            trail = AddTrail();
        }

        // Encochée : c'est l'arc qui la déplace.
        public void Hold()
        {
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
            trail.emitting = false;
        }

        public void Launch(Vector3 velocity)
        {
            flying = true;
            balloonsLeft = BowUpgrades.PassThrough;   // lu au tir : une amélioration achetée compte dès la flèche suivante
            bouncesLeft = BowUpgrades.Bounces;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = velocity;
            trail.Clear();
            trail.emitting = true;
            Destroy(gameObject, lifetime);
        }

        void FixedUpdate()
        {
            // La flèche regarde toujours dans le sens où elle va : elle suit sa courbe
            if (flying && body.linearVelocity.sqrMagnitude > 0.5f)
                body.MoveRotation(Quaternion.LookRotation(body.linearVelocity));
        }

        void OnTriggerEnter(Collider other)
        {
            if (!flying || other.isTrigger || other.GetComponentInParent<PlayerRig>()) return;   // pas le joueur ni ses mains

            var balloon = other.GetComponentInParent<Balloon>();
            if (!balloon) { Stick(); return; }
            if (!touched.Add(balloon)) return;   // déjà percé (un ballon peut avoir plusieurs colliders)

            var center = balloon.transform.position;
            balloon.Pop(BowUpgrades.Pierce);
            QuincyPower.AddCharge();   // chaque ballon touché remplit la jauge du pouvoir de Quincy
            if (BowUpgrades.Frost) balloon.Slow(BowUpgrades.FrostFactor, BowUpgrades.FrostSeconds);
            if (BowUpgrades.Explosive) Explode(center, balloon);
            if (--balloonsLeft > 0) return;
            if (bouncesLeft > 0 && Bounce(center)) { bouncesLeft--; balloonsLeft = 1; return; }
            Destroy(gameObject);   // plus de ballon à traverser ni de rebond : la flèche est détruite
        }

        // Rebond : vers le ballon pas encore touché le plus proche, en ligne droite (sans gravité : elle ne rate pas sa cible)
        bool Bounce(Vector3 from)
        {
            Balloon next = null;
            float best = BowUpgrades.BounceRange * BowUpgrades.BounceRange;
            foreach (var b in Balloon.All)
            {
                if (touched.Contains(b)) continue;
                float d = (b.transform.position - from).sqrMagnitude;
                if (d < best) { best = d; next = b; }
            }
            if (!next) return false;
            float speed = Mathf.Max(body.linearVelocity.magnitude, 15f);
            body.useGravity = false;
            body.linearVelocity = (next.transform.position - transform.position).normalized * speed;
            Sfx.Play(Sound.Press, transform.position, 0.6f, 1.6f);   // un petit « tic » aigu : elle a rebondi
            return true;
        }

        // L'onde de l'explosion : les ballons les plus proches de celui touché, dans le rayon, perdent des couches.
        void Explode(Vector3 center, Balloon hit)
        {
            float radius = BowUpgrades.ExplosionRadius;
            var neighbours = Balloon.All
                .Where(b => b != hit && (b.transform.position - center).sqrMagnitude <= radius * radius)
                .OrderBy(b => (b.transform.position - center).sqrMagnitude)
                .Take(BowUpgrades.ExplosionBalloons - 1)   // -1 : le ballon touché compte dans le total
                .ToList();
            foreach (var b in neighbours) b.Pop(BowUpgrades.ExplosionLayers);
            Shockwave.Spawn(center, radius);
        }

        // Plantée : elle s'arrête net là où elle a touché, puis disparaît. (Pas d'accroche par parent :
        // les cubes du décor sont étirés, la flèche le serait aussi.)
        void Stick()
        {
            flying = false;
            body.isKinematic = true;
            trail.emitting = false;
            Destroy(gameObject, stuckTime);
        }

        // Effet de vitesse : une traînée dorée (pas blanche : elle se perdait dans le ciel), courte (0,12 s), qui s'affine et s'efface.
        TrailRenderer AddTrail()
        {
            var t = gameObject.AddComponent<TrailRenderer>();
            t.sharedMaterial = Visuals.LineMaterial;
            t.time = 0.12f;
            t.widthMultiplier = 0.035f;
            t.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            t.startColor = new Color(1f, 0.8f, 0.3f, 0.8f);
            t.endColor = new Color(1f, 0.5f, 0.1f, 0f);
            t.minVertexDistance = 0.1f;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            t.emitting = false;
            return t;
        }
    }
}
