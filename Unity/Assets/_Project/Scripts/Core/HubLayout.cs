namespace SAE
{
    // Les mesures du hub (la cabane), partagées par le générateur de scène (PrototypeGenerator) et par ce qui
    // s'installe au lancement (HarvesterSetup). Tout est posé sur un cercle autour du joueur.
    // Angles en degrés : 0 = devant le joueur (le plateau), positif = à droite. Distances en mètres depuis le centre.
    public static class HubLayout
    {
        public const float Ring = 3.5f;              // rayon du cercle : serré, pour tout atteindre en VR en un ou deux pas
        public const float CabinRadius = 5.2f;       // les murs de la cabane (le bananier est à Ring + 0,7)
        public const float CabinHeight = 3.2f;       // hauteur sous plafond
        public const float CarpetRadius = 2.0f;      // le tapis rond au centre, là où se tient le joueur

        public const float BasketAngle = 40f;        // le panier, à côté du plateau, après Vider
        public const float BasketRadius = Ring - 0.6f;

        public const float HarvesterPanelAngle = -120f;   // le panneau RÉCOLTEUR, entre la caisse et la bibliothèque
        public const float HarvesterHomeAngle = -171f;    // où le singe récolteur attend, devant la table des bananes
        public const float HarvesterHomeRadius = Ring - 1.2f;
    }
}
