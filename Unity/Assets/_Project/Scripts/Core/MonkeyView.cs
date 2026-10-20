using UnityEngine;

namespace SAE
{
    // L'apparence d'un singe, construite par Visuals.MonkeyPiece :
    // - « Corps » : le cube couleur de la rareté. Avec un modèle 3D, il reste invisible mais sert toujours
    //   de repère (taille, le plateau du hub en fait une miniature) ;
    // - « Modele » : le modèle 3D du type de singe (s'il existe) ;
    // - « Aura » : les flammes de la couleur de la rareté.
    // Les autres scripts passent par ici pour griser, teinter ou cacher le singe, quel que soit son rendu.
    // Optimisation : un singe qui ne bouge pas (bibliothèque, plateau, fiche, coffre) est « figé » au lancement :
    // son modèle à squelette (SkinnedMeshRenderer, recalculé à chaque image par le processeur) devient un simple mesh.
    // Seuls les singes animés en code gardent leur squelette : ceux de la carte (TowerAnimator) et les récolteurs.
    public class MonkeyView : MonoBehaviour
    {
        public Monkey monkey;
        public ColorTint body;
        public GameObject model;     // null : pas de modèle, on voit le cube
        public Aura aura;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        Renderer[] modelRenderers;   // gardés en mémoire : Tint est appelé à chaque frame par l'aperçu de pose
        Material[][] originals;      // matériaux d'origine du modèle, d'où partent les copies teintées
        Color modelColor = Color.white;

        // Un mesh figé par modèle (et non par singe) : 200 singes du même type partagent le même, le casque les dessine d'un coup
        static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> frozenMeshes = new System.Collections.Generic.Dictionary<Mesh, Mesh>();

        void Start()
        {
            bool animated = GetComponent<TowerAnimator>() || GetComponentInParent<HarvesterMonkey>();
            if (model && !animated) Freeze();
        }

        // Remplace chaque SkinnedMeshRenderer du modèle par un MeshRenderer, dans la pose actuelle (bras baissés, Visuals.RestPose).
        void Freeze()
        {
            // Déjà teinté (case vide) : on remet d'abord les matériaux d'origine, la teinte est reposée à la fin
            if (modelRenderers != null && originals != null)
                for (int i = 0; i < modelRenderers.Length; i++) if (modelRenderers[i]) modelRenderers[i].sharedMaterials = originals[i];
            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skinned.sharedMesh) continue;
                if (!frozenMeshes.TryGetValue(skinned.sharedMesh, out var mesh))
                {
                    mesh = new Mesh { name = skinned.sharedMesh.name + " (figé)" };
                    // useScale = true : le mesh est ramené dans le repère de l'objet, échelle comprise. Sans ça, l'échelle du FBX
                    // était comptée deux fois (une dans le mesh, une par le transform) et les singes devenaient invisibles.
                    skinned.BakeMesh(mesh, true);
                    mesh.RecalculateBounds();
                    frozenMeshes[skinned.sharedMesh] = mesh;
                }
                var go = skinned.gameObject;
                var materials = skinned.sharedMaterials;
                DestroyImmediate(skinned);   // tout de suite : un objet ne peut pas avoir deux renderers
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }
            var animator = model.GetComponentInChildren<Animator>();
            if (animator) animator.enabled = false;
            modelRenderers = null;   // les renderers ont changé : TintModel les recherchera
            originals = null;
            if (modelColor != Color.white) TintModel(modelColor);   // on remet la teinte déjà posée (case vide, aperçu)
        }

        // Case vide de la bibliothèque : un singe sombre, sans aura (on voit ce qu'on pourrait avoir).
        public void SetEmpty(bool empty, Color emptyColor, Monkey monkey)
        {
            if (aura) aura.gameObject.SetActive(!empty);
            if (model) TintModel(empty ? emptyColor : Color.white);
            else body.Set(empty ? emptyColor : MonkeyData.RarityColor(monkey.level), !empty && MonkeyData.IsRainbow(monkey.level));
        }

        // Aperçu de pose : vert si on peut poser, rouge sinon.
        public void Tint(Color color)
        {
            if (model) TintModel(color);
            else body.Set(color);
        }

        // La couleur multiplie la texture du modèle (blanc = couleurs d'origine).
        // En jeu : matériaux partagés par couleur (ColorTint.Shared), pour que le SRP Batcher regroupe les singes ;
        // hors Play : MaterialPropertyBlock, pour ne rien enregistrer dans la scène.
        void TintModel(Color color)
        {
            if (modelRenderers != null && color == modelColor) return;   // déjà de cette couleur
            modelColor = color;
            if (modelRenderers == null)
            {
                modelRenderers = model.GetComponentsInChildren<Renderer>();
                originals = new Material[modelRenderers.Length][];
                for (int i = 0; i < modelRenderers.Length; i++) originals[i] = modelRenderers[i].sharedMaterials;
            }
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                var r = modelRenderers[i];
                if (Application.isPlaying)
                {
                    var mats = new Material[originals[i].Length];
                    for (int k = 0; k < mats.Length; k++) mats[k] = color == Color.white ? originals[i][k] : ColorTint.Shared(originals[i][k], color);
                    r.sharedMaterials = mats;
                    continue;
                }
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
