using UnityEngine;

namespace SAE
{
    // Une des 3 grandes zones du tapis de la roulette : ROUGE ×2, NOIR ×2, VERT ×36 (comme un vrai tapis de casino).
    // On la touche avec la main, on la vise avec le rayon ou on clique dessus (mode PC) pour parier sur sa couleur :
    // son cadre s'allume, les bananes misées viennent s'y poser, et les cases de cette couleur s'allument sur la roue.
    public class RouletteBetZone : MonoBehaviour, IPressable
    {
        public RouletteTable table;
        public RouletteColor color;
        public ColorTint glow;          // le cadre doré autour de la zone (caché tant qu'elle n'est pas choisie)

        public void Press() => table.Select(color);
    }
}
