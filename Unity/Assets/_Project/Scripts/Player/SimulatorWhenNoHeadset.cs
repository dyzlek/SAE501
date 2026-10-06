#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SAE
{
    // Le simulateur XR (tester au clavier/souris dans l'éditeur) ne se lance que si AUCUN casque n'est branché.
    // Sinon il prend la place du vrai casque (Quest Link) : la tête et les manettes ne bougeraient plus.
    // Éditeur seulement : le build casque n'en a jamais besoin.
    static class SimulatorWhenNoHeadset
    {
        const string SimulatorPrefab = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

        // Après le chargement de la scène : OpenXR a déjà essayé de démarrer le casque à ce moment-là.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfNeeded()
        {
            var manager = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
            if (manager && manager.activeLoader) return;   // un casque tourne : pas de simulateur

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefab);
            if (prefab) Object.Instantiate(prefab).name = "XR Interaction Simulator";
        }
    }
}
#endif
