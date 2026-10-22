using UnityEngine;

namespace SAE
{
    // Une case de la roue (Case_00 à Case_36 du modèle Blender/roulette.py). On ne parie plus en la touchant
    // (c'est le tapis, RouletteBetZone) : elle sert à montrer où l'on gagne. Les cases de la couleur choisie
    // pulsent vers le doré, et la case gagnante clignote en blanc (RouletteTable).
    public class RoulettePocket : MonoBehaviour
    {
        public RouletteColor color;
        public ColorTint tint;          // la couleur de la case (rouge, noire ou verte au repos)
        public Color baseColor;         // sa couleur au repos

        public void Rest() => tint.Set(baseColor);
        public void Highlight(Color glow, float amount) => tint.Set(Color.Lerp(baseColor, glow, amount));
    }
}
