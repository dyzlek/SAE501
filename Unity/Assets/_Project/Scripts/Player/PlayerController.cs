using UnityEngine;
using UnityEngine.InputSystem;

namespace SAE
{
    // Joueur clavier/souris pour le prototype (sans casque).
    // ZQSD/WASD : marcher · souris : regarder · clic gauche : interagir · clic droit : lâcher · Échap : libérer la souris.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 3.5f;
        public float mouseSensitivity = 0.1f;
        public float reach = 5f;

        // Ce que le joueur vise : lu par le plateau pour afficher l'aperçu du singe à poser.
        public static PlayerController Local { get; private set; }
        public IClickable Target { get; private set; }
        public Vector3 AimPoint { get; private set; }

        CharacterController controller;
        Transform cam;
        float pitch;
        float verticalSpeed;
        GameObject heldVisual;
        Monkey? heldShown;

        void Awake() => Local = this;

        // Téléportation (cube Jouer / Hub). Le CharacterController doit être coupé pendant le déplacement.
        public void TeleportTo(Transform spot)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spot.position, spot.rotation);
            controller.enabled = true;
            pitch = 0f;
        }

        void Start()
        {
            controller = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>().transform;
            LockCursor(true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.escapeKey.wasPressedThisFrame) LockCursor(false);
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked)
            {
                if (mouse.leftButton.wasPressedThisFrame) LockCursor(true);
                return;
            }

            // Regarder
            var look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            cam.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Marcher (les touches sont lues par position : ZQSD sur clavier AZERTY)
            var input = Vector2.zero;
            if (kb.wKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed) input.y -= 1;
            if (kb.dKey.isPressed) input.x += 1;
            if (kb.aKey.isPressed) input.x -= 1;
            var move = transform.TransformDirection(new Vector3(input.x, 0f, input.y).normalized) * speed;
            verticalSpeed = controller.isGrounded ? -1f : verticalSpeed - 9.81f * Time.deltaTime;
            move.y = verticalSpeed;
            controller.Move(move * Time.deltaTime);

            // Viser un objet
            Target = null;
            if (Physics.Raycast(cam.position, cam.forward, out var hit, reach))
            {
                Target = hit.collider.GetComponentInParent<IClickable>();
                AimPoint = hit.point;
            }

            if (mouse.leftButton.wasPressedThisFrame) Target?.OnClick(this, AimPoint);
            if (mouse.rightButton.wasPressedThisFrame) GameState.ReturnHeld();   // lâcher = ranger dans la bibliothèque

            UpdateHeldVisual();
        }

        // Le singe tenu s'affiche en bas à droite de la vue.
        void UpdateHeldVisual()
        {
            var held = GameState.Held;
            if (held.HasValue == heldShown.HasValue && (!held.HasValue || held.Value.Equals(heldShown.Value))) return;
            if (heldVisual) Destroy(heldVisual);
            heldShown = held;
            if (held.HasValue)
                heldVisual = Visuals.MonkeyPiece(held.Value, cam, new Vector3(0.3f, -0.25f, 0.6f), 0.15f);
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnGUI()
        {
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            GUI.Label(new Rect(center.x - 5, center.y - 10, 20, 20), "+");
            if (Target != null)
                GUI.Label(new Rect(center.x + 15, center.y - 10, 500, 25), Target.GetHint(AimPoint));

            // Pas d'interface collée à l'écran (en VR ça donne le vertige) : l'argent est sur la caisse du hub,
            // le singe tenu est visible dans la main. Seuls restent le viseur et l'aide, pour tester sans casque.
            GUI.Label(new Rect(10, Screen.height - 30, 900, 25),
                "ZQSD : marcher · Souris : regarder · Clic gauche : interagir · Clic droit : lâcher · Échap : souris");
        }
    }
}
