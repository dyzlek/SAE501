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
        const float WalkRadius = HubLayout.Ring - 0.9f;   // 1,9 m : le disque libre au centre (zone de téléportation du hub)

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
            body.interpolation = RigidbodyInterpolation.Interpolate;
            grab = gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;   // c'est nous qui le lançons (ThrowVelocity : XRI perd l'élan d'un objet cinématique)
            grab.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near;   // pris de loin, il vient dans la main
            gameObject.AddComponent<GrabReach>();
            handSpeed = gameObject.AddComponent<ThrowVelocity>();
            PlayerRig.IgnoreCollisions(gameObject);   // on ne marche pas sur lui en le tenant
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
            body.isKinematic = true;
            monkey.PickedUp();
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

        // Il se remet debout, au sol, dans la cabane ; puis HarvesterMonkey le renvoie au travail
        void Land()
        {
            flying = false;
            body.isKinematic = true;
            body.useGravity = false;
            var flat = new Vector3(transform.position.x, 0f, transform.position.z);
            flat = Vector3.ClampMagnitude(flat, WalkRadius);   // le point du disque libre le plus proche, au sol
            var spot = new Vector3(flat.x, monkey.home.y, flat.z);
            var look = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            transform.SetPositionAndRotation(spot, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : Vector3.forward));
            monkey.PutDown();
        }
    }
}
