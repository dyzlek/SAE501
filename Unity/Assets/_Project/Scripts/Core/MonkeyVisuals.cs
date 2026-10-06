using UnityEngine;

namespace SAE
{
    // Les modèles 3D des singes (un par type) et le matériau de leur aura, rangés dans un seul asset :
    // Assets/_Project/Resources/MonkeyVisuals.asset, rempli par le menu SAE → Brancher les modèles des singes.
    // Il est dans un dossier Resources pour que le code le trouve aussi en jeu, sans glisser de référence à la main.
    [CreateAssetMenu(menuName = "SAE/Modèles des singes")]
    public class MonkeyVisuals : ScriptableObject
    {
        public GameObject[] models = new GameObject[MonkeyData.TypeCount];   // dans l'ordre de MonkeyType
        public float[] yaw = new float[MonkeyData.TypeCount];                 // rotation (degrés) pour que le modèle regarde vers -Z
        public Material auraMaterial;                                         // particules additives (flammes)

        static MonkeyVisuals instance;

        public static MonkeyVisuals Instance
        {
            get
            {
                if (!instance) instance = Resources.Load<MonkeyVisuals>("MonkeyVisuals");
                return instance;
            }
        }

        // Le modèle d'un type de singe, ou null s'il n'y en a pas encore (on garde alors le cube).
        public static GameObject Model(MonkeyType type, out float yaw)
        {
            var visuals = Instance;
            yaw = 0f;
            if (!visuals || (int)type >= visuals.models.Length) return null;
            if ((int)type < visuals.yaw.Length) yaw = visuals.yaw[(int)type];
            return visuals.models[(int)type];
        }
    }
}
