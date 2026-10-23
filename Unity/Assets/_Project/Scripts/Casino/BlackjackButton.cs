using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SAE
{
    // Un gros bouton de la borne de blackjack (JOUER, CARTE, RESTER, DOUBLER, et les touches du terminal) :
    // un socle sombre et un dessus coloré qui s'enfonce. On l'enfonce avec la main (HandPress) ou on le vise au rayon
    // (RayPress), ou on clique dessus en mode PC. Gris quand il ne peut rien faire, flash rouge si l'appui est refusé
    // (comme les jetons de la roulette, RouletteButton).
    public class BlackjackButton : MonoBehaviour, IPressable
    {
        static readonly Color Base = new Color(0.08f, 0.08f, 0.1f);
        static readonly Color Off = new Color(0.35f, 0.35f, 0.35f);
        static readonly Color RefusedColor = new Color(0.9f, 0.15f, 0.1f);

        Func<bool> action;     // ce que fait l'appui ; renvoie false si c'est refusé
        Func<bool> usable;     // peut-il servir maintenant ? (sinon il est gris)
        Color color;
        Transform cap;
        ColorTint capTint;
        Vector3 capRest;
        float pressed, refused;

        // Un bouton de 'size' (largeur, hauteur) posé sur la face -Z du parent (le côté du joueur), dessus vers -Z
        // textColor : blanc sur les boutons foncés, foncé sur les touches claires du pavé (sinon on ne lit rien)
        public static BlackjackButton Create(Transform parent, string label, Vector3 localPos, Vector2 size, Color color,
                                             Func<bool> action, Func<bool> usable, float textHeight, Color? textColor = null)
        {
            var root = new GameObject("Bouton " + label).transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            Visuals.Box("Socle", root, new Vector3(0f, 0f, 0.01f), new Vector3(size.x + 0.02f, size.y + 0.02f, 0.02f), Base);

            // Le dessus porte le collider : c'est lui qu'on touche ou qu'on vise
            var cap = Visuals.Solid("Dessus", root, new Vector3(0f, 0f, -0.015f), new Vector3(size.x, size.y, 0.03f), color);
            var b = cap.AddComponent<BlackjackButton>();
            cap.AddComponent<XRSimpleInteractable>();   // après le collider (Solid)
            cap.AddComponent<RayPress>();
            b.action = action;
            b.usable = usable;
            b.color = color;
            b.cap = cap.transform;
            b.capTint = cap.GetComponent<ColorTint>();
            b.capRest = cap.transform.localPosition;
            Visuals.Text(cap.transform, label, new Vector3(0f, 0f, -0.6f), textHeight / size.y, textColor ?? Color.white, title: true)
                .transform.localScale = new Vector3(size.y / size.x, 1f, 1f);   // le texte, enfant du dessus étiré : on le remet droit
            return b;
        }

        public void Press()
        {
            pressed = 1f;
            if (!action()) { refused = 1f; Sounds.Play(Sounds.AchatRefuse, transform.position, 0.6f); }
        }

        void Update()
        {
            pressed = Mathf.MoveTowards(pressed, 0f, Time.deltaTime * 5f);
            refused = Mathf.MoveTowards(refused, 0f, Time.deltaTime * 2f);
            cap.localPosition = capRest + Vector3.forward * (0.012f * pressed);   // il s'enfonce vers le socle (+Z)
            capTint.Set(Color.Lerp(usable() ? color : Off, RefusedColor, refused));
        }
    }
}
