using UnityEngine;

namespace SAE
{
    // Un ballon qui monte dans le ciel du menu, puis repart d'en bas ; visé au rayon, il éclate (et repart d'en bas)
    public class MenuBalloon : MonoBehaviour, IPressable
    {
        const float Top = 9f;
        float speed, phase;
        Vector3 start;

        public static void Create(Transform parent, Color color, int index)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // garde son collider : on peut le viser
            go.name = "Ballon";
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            go.AddComponent<ColorTint>().Set(color);
            Visuals.Box("Ficelle", go.transform, new Vector3(0f, -0.9f, 0f), new Vector3(0.02f, 0.8f, 0.02f), Color.white);
            var balloon = go.AddComponent<MenuBalloon>();
            var random = new System.Random(index * 7919);
            balloon.start = new Vector3((float)random.NextDouble() * 12f - 6f, 0f, 4f + (float)random.NextDouble() * 6f);
            balloon.speed = 0.4f + (float)random.NextDouble() * 0.5f;
            balloon.phase = (float)random.NextDouble() * Top;   // ils ne partent pas tous d'en bas
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            go.AddComponent<RayPress>();
        }

        void Update()
        {
            float y = Mathf.Repeat(Time.unscaledTime * speed + phase, Top);
            float sway = Mathf.Sin(Time.unscaledTime * 1.3f + phase) * 0.3f;
            transform.localPosition = start + new Vector3(sway, y, 0f);
        }

        public void Press()
        {
            Sounds.Pop(transform.position);
            phase -= transform.localPosition.y - start.y;   // il repart d'en bas
        }
    }
}
