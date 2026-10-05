using UnityEngine;

namespace SAE
{
    // Couleur d'un objet sans créer de matériau (MaterialPropertyBlock). Marche aussi hors Play.
    [ExecuteAlways]
    public class ColorTint : MonoBehaviour
    {
        public Color color = Color.white;
        public bool rainbow;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock block;
        Renderer rend;

        public void Set(Color c, bool isRainbow = false) { color = c; rainbow = isRainbow; Apply(); }

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Update() { if (rainbow) Apply(); }

        void Apply()
        {
            if (!rend) rend = GetComponent<Renderer>();
            if (!rend) return;
            block ??= new MaterialPropertyBlock();
            var c = rainbow ? Color.HSVToRGB(Mathf.Repeat(Time.realtimeSinceStartup * 0.3f, 1f), 0.8f, 1f) : color;
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            rend.SetPropertyBlock(block);
        }
    }
}
