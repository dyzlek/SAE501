using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE → Calculer l'occlusion : précalcule ce que chaque endroit de la scène peut voir (occlusion culling).
    // En jeu, Unity ne dessine plus ce qui est caché derrière un mur : depuis la cabane, la bananeraie et la carte
    // ne coûtent presque plus rien. Seuls les objets « static » comptent (PrototypeGenerator.MakeStatic, MeshBatch).
    // À relancer après chaque SAE → Générer le prototype (le calcul prend de quelques secondes à une minute).
    public static class OcclusionMenu
    {
        [MenuItem("SAE/Calculer l'occlusion (Hub et Labyrinthe)")]
        static void Bake()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var start = EditorSceneManager.GetActiveScene().path;

            // La carte (Labyrinthe) est chargée par-dessus le hub pendant le jeu : Unity demande de calculer
            // l'occlusion avec les deux scènes ouvertes ensemble, sinon les données d'une scène ignorent l'autre.
            var hub = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hub.unity", OpenSceneMode.Single);
            var map = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Labyrinthe.unity", OpenSceneMode.Additive);
            // Tailles en mètres : un objet de moins de 1 m ne cache rien, 25 cm de précision pour les trous (fenêtres, portes)
            StaticOcclusionCulling.smallestOccluder = 1f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            if (StaticOcclusionCulling.Compute()) Debug.Log("Occlusion calculée (Hub + Labyrinthe).");
            else Debug.LogWarning("Occlusion : échec du calcul.");
            EditorSceneManager.SaveScene(hub);
            EditorSceneManager.SaveScene(map);

            if (!string.IsNullOrEmpty(start) && start != hub.path) EditorSceneManager.OpenScene(start);
        }
    }
}
