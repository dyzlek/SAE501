using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SAE.EditorTools
{
    // Menu SAE → Mode de jeu : jouer en VR (casque, Quest Link) ou au clavier/souris sur PC, pour tester vite.
    // Le choix est retenu (EditorPrefs) et appliqué à la scène ouverte (PlayerMode) ; le générateur l'applique aussi.
    // En mode PC, OpenXR ne démarre pas dans l'éditeur (sinon l'image partirait dans le casque).
    // Le build casque (Android) est toujours en VR, quel que soit ce choix.
    public static class PlayerModeMenu
    {
        const string Key = "SAE.ModeVR";
        const string VrItem = "SAE/Mode de jeu/VR (casque)";
        const string PcItem = "SAE/Mode de jeu/PC (clavier-souris)";

        public static bool IsVR => EditorPrefs.GetBool(Key, true);

        [MenuItem(VrItem)] static void SetVR() => Set(true);
        [MenuItem(PcItem)] static void SetPC() => Set(false);

        // Coche le mode actif dans le menu
        [MenuItem(VrItem, true)] static bool CheckVR() { Menu.SetChecked(VrItem, IsVR); Menu.SetChecked(PcItem, !IsVR); return !EditorApplication.isPlaying; }
        [MenuItem(PcItem, true)] static bool CheckPC() { Menu.SetChecked(VrItem, IsVR); Menu.SetChecked(PcItem, !IsVR); return !EditorApplication.isPlaying; }

        static void Set(bool vr)
        {
            EditorPrefs.SetBool(Key, vr);
            if (!Apply()) Debug.LogWarning("Mode de jeu : pas de joueur à choisir dans cette scène. Lance SAE → Générer le prototype.");
            else Debug.Log(vr ? "Mode de jeu : VR (casque)." : "Mode de jeu : PC (clavier-souris).");
        }

        // Applique le mode retenu à la scène ouverte. false si elle n'a pas de PlayerMode (scène à régénérer).
        public static bool Apply()
        {
            // OpenXR ne démarre dans l'éditeur (plateforme PC) qu'en mode VR
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
            {
                var pc = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (pc) { pc.InitManagerOnStart = IsVR; EditorUtility.SetDirty(perTarget); AssetDatabase.SaveAssets(); }
            }

            var mode = Object.FindFirstObjectByType<SAE.PlayerMode>(FindObjectsInactive.Include);
            if (!mode) return false;
            mode.vr = IsVR;
            EditorUtility.SetDirty(mode);
            EditorSceneManager.MarkSceneDirty(mode.gameObject.scene);
            EditorSceneManager.SaveScene(mode.gameObject.scene);
            return true;
        }
    }
}
