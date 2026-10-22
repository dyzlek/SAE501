using UnityEngine;

namespace SAE
{
    // Un autre joueur du coop, vu chez nous : une tête avec une visière, un buste, deux mains (au casque seulement :
    // au PC, les « mains » sont la caméra) et son nom au-dessus. Une couleur par joueur.
    // On ne le voit que s'il est dans le même niveau que nous (hub, carte ou menu).
    // Ses positions arrivent 20 fois par seconde (Coop) : on glisse vers elles pour que ça reste fluide.
    public class CoopAvatar : MonoBehaviour
    {
        static readonly Color[] colors =
        {
            new Color(1f, 0.8f, 0.3f), new Color(0.3f, 0.6f, 1f), new Color(0.4f, 0.85f, 0.4f), new Color(1f, 0.45f, 0.75f),
        };
        static readonly Color Visor = new Color(0.08f, 0.08f, 0.1f);

        const float Smooth = 18f;            // plus c'est grand, plus il colle à la dernière position reçue
        const float HeadSize = 0.22f;        // en mètres
        const float HandSize = 0.09f;
        const float NeckToChest = 0.45f;     // le buste est sous la tête

        Transform visuals, head, body, left, right;
        Level level;
        bool vr;
        Vector3 headPos, leftPos, rightPos;
        Quaternion headRot = Quaternion.identity, leftRot = Quaternion.identity, rightRot = Quaternion.identity;
        bool placed;

        public static CoopAvatar Create(byte id, Transform parent)
        {
            var go = new GameObject($"Joueur {id + 1}");
            go.transform.SetParent(parent, false);
            var avatar = go.AddComponent<CoopAvatar>();
            avatar.Build(colors[id % colors.Length], $"JOUEUR {id + 1}");
            return avatar;
        }

        void Build(Color color, string name)
        {
            visuals = new GameObject("Corps").transform;
            visuals.SetParent(transform, false);
            head = Part(PrimitiveType.Sphere, Vector3.one * HeadSize, color);
            Visuals.Box("Visière", head, new Vector3(0f, 0.08f, 0.38f), new Vector3(0.8f, 0.3f, 0.3f), Visor);
            body = Part(PrimitiveType.Capsule, new Vector3(0.3f, 0.3f, 0.2f), color * 0.8f);
            left = Part(PrimitiveType.Sphere, Vector3.one * HandSize, color);
            right = Part(PrimitiveType.Sphere, Vector3.one * HandSize, color);
            Visuals.Label(head, name, new Vector3(0f, 1.3f, 0f), 0.5f, color, title: true);   // dans le repère de la tête (× 0,22)
        }

        Transform Part(PrimitiveType shape, Vector3 size, Color color)
        {
            var go = Visuals.Primitive(shape);
            Visuals.Kill(go.GetComponent<Collider>());   // on ne se cogne pas aux autres joueurs, les flèches les traversent
            go.transform.SetParent(visuals, false);
            go.transform.localScale = size;
            go.AddComponent<ColorTint>().Set(color);
            return go.transform;
        }

        public void SetPose(Level lvl, bool isVR, Vector3 hp, Quaternion hr, Vector3 lp, Quaternion lr, Vector3 rp, Quaternion rr)
        {
            level = lvl;
            vr = isVR;
            headPos = hp; headRot = hr;
            leftPos = lp; leftRot = lr;
            rightPos = rp; rightRot = rr;
        }

        void Update()
        {
            bool shown = level == Levels.Current && headPos != Vector3.zero;
            visuals.gameObject.SetActive(shown);
            if (!shown) { placed = false; return; }

            // La première fois (ou en revenant dans le niveau) : directement à sa place, sans glisser depuis l'origine
            float t = placed ? 1f - Mathf.Exp(-Smooth * Time.unscaledDeltaTime) : 1f;
            placed = true;
            Follow(head, headPos, headRot, t);
            left.gameObject.SetActive(vr);
            right.gameObject.SetActive(vr);
            if (vr)
            {
                Follow(left, leftPos, leftRot, t);
                Follow(right, rightPos, rightRot, t);
            }

            // Le buste reste droit : il ne suit que la direction du regard, à plat
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.001f) body.rotation = Quaternion.LookRotation(forward);
            body.position = head.position + Vector3.down * NeckToChest;
        }

        static void Follow(Transform part, Vector3 pos, Quaternion rot, float t)
        {
            part.SetPositionAndRotation(Vector3.Lerp(part.position, pos, t), Quaternion.Slerp(part.rotation, rot, t));
        }
    }
}
