using UnityEngine;

/// Récolteur automatique, volontairement limité pour que ramasser à la main reste plus intéressant :
///  - il ne ramasse qu'UNE banane toutes les « intervalle » secondes,
///  - il ne prend que les bananes au sol depuis un moment (« delaiAvantRamassage » = part de la durée de pourriture),
///  - il garde une commission : seule « partJoueur » de la valeur va dans le panier.
/// Il dépose directement dans « panier ». Achat / améliorations : Acheter(), AmeliorerVitesse(), AmeliorerPart()
/// (le paiement sera branché plus tard sur le système d'argent ; les prix sont déjà calculés).
public class Recolteur : MonoBehaviour
{
    public Bananier bananier;
    public Panier panier;

    [Header("Achat")]
    public int prixAchat = 500;
    public bool achete = false;

    [Header("Réglages")]
    [Tooltip("Secondes entre deux ramassages")] public float intervalle = 6f;
    [Tooltip("La banane doit avoir passé cette part de sa durée de pourriture au sol")]
    [Range(0f, 1f)] public float delaiAvantRamassage = 0.5f;
    [Tooltip("Part de la valeur reversée au joueur")] [Range(0f, 1f)] public float partJoueur = 0.6f;

    [Header("Améliorations")]
    public float intervalleMin = 2f;
    public float gainIntervalle = 1f;
    public float partMax = 0.85f;
    public float gainPart = 0.05f;
    public int prixAmelioration = 300;
    public float multiplicateurPrix = 1.8f;
    int nbAmeliorations;

    float minuteur;

    public int PrixProchaineAmelioration => Mathf.RoundToInt(prixAmelioration * Mathf.Pow(multiplicateurPrix, nbAmeliorations));

    public bool Acheter()
    {
        if (achete) return false;
        achete = true;
        return true;
    }

    public bool AmeliorerVitesse()
    {
        if (!achete || intervalle <= intervalleMin) return false;
        intervalle = Mathf.Max(intervalleMin, intervalle - gainIntervalle); nbAmeliorations++;
        return true;
    }

    public bool AmeliorerPart()
    {
        if (!achete || partJoueur >= partMax) return false;
        partJoueur = Mathf.Min(partMax, partJoueur + gainPart); nbAmeliorations++;
        return true;
    }

    void Update()
    {
        if (!achete || bananier == null || panier == null) return;
        minuteur += Time.deltaTime;
        if (minuteur < intervalle) return;

        // la plus vieille banane encore bonne, posée depuis assez longtemps, que personne ne tient
        Banane cible = null;
        foreach (var b in bananier.BananesAuSol)
        {
            if (b == null || b.EstPourrie || b.EnMain || b.Progression < delaiAvantRamassage) continue;
            if (cible == null || b.Progression > cible.Progression) cible = b;
        }
        if (cible == null) return;

        minuteur = 0f;
        panier.RecevoirPart(cible, partJoueur);
    }
}
