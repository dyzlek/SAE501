namespace SAE
{
    // Les mesures du hub (la cabane), partagées par le générateur de scène (PrototypeGenerator) et par ce qui
    // s'installe au lancement (HarvesterSetup). Tout est posé sur un cercle autour du joueur.
    // Angles en degrés : 0 = devant le joueur (le plateau), positif = à droite. Distances en mètres depuis le centre.
    public static class HubLayout
    {
        public const float Ring = 2.8f;              // rayon du cercle : serré, pour tout atteindre en VR en un ou deux pas
        public const float CabinRadius = 3.4f;       // les murs de la cabane (le bananier est dehors, derrière la grande porte)
        public const float CabinHeight = 3.0f;       // hauteur des murs (le toit conique monte à 4,8 m)
        public const float CarpetRadius = 1.5f;      // le tapis rond au centre, là où se tient le joueur

        public const float BasketAngle = 45f;        // le panier, à côté du plateau, après Vider
        public const float BasketRadius = Ring - 0.3f;

        public const float HarvesterPanelAngle = -132f;   // le panneau RÉCOLTEUR (la caisse est accrochée au mur au-dessus)
        public const float HarvesterHomeAngle = -160f;    // où le singe récolteur attend, devant la table des bananes
        public const float HarvesterHomeRadius = Ring - 1.1f;
    }
}
