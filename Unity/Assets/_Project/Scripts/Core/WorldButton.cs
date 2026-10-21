using System;
using UnityEngine;

namespace SAE
{
    // Un bouton dans le décor : un cube coloré qu'on vise au rayon (RayPress) ou qu'on touche (HandPress), son texte posé
    // devant (pas dans le cube étiré : il serait déformé). Sert au menu principal, à la pause, aux options et au clavier
    // du classement. Create renvoie le texte, pour pouvoir le changer (« MODIFIER » / « ANNULER »).
    // +Z local du parent = dos au joueur : le texte se lit côté -Z.
    public class WorldButton : MonoBehaviour, IPressable
    {
        public Action action;
        public bool interactable = true;   // faux : le bouton est grisé et ne fait rien (CONTINUER sans sauvegarde)

        public static TextMesh Create(Transform parent, string label, Vector3 pos, Vector2 size, Color color, Action action, float textHeight = 0f)
        {
            var go = Visuals.Solid("Bouton " + label, parent, pos, new Vector3(size.x, size.y, 0.04f), color);
            go.AddComponent<WorldButton>().action = action;
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();   // après le collider (Solid)
            go.AddComponent<RayPress>();
            if (textHeight <= 0f) textHeight = label.Length > 1 ? size.y * 0.37f : size.y * 0.55f;
            return Visuals.Text(parent, label, pos + new Vector3(0f, 0f, -0.03f), textHeight, Color.white, title: true);
        }

        public void Press()
        {
            // Le « clic » et la vibration sont déjà joués par RayPress / HandPress
            if (!interactable) { Sounds.Play(Sounds.AchatRefuse, transform.position, 0.6f); return; }
            action?.Invoke();
        }

        // Grise (ou rallume) le bouton
        public void SetInteractable(bool on, Color color)
        {
            interactable = on;
            GetComponent<ColorTint>().Set(on ? color : new Color(0.35f, 0.35f, 0.35f));
        }
    }
}
