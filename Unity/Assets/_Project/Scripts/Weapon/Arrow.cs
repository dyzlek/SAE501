using UnityEngine;

namespace SAE
{
    // Une flèche (prefab Prefabs/Fleche.prefab : l'origine est l'encoche, la pointe vers +Z).
    // Encochée, elle suit la corde sans physique. Tirée, elle vole avec la gravité et s'oriente dans le sens
    // de sa vitesse (elle pique du nez en fin de course). Elle éclate les ballons qu'elle traverse,
    // se plante dans ce qu'elle touche d'autre, puis disparaît.
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : MonoBehaviour
    {
        public float damage = 2f;        // dégâts par ballon touché
        public int maxBalloons = 3;      // nombre de ballons qu'elle traverse avant de s'arrêter
        public float lifetime = 4f;      // secondes avant de disparaître une fois tirée (comme les balles du cours)
        public float stuckTime = 2f;     // secondes avant de disparaître une fois plantée

        Rigidbody body;
        bool flying;
        int balloonsHit;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // rapide : sans ça, elle traverserait les ballons
        }

        // Encochée : c'est l'arc qui la déplace.
        public void Hold()
        {
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
        }

        public void Launch(Vector3 velocity)
        {
            flying = true;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = velocity;
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
            if (balloon)
            {
                balloon.Hit(damage);
                if (++balloonsHit < maxBalloons) return;   // elle continue sa course
            }
            Stick();
        }

        // Plantée : elle s'arrête net là où elle a touché, puis disparaît. (Pas d'accroche par parent :
        // les cubes du décor sont étirés, la flèche le serait aussi.)
        void Stick()
        {
            flying = false;
            body.isKinematic = true;
            Destroy(gameObject, stuckTime);
        }
    }
}
