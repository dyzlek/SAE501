using UnityEditor;
using UnityEngine;

namespace SAE.EditorTools
{
    // Ajoute les tags de Tags.All dans Project Settings → Tags à chaque chargement de l'éditeur,
    // pour que chaque membre de l'équipe les ait sans rien faire.
    [InitializeOnLoad]
    public static class TagSetup
    {
        static TagSetup() => EnsureTags();

        public static void EnsureTags()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var manager = new SerializedObject(assets[0]);
            var tags = manager.FindProperty("tags");

            bool changed = false;
            foreach (var tag in Tags.All)
            {
                bool exists = false;
                for (int i = 0; i < tags.arraySize; i++)
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
                if (exists) continue;

                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
                changed = true;
            }

            if (changed)
            {
                manager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("Tags SAE ajoutés : " + string.Join(", ", Tags.All));
            }
        }
    }
}
