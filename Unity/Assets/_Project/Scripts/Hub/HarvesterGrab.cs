using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Pour s'amuser : on peut prendre un singe récolteur dans sa main (il gigote) et le lancer un peu partout.
    // Il vole en tournoyant, atterrit, se remet debout, est sonné un instant, puis retourne ramasser des bananes.
    // Il se pose au sol, au point le plus proche de là où il est tombé où il peut marcher : le disque libre au centre
    // de la cabane (le même où l'on se téléporte en VR). Tombé sur un meuble, dehors ou dans le vide : il est ramené au bord
    // de ce disque, du côté où il est tombé. Il ne se fait jamais mal, promis.
    [RequireComponent(typeof(HarvesterMonkey))]
    public class HarvesterGrab : MonoBehaviour, IThrowable
    {
        const float SettleSpeed = 0.3f;     // en m/s : plus lent que ça, il a fini de rouler
        const float MinFlight = 0.3f;       // en secondes : il ne « se pose » pas dès le lâcher
        const float MaxFlight = 5f;         // en secondes : au-delà, on le remet à sa place

        HarvesterMonkey monkey;
        Rigidbody body;
        XRGrabInteractable grab;
        ThrowVelocity handSpeed;
        bool flying;
        Vector3 throwVelocity;
        float launchedAt;

        void Awake()
        {
            monkey = GetComponent<HarvesterMonkey>();
            var col = gameObject.AddComponent<CapsuleCollider>();   // avant le XR Grab : il récupère les colliders à sa création
            col.height = monkey.height;
            col.radius = monkey.height * 0.3f;
            col.center = Vector3.up * monkey.height / 2f;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;      // il marche tout seul : pas de physique hors des lancers
            body.useGravity = false;
            // Pas d'interpolation quand il marche : elle remettait le singe à sa position physique à chaque image,
            // et il n'avançait qu'à un tiers de sa vitesse. Elle ne sert que pendant le vol (voir FixedUpdate et Land).
            body.interpolation = RigidbodyInterpolation.None;
            grab = gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;   // c'est nous qui le lançons (ThrowVelocity : XRI perd l'élan d'un objet cinématique)
            grab.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near;   // pris de loin, il vient dans la main
            gameObject.AddComponent<GrabReach>();
            handSpeed = gameObject.AddComponent<ThrowVelocity>();
            gameObject.AddComponent<ThrowTrail>();   // la traînée dorée quand il vole
            PlayerRig.IgnoreCollisions(gameObject);   // on ne marche pas sur lui en le tenant
            if (monkey.Mirrored) SetGrabbable(false);   // coop : il imitait déjà le singe de l'hôte avant d'apparaître
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }

        void OnGrab(SelectEnterEventArgs args)
        {
            Grab();
            PlayerRig.Buzz(args.interactorObject.transform, 0.4f);
        }

        void OnRelease(SelectExitEventArgs args) => Throw(handSpeed.Velocity);

        // Pris dans la main (VR) ou au clic (mode PC) : il arrête de travailler et gigote
        public void Grab()
        {
            flying = false;
            handSpeed.Clear();
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;   // rattrapé en vol : de nouveau cinématique
            body.isKinematic = true;
            monkey.PickedUp();
        }

        // Coop, chez l'invité : le singe imite celui de l'hôte (HarvesterMonkey.Mirror), on ne peut plus l'attraper.
        // On coupe son collider : ni la main, ni le rayon, ni le clic du mode PC ne le trouvent.
        public void SetGrabbable(bool on)
        {
            if (!grab) return;   // singe encore jamais affiché : Awake s'en chargera (voir la fin d'Awake)
            grab.enabled = on;
            foreach (var c in GetComponents<Collider>()) c.enabled = on;
            if (on) return;
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
        }

        // Lâché avec cette vitesse : il vole (la physique prend le relais au prochain pas, voir FixedUpdate)
        public void Throw(Vector3 velocity)
        {
            throwVelocity = velocity;
            flying = true;
            launchedAt = Time.time;
        }

        void FixedUpdate()
        {
            if (!flying || !body.isKinematic) return;
            // Juste après le lâcher (XRI l'a remis cinématique) : la physique prend le relais, avec l'élan de la main et une vrille
            body.isKinematic = false;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;   // un vol fluide, même entre deux pas de physique
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // lancé fort, il ne traverse pas un sol fin
            body.linearVelocity = throwVelocity;
            body.angularVelocity = Random.onUnitSphere * 8f;
        }

        void Update()
        {
            if (!flying) return;
            float age = Time.time - launchedAt;
            bool lost = age > MaxFlight || transform.position.y < FallGuard.FallHeight;
            bool settled = age > MinFlight && !body.isKinematic && body.linearVelocity.sqrMagnitude < SettleSpeed * SettleSpeed;
            if (lost || settled) Land();
        }

        // Il se remet debout, au sol, sur la terrasse de la bananeraie ; puis HarvesterMonkey le renvoie au travail
        void Land()
        {
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;   // de nouveau à pied : c'est HarvesterMonkey qui le déplace
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;   // le continu ne sert qu'en vol (et pas en cinématique)
            var center = monkey.areaCenter;
            var flat = new Vector3(transform.position.x - center.x, 0f, transform.position.z - center.z);
            flat = Vector3.ClampMagnitude(flat, monkey.areaRadius);   // le point de la terrasse le plus proche, au sol
            var spot = new Vector3(center.x + flat.x, monkey.home.y, center.z + flat.z);
            var look = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            transform.SetPositionAndRotation(spot, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward));
            monkey.PutDown();
        }
    }
}
