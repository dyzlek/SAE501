using UnityEngine;

namespace SAE
{
    // L'apparence d'un singe, construite par Visuals.MonkeyPiece :
    // - « Corps » : le cube couleur de la rareté. Avec un modèle 3D, il reste invisible mais sert toujours
    //   de repère (taille, Mirrored pour la miniature du plateau) ;
    // - « Modele » : le modèle 3D du type de singe (s'il existe) ;
    // - « Aura » : les flammes de la couleur de la rareté.
    // Les autres scripts passent par ici pour griser, teinter ou cacher le singe, quel que soit son rendu.
    public class MonkeyView : MonoBehaviour
    {
        public Monkey monkey;
        public ColorTint body;
        public GameObject model;     // null : pas de modèle, on voit le cube
        public Aura aura;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock block;
        Renderer[] modelRenderers;   // gardés en mémoire : Tint est appelé à chaque frame par l'aperçu de pose
        Color modelColor = Color.white;

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

        // La couleur multiplie la texture du modèle (blanc = couleurs d'origine), sans créer de matériau.
        void TintModel(Color color)
        {
            if (modelRenderers != null && color == modelColor) return;   // déjà de cette couleur
            modelColor = color;
            modelRenderers ??= model.GetComponentsInChildren<Renderer>();
            block ??= new MaterialPropertyBlock();
            foreach (var r in modelRenderers)
            {
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
