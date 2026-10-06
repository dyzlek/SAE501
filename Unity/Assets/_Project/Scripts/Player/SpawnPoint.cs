using UnityEngine;

namespace SAE
{
    // Point d'arrivée du joueur (hub, carte). Il dit si l'on peut marcher dans cette zone :
    // au hub, le joueur reste au centre et fait tout en tournant la tête (tout est à portée du regard) ;
    // sur la carte, il peut se déplacer.
    public class SpawnPoint : MonoBehaviour
    {
        public bool canWalk = true;
    }
}
