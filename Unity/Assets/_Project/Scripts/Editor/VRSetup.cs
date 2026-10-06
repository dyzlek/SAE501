using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace SAE.EditorTools
{
    // Menu SAE → Configurer la VR : fait en une fois les réglages du cours (supports 2 et 3), sans cliquer partout.
    //   - XR Plug-in Management : OpenXR activé pour Android (le casque) et PC (Quest Link) ;
    //   - OpenXR : support Meta Quest + profils des manettes Touch (pas l'Eye Gaze) ;
    //   - Android : IL2CPP, ARM64, API 32 minimum (exigé par le Quest) ;
    //   - XRI : couche d'interaction « Teleport » (n° 31, celle des Starter Assets). Le simulateur XR n'est PAS lancé
    //     par XRI (il remplacerait le vrai casque) : c'est SimulatorWhenNoHeadset qui le lance, seulement sans casque.
    // Le générateur l'appelle à chaque fois : on peut le relancer sans risque.
    public static class VRSetup
    {
        const string SamplesFolder = "Assets/Samples/XR Interaction Toolkit/3.6.1";
        public const string RigPrefab = SamplesFolder + "/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string LayerSettings = "Assets/XRI/Settings/Resources/InteractionLayerSettings.asset";
        const string SimulatorSettings = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
        public const int TeleportLayer = 31;

        [MenuItem("SAE/Configurer la VR")]
        public static void Configure()
        {
            EnableOpenXR(BuildTargetGroup.Android);
            EnableOpenXR(BuildTargetGroup.Standalone);

            var android = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (android) EnableFeatures(android, quest: true);
            var pc = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (pc) EnableFeatures(pc, quest: false);

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            SetAssetProperty(LayerSettings, so => so.FindProperty("m_LayerNames").GetArrayElementAtIndex(TeleportLayer).stringValue = "Teleport");
            SetAssetProperty(SimulatorSettings, so => so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = false);

            AssetDatabase.SaveAssets();
            Debug.Log("VR configurée : OpenXR (Android + PC), Meta Quest, couche Teleport.");
        }

        // Coche « OpenXR » dans Project Settings → XR Plug-in Management pour cette plateforme.
        static void EnableOpenXR(BuildTargetGroup group)
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings = perTarget.SettingsForBuildTarget(group);
            settings.InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(settings.Manager, typeof(OpenXRLoader).FullName, group);
            EditorUtility.SetDirty(perTarget);
        }

        // Le casque a besoin du support Meta Quest ; les deux plateformes ont besoin des profils des manettes.
        static void EnableFeatures(OpenXRSettings settings, bool quest)
        {
            foreach (var feature in settings.GetFeatures())
            {
                bool wanted = feature is OculusTouchControllerProfile
                              || feature is MetaQuestTouchPlusControllerProfile
                              || feature is MetaQuestTouchProControllerProfile
                              || (quest && feature is MetaQuestFeature);
                if (!wanted || feature.enabled) continue;
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        // Modifie un réglage d'un fichier .asset comme le ferait l'Inspector.
        static void SetAssetProperty(string path, System.Action<SerializedObject> edit)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (!asset) { Debug.LogWarning("Configurer la VR : réglage introuvable " + path); return; }
            var so = new SerializedObject(asset);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
