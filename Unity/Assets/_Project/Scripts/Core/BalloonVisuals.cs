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

        // Pose la texture de la sorte sur tous les morceaux d'un modèle (sans créer de matériau).
        public static void ApplyTexture(GameObject model, BalloonKind kind)
        {
            var v = Instance;
            if (!v || (int)kind >= v.textures.Length || !v.textures[(int)kind]) return;
            block ??= new MaterialPropertyBlock();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetTexture(BaseMapId, v.textures[(int)kind]);
                block.SetTexture(MainTexId, v.textures[(int)kind]);
                r.SetPropertyBlock(block);
            }
        }
    }
}
