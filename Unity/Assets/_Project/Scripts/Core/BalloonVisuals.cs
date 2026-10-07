using UnityEngine;

namespace SAE
{
    // Les modèles 3D des ballons (un par sorte), rangés dans un seul asset :
    // Assets/_Project/Resources/BalloonVisuals.asset, rempli par le menu SAE → Brancher les modèles des ballons.
    // Comme MonkeyVisuals, il est dans un dossier Resources pour que le code le trouve aussi en jeu.
    [CreateAssetMenu(menuName = "SAE/Modèles des ballons")]
    public class BalloonVisuals : ScriptableObject
    {
        public GameObject[] models = new GameObject[5];   // dans l'ordre de BalloonKind

        static BalloonVisuals instance;

        // Le modèle d'une sorte de ballon, ou null s'il n'y en a pas (on garde alors la sphère).
        public static GameObject Model(BalloonKind kind)
        {
            if (!instance) instance = Resources.Load<BalloonVisuals>("BalloonVisuals");
            if (!instance || (int)kind >= instance.models.Length) return null;
            return instance.models[(int)kind];
        }
    }
}
