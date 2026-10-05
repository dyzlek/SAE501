namespace SAE
{
    // Tags Unity du projet. Ils sont créés automatiquement dans Project Settings → Tags (voir Editor/TagSetup.cs).
    // Utilisation : gameObject.CompareTag(Tags.Ballon), GameObject.FindGameObjectsWithTag(Tags.Singe)…
    public static class Tags
    {
        public const string Ballon = "Ballon";
        public const string Singe = "Singe";
        public const string Joueur = "Joueur";
        public const string Plateau = "Plateau";
        public const string Bibliotheque = "Bibliotheque";
        public const string Piste = "Piste";
        public const string Terrain = "Terrain";
        public const string Bouton = "Bouton";

        public static readonly string[] All = { Ballon, Singe, Joueur, Plateau, Bibliotheque, Piste, Terrain, Bouton };
    }
}
