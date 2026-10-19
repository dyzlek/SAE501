using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    public enum BombKind { Glace, Colle, Clous }

    // Une bombe à lancer à la main sur les ballons (#134) : on la prend sur le râtelier (BombRack), on la lance
    // (vitesse de la main, ThrowVelocity, comme la fléchette), elle éclate au premier contact :
    //   Glace : gèle presque sur place les ballons autour (3 s) ;
    //   Colle : les ralentit longtemps (8 s), sur une zone plus large ;
    //   Clous : pose un piège sur le chemin (NailTrap) qui éclate les ballons qui passent dessus.
    // Les blindés ne se laissent ni geler ni coller (Balloon.Slow). Une bombe lancée ne se ramasse pas :
    // le râtelier en refait une après quelques secondes (rien à ramasser par terre, confort VR).
    [RequireComponent(typeof(Rigidbody))]
    public class Bomb : MonoBehaviour, IThrowable
    {
        const float Size = 0.14f;          // diamètre, en mètres : tient dans la main
        const float LostDelay = 5f;        // partie dans le vide : elle disparaît quand même

        public BombKind kind;
        public BombRack rack;              // le râtelier qui la remplacera

        Rigidbody body;
        XRGrabInteractable grab;
        ThrowVelocity handSpeed;
        bool flying, thrown;
        Vector3 throwVelocity;
        float lostAt = -1f;

        // Les réglages de chaque bombe : couleur, rayon de l'effet (m), ralentissement (× vitesse), durée (s)
        public static Color ColorOf(BombKind k) => k switch
        {
            BombKind.Glace => new Color(0.45f, 0.8f, 1f),
            BombKind.Colle => new Color(0.45f, 0.85f, 0.2f),
            _ => new Color(0.55f, 0.55f, 0.6f),
        };
        static float Radius(BombKind k) => k == BombKind.Colle ? 3f : 2.5f;
        static float SlowFactor(BombKind k) => k == BombKind.Glace ? 0.05f : 0.35f;
        static float SlowSeconds(BombKind k) => k == BombKind.Glace ? 3f : 8f;

        // Fabrique une bombe (boule colorée, mèche, petit nom au-dessus) posée en 'position'
        public static Bomb Create(BombKind kind, BombRack rack, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // la boule garde son collider (saisie, contact)
            go.name = "Bombe " + kind;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Size;
            go.AddComponent<ColorTint>().Set(ColorOf(kind));
            Visuals.Box("Mèche", go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.12f, 0.35f, 0.12f), new Color(0.25f, 0.18f, 0.1f));

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;            // elle attend sur le râtelier sans tomber
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var g = go.AddComponent<XRGrabInteractable>();
            g.throwOnDetach = false;          // c'est Bomb qui la lance (voir ThrowVelocity)
            g.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractableFarAttachMode.Near;
            go.AddComponent<GrabReach>();
            var bomb = go.AddComponent<Bomb>();
            bomb.kind = kind;
            bomb.rack = rack;
            return bomb;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            handSpeed = gameObject.AddComponent<ThrowVelocity>();
            gameObject.AddComponent<ThrowTrail>();
            PlayerRig.IgnoreCollisions(gameObject);
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
            PlayerRig.Buzz(args.interactorObject.transform, 0.3f);
        }

        void OnRelease(SelectExitEventArgs args) => Throw(handSpeed.Velocity);

        public void Grab()
        {
            handSpeed.Clear();
            flying = false;
            body.isKinematic = true;
        }

        public void Throw(Vector3 velocity)
        {
            throwVelocity = velocity;
            flying = true;
            if (!thrown && rack) rack.Taken(this);   // le râtelier lance son minuteur pour en refaire une
            thrown = true;
            lostAt = Time.time + LostDelay;
        }

        void FixedUpdate()
        {
            if (!flying || !body.isKinematic) return;
            body.isKinematic = false;   // XRI la remet cinématique au lâcher : on la rend à la physique
            body.useGravity = true;
            body.linearVelocity = throwVelocity;
        }

        void Update()
        {
            if (lostAt > 0f && Time.time > lostAt && !grab.isSelected) Destroy(gameObject);
        }

        // Premier contact après le lancer (sol, décor, ballon) : elle éclate
        void OnCollisionEnter(Collision collision)
        {
            if (flying && !collision.collider.GetComponentInParent<PlayerRig>()) Explode(transform.position);
        }

        // Elle traverse les ballons (leurs colliders sont des triggers) : un ballon touché la fait éclater aussi
        void OnTriggerEnter(Collider other)
        {
            if (flying && other.GetComponentInParent<Balloon>()) Explode(transform.position);
        }

        void Explode(Vector3 at)
        {
            flying = false;
            var color = ColorOf(kind);
            if (kind == BombKind.Clous)
            {
                NailTrap.PlaceNear(at);
                Shockwave.Spawn(at, 0.8f, color, false);
            }
            else
            {
                float radius = Radius(kind);
                foreach (var b in Balloon.All)
                    if ((b.transform.position - at).sqrMagnitude <= radius * radius) b.Slow(SlowFactor(kind), SlowSeconds(kind));
                Shockwave.Spawn(at, radius, color, false);
            }
            Sfx.Play(Sound.Pop, at, 1f, 0.6f);   // un « pop » grave : la bombe éclate
            Destroy(gameObject);
        }
    }
}
