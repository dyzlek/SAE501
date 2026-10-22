using System;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Menu SAE > Coop > Créer les prefabs réseau : les trois objets réseau du coop (Netcode for GameObjects),
    // rangés dans Assets/_Project/Resources/Coop où Coop les charge. NGO reconnaît un objet réseau à l'identifiant
    // de son prefab (GlobalObjectIdHash), calculé par Unity : c'est pour ça qu'il faut de vrais prefabs, pas des objets
    // créés pendant le jeu. À relancer seulement si on change ce qu'il y a dedans.
    public static class CoopPrefabs
    {
        const string Folder = "Assets/_Project/Resources/Coop";

        [MenuItem("SAE/Coop/Créer les prefabs réseau")]
        public static void Create()
        {
            Directory.CreateDirectory(Folder);
            Save("CoopJoueur", go => go.AddComponent<CoopPlayer>());
            Save("CoopPartie", go => go.AddComponent<CoopGame>());
            Save("CoopBallon", go =>
            {
                go.AddComponent<CoopBalloon>();
                // La position et la rotation du ballon, envoyées par l'hôte. Les invités glissent d'une position à l'autre
                // (Interpolate). La taille ne change pas : on ne l'envoie pas. Demi-précision : deux fois moins de données.
                var sync = go.AddComponent<NetworkTransform>();
                sync.SyncScaleX = sync.SyncScaleY = sync.SyncScaleZ = false;
                sync.UseHalfFloatPrecision = true;
                sync.UseQuaternionSynchronization = true;
                sync.UseQuaternionCompression = true;
                sync.Interpolate = true;
            });
            AssetDatabase.SaveAssets();
            Debug.Log($"Coop : prefabs réseau créés dans {Folder}");
        }

        static void Save(string name, Action<GameObject> build)
        {
            var go = new GameObject(name);
            go.AddComponent<NetworkObject>();
            build(go);
            string path = $"{Folder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            // En chargeant le prefab, NetworkObject calcule son identifiant (OnValidate) ; SaveAssets l'écrit dans le fichier
            var asset = AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
            EditorUtility.SetDirty(asset);
        }
    }
}
