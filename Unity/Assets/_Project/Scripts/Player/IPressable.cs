namespace SAE
{
    // Ce que la main enfonce en le touchant : les boutons (JOUER, LANCER, Vider, améliorations) et le coffre.
    // C'est HandPress, au bout du doigt de chaque manette, qui appelle Press().
    public interface IPressable
    {
        void Press();
    }
}
