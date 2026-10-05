using UnityEngine;
using UnityEngine.InputSystem;

namespace Sae501.Coffres
{
    // Déplacement à l'écran (sans casque) : Z avancer, S reculer, Q gauche, D droite, souris pour regarder.
    // Échap libère la souris. Ce script sera remplacé par le rig XR (téléportation + snap turn) en VR.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float moveSpeed = 3f;
        public float mouseSensitivity = 0.1f; // degrés par pixel
        public float gravity = -9.81f;

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        bool menuOpen;

        // true = souris capturée, on regarde et on marche.
        public bool ControlEnabled { get; private set; }

        void Awake() => controller = GetComponent<CharacterController>();

        void Start() => SetControl(true);

        // Quand un menu est ouvert, la souris est libérée pour cliquer dessus.
        public void SetMenuOpen(bool open)
        {
            menuOpen = open;
            SetControl(!open);
        }

        void SetControl(bool on)
        {
            ControlEnabled = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (ControlEnabled && keyboard.escapeKey.wasPressedThisFrame) SetControl(false);
            else if (!ControlEnabled && !menuOpen && mouse.leftButton.wasPressedThisFrame) SetControl(true);

            if (!ControlEnabled) return;

            // Regard : la souris tourne le corps (gauche/droite) et la caméra (haut/bas).
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Déplacement (touches Z Q S D).
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f);
            float z = (keyboard.zKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f) * moveSpeed;

            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }

        // Petit point de visée au centre de l'écran.
        void OnGUI()
        {
            if (!ControlEnabled) return;
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 2f, Screen.height * 0.5f - 2f, 4f, 4f), Texture2D.whiteTexture);
        }
    }
}
