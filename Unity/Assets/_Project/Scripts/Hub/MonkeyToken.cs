using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Un singe qu'on prend à la main sur l'étagère (XR Grab Interactable).
    // Chaque case de la bibliothèque qui possède au moins un singe en pose un sur elle (voir LibrarySlot).
    //   - le saisir le sort de l'inventaire (GameState.Held) ;
    //   - le lâcher au-dessus du plateau ou de la carte le pose, ou le fusionne s'il tombe sur un singe identique ;
    //   - le lâcher ailleurs le range dans la bibliothèque.
    // Pendant qu'on le tient, le plateau montre l'aperçu vert/rouge et le halo de fusion juste en dessous.
    public class MonkeyToken : MonoBehaviour
    {
        public const float Size = 0.1f;      // en mètres : tient dans la main
        const float DropReach = 1.5f;        // on cherche le plateau ou la carte jusqu'à 1,5 m sous la main

        // Le singe tenu en main (un seul à la fois, comme GameState.Held).
        public static MonkeyToken Held { get; private set; }

        Monkey monkey;
        LibrarySlot slot;
        XRGrabInteractable grab;
        static readonly RaycastHit[] hits = new RaycastHit[8];

        // Construit le singe à saisir : le cube du singe, un collider, un Rigidbody (exigé par XR Grab) et le XR Grab.
        public static MonkeyToken Create(LibrarySlot slot, Monkey monkey, Vector3 localPosition)
        {
            var piece = Visuals.MonkeyPiece(monkey, slot.transform, localPosition, Size);
            piece.name = $"Singe à saisir {monkey}";
            // Visuals retire les colliders du cube en fin d'image (Destroy) : on le fait tout de suite,
            // sinon le XR Grab garderait un collider détruit.
            foreach (var c in piece.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
            piece.AddComponent<BoxCollider>().size = Vector3.one * Size;   // avant le XR Grab : il récupère les colliders à sa création

            var rb = piece.AddComponent<Rigidbody>();
            rb.isKinematic = true;     // il attend sur l'étagère sans tomber
            rb.useGravity = false;

            var grab = piece.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;   // il reste dans la main là où on l'a attrapé

            var token = piece.AddComponent<MonkeyToken>();
            token.monkey = monkey;
            token.slot = slot;
            token.grab = grab;
            token.Listen(true);
            return token;
        }

        void Listen(bool on)
        {
            if (!grab) return;
            if (on)
            {
                grab.selectEntered.AddListener(OnGrab);
                grab.selectExited.AddListener(OnRelease);
            }
            else
            {
                grab.selectEntered.RemoveListener(OnGrab);
                grab.selectExited.RemoveListener(OnRelease);
            }
        }

        void OnEnable() => Listen(true);
        void OnDisable() => Listen(false);

        // Une seule main peut tenir un singe : les autres singes ne sont pas saisissables pendant ce temps.
        void Update() => grab.enabled = Held == null || Held == this;

        void OnGrab(SelectEnterEventArgs args)
        {
            if (Held != null) return;
            Held = this;
            slot.Detach(this);                      // la case en pose un autre si on en a encore
            GameState.TakeFromInventory(monkey);
            PlayerRig.Buzz(args.interactorObject.transform, 0.3f);
        }

        void OnRelease(SelectExitEventArgs args)
        {
            if (Held != this) return;
            Held = null;

            bool placed = TryGetSurfacePoint(out var surface, out var point) && surface.Drop(point);
            if (!placed) GameState.ReturnHeld();    // lâché dans le vide : il retourne dans la bibliothèque
            PlayerRig.Buzz(args.interactorObject.transform, placed ? 0.7f : 0.2f);
            Destroy(gameObject);
        }

        // Le plateau (ou la carte) juste sous le singe tenu, et le point visé dessus.
        public bool TryGetSurfacePoint(out PlacementSurface surface, out Vector3 point)
        {
            surface = null;
            point = default;
            // DefaultRaycastLayers ignore le joueur (couche Ignore Raycast, comme dans le cours)
            int count = Physics.RaycastNonAlloc(transform.position, Vector3.down, hits, DropReach,
                                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform) || hits[i].distance >= best) continue;   // pas lui-même
                best = hits[i].distance;
                surface = hits[i].collider.GetComponentInParent<PlacementSurface>();
                point = hits[i].point;
            }
            return surface != null;
        }
    }
}
