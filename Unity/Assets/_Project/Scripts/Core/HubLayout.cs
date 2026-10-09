namespace SAE
{
    // Les mesures du hub (la cabane), partagées par le générateur de scène (PrototypeGenerator) et par le script
    // Blender de la cabane. Tout est posé sur un cercle autour du joueur. (Le bananier est dans la bananeraie.)
    // Angles en degrés : 0 = devant le joueur (le plateau), positif = à droite. Distances en mètres depuis le centre.
    public static class HubLayout
    {
        public const float Ring = 2.8f;              // rayon du cercle : serré, pour tout atteindre en VR en un ou deux pas
        public const float CabinRadius = 3.4f;       // les murs de la cabane (la bananeraie est derrière la grande porte)
        public const float CabinHeight = 3.0f;       // hauteur des murs (le toit conique monte à 4,8 m)
        public const float CarpetRadius = 1.5f;      // le tapis rond au centre, là où se tient le joueur

        public const float MoneyBoardAngle = -136f;  // la caisse (l'argent), accrochée au mur
    }
}
