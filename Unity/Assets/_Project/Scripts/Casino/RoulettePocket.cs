using UnityEngine;

namespace SAE
{
    // Une case de la roue : on la touche avec la main, on la vise avec le rayon ou on clique dessus (mode PC)
    // pour parier sur sa couleur. Toutes les cases de cette couleur s'entourent alors de leur lueur (RouletteTable.Select).
    public class RoulettePocket : MonoBehaviour, IPressable
    {
        public RouletteTable table;
        public RouletteColor color;
        public ColorTint glow;          // la lueur dorée juste sous la case, un peu plus grande qu'elle (cachée au départ)

        public void Press() => table.Select(color);
    }
}
