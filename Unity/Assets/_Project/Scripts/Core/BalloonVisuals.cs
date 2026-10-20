using UnityEngine;

namespace SAE
{
    // Les modèles 3D des ballons (un par sorte) et leurs textures, rangés dans un seul asset :
    // Assets/_Project/Resources/BalloonVisuals.asset, rempli par le menu SAE → Brancher les modèles des ballons.
    // Comme MonkeyVisuals, il est dans un dossier Resources pour que le code le trouve aussi en jeu.
    // Les textures sont posées par le code : les FBX pointent vers des fichiers d'un autre ordinateur,
    // donc Unity les importe en blanc.
    [CreateAssetMenu(menuName = "SAE/Modèles des ballons")]
    public class BalloonVisuals : ScriptableObject
    {
        public GameObject[] models = new GameObject[6];   // dans l'ordre de BalloonKind
        public Texture2D[] textures = new Texture2D[6];   // idem

        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static BalloonVisuals instance;
        static MaterialPropertyBlock block;

        static BalloonVisuals Instance
        {
            get
            {
                if (!instance) instance = Resources.Load<BalloonVisuals>("BalloonVisuals");
                return instance;
            }
        }

        // Le modèle d'une sorte de ballon, ou null s'il n'y en a pas (on garde alors la sphère).
        public static GameObject Model(BalloonKind kind)
        {
            var v = Instance;
            if (!v || (int)kind >= v.models.Length) return null;
            return v.models[(int)kind];
        }

        // Pose la texture de la sorte sur tous les morceaux d'un modèle.
        // En jeu : un matériau partagé par (matériau, texture), pour que le SRP Batcher et l'instancing regroupent
        // tous les ballons d'une sorte (un MaterialPropertyBlock l'empêchait, et ColorTint l'effaçait en teintant).
        // Hors Play (plateau du hub dans l'éditeur) : MaterialPropertyBlock, rien n'est enregistré dans la scène.
        public static void ApplyTexture(GameObject model, BalloonKind kind)
        {
            var v = Instance;
            if (!v || (int)kind >= v.textures.Length || !v.textures[(int)kind]) return;
            var texture = v.textures[(int)kind];
            block ??= new MaterialPropertyBlock();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (Application.isPlaying)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Textured(mats[i], texture);
                    r.sharedMaterials = mats;
                    continue;
                }
                r.GetPropertyBlock(block);
                block.SetTexture(BaseMapId, texture);
                block.SetTexture(MainTexId, texture);
                r.SetPropertyBlock(block);
            }
        }

        static readonly System.Collections.Generic.Dictionary<(Material, Texture), Material> textured =
            new System.Collections.Generic.Dictionary<(Material, Texture), Material>();

        // Pas de rechargement du code au Play : on vide le cache, ses matériaux ont été détruits à la fin du Play précédent
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => textured.Clear();

        static Material Textured(Material source, Texture texture)
        {
            if (!source) return null;
            if (!textured.TryGetValue((source, texture), out var m) || !m)
            {
                m = new Material(source) { name = $"{source.name} {texture.name}", enableInstancing = true };
                if (m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, texture);
                if (m.HasProperty(MainTexId)) m.SetTexture(MainTexId, texture);
                textured[(source, texture)] = m;
            }
            return m;
        }
    }
}
