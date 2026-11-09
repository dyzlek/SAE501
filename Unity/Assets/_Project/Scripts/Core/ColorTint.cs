using UnityEngine;

namespace SAE
{
    // Couleur d'un objet. Hors Play : MaterialPropertyBlock (rien n'est enregistré dans la scène).
    // En jeu : un matériau partagé par couleur (Shared). Un MaterialPropertyBlock empêche le SRP Batcher de regrouper
    // les objets, et chaque cube teinté coûtait un changement de matériau (SetPass) : tous les cubes d'une même couleur
    // partagent maintenant le même matériau. Le mode arc-en-ciel change à chaque image : il garde le MaterialPropertyBlock.
    [ExecuteAlways]
    public class ColorTint : MonoBehaviour
    {
        public Color color = Color.white;
        public bool rainbow;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock block;
        Renderer rend;
        Material[] originals;   // les matériaux d'origine, d'où partent les copies teintées

        static readonly System.Collections.Generic.Dictionary<(Material, Color), Material> shared =
            new System.Collections.Generic.Dictionary<(Material, Color), Material>();

        // Le projet ne recharge pas le code au Play (Enter Play Mode Options) : sans ça, le cache garderait
        // les matériaux détruits à la fin du Play précédent, et tout s'afficherait en magenta.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => shared.Clear();

        // Le matériau source, teinté de cette couleur : créé une seule fois, puis partagé par tous les objets de cette couleur
        public static Material Shared(Material source, Color c)
        {
            if (!source) return null;
            if (!shared.TryGetValue((source, c), out var m) || !m)
            {
                m = new Material(source) { name = $"{source.name} {ColorUtility.ToHtmlStringRGB(c)}", enableInstancing = true };
                if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
                if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
                shared[(source, c)] = m;
            }
            return m;
        }

        public void Set(Color c, bool isRainbow = false) { color = c; rainbow = isRainbow; Apply(); }

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Update() { if (rainbow) Apply(); }

        void Apply()
        {
            if (!rend) rend = GetComponent<Renderer>();
            if (!rend) return;
            if (Application.isPlaying && !rainbow)
            {
                originals ??= rend.sharedMaterials;
                var mats = new Material[originals.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = Shared(originals[i], color);
                rend.SetPropertyBlock(null);   // plus de bloc : le SRP Batcher peut regrouper
                rend.sharedMaterials = mats;
                return;
            }
            block ??= new MaterialPropertyBlock();
            var c = rainbow ? Color.HSVToRGB(Mathf.Repeat(Time.realtimeSinceStartup * 0.3f, 1f), 0.8f, 1f) : color;
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            rend.SetPropertyBlock(block);
        }
    }
}
