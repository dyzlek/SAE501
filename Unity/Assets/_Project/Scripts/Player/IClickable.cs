namespace SAE
{
    // Tout objet sur lequel le joueur peut cliquer (plus tard : saisir avec la main en VR).
    public interface IClickable
    {
        string Hint { get; }
        void OnClick(PlayerController player);
    }
}
