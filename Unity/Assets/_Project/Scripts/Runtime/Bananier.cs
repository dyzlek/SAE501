using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Bananier : fait tomber des bananes régulièrement.
/// 3 statistiques améliorables :
///   - Fréquence : temps entre deux bananes (diminue à chaque niveau)
///   - Pourriture : temps avant qu'une banane au sol pourrisse (augmente à chaque niveau)
///   - Valeur : argent rapporté par une banane (augmente à chaque niveau)
public class Bananier : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        [Tooltip("Valeur au niveau 1")] public float valeurDeBase = 1f;
        [Tooltip("Ajouté (ou retiré si négatif) à chaque niveau")] public float parNiveau = 1f;
        [Tooltip("Limite (min ou max selon le sens)")] public float limite = 999f;
        [Tooltip("Prix de la 1re amélioration")] public int prixDeBase = 50;
        [Tooltip("Le prix est multiplié par ça à chaque niveau")] public float multiplicateurPrix = 1.6f;
        [Tooltip("Niveau max (0 = illimité)")] public int niveauMax = 10;
        public int niveau = 1;

        public float Valeur
        {
            get
            {
                float v = valeurDeBase + parNiveau * (niveau - 1);
                return parNiveau >= 0 ? Mathf.Min(v, limite) : Mathf.Max(v, limite);
            }
        }
        public bool EstAuMax => niveauMax > 0 && niveau >= niveauMax;
        public int PrixAmelioration => Mathf.RoundToInt(prixDeBase * Mathf.Pow(multiplicateurPrix, niveau - 1));
    }

    [Header("Bananes")]
    public Banane prefabBanane;
    [Tooltip("Point d'apparition (l'empty BananaSpawn du modèle). Vide = le bananier lui-même.")]
    public Transform pointApparition;
    [Tooltip("Les bananes tombent du côté de cet objet (le panier). Vide = tout autour.")]
    public Transform versCible;
    [Tooltip("Socle / bac du bananier : sert à calculer la distance mini pour tomber EN DEHORS. Vide = distanceMin à la main.")]
    public Renderer socle;
    [Tooltip("Marge ajoutée au bord du socle")] public float margeSocle = 0.15f;
    [Tooltip("Distance mini depuis le centre (recalculée depuis le socle si renseigné)")] public float distanceMin = 0.9f;
    [Tooltip("Profondeur de la zone de chute au-delà de distanceMin")] public float largeurZone = 0.35f;
    [Tooltip("Écart d'angle autour de la direction du panier (degrés)")] public float angleDispersion = 30f;
    [Tooltip("Distance à garder avant le panier (pour ne pas tomber dedans toute seule)")] public float margePanier = 0.45f;
    [Tooltip("Hauteur de la banane posée au sol (moitié de sa taille)")] public float hauteurAuSol = 0.1f;
    [Tooltip("Nombre max de bananes au sol en même temps")] public int maxAuSol = 12;

    [Header("Statistiques améliorables")]
    [Tooltip("Secondes entre deux bananes")]
    public Stat frequence = new Stat { valeurDeBase = 8f, parNiveau = -0.6f, limite = 1.5f, prixDeBase = 60 };
    [Tooltip("Secondes avant qu'une banane pourrisse")]
    public Stat pourriture = new Stat { valeurDeBase = 15f, parNiveau = 3f, limite = 60f, prixDeBase = 40 };
    [Tooltip("Argent rapporté par une banane")]
    public Stat valeur = new Stat { valeurDeBase = 5f, parNiveau = 3f, limite = 999f, prixDeBase = 80 };

    [Header("Boing (optionnel)")]
    [Tooltip("Partie qui fait le boing (Bananier_Arbre). Vide = pas d'animation.")]
    public Transform arbre;
    public float dureeBoing = 0.35f;

    public bool enProduction = true;

    readonly List<Banane> auSol = new List<Banane>();
    float minuteur;
    Vector3 echelleArbre;

    public float Frequence => frequence.Valeur;
    public float DureePourriture => pourriture.Valeur;
    public int ValeurBanane => Mathf.RoundToInt(valeur.Valeur);
    public IReadOnlyList<Banane> BananesAuSol => auSol;

    void Start()
    {
        if (pointApparition == null) pointApparition = transform;
        if (arbre != null) echelleArbre = arbre.localScale;
        if (socle != null)
        {
            var e = socle.bounds.extents;
            distanceMin = Mathf.Max(e.x, e.z) + margeSocle;
        }
    }

    /// Point au sol où tombe la prochaine banane : à l'extérieur du socle, du côté du panier.
    Vector3 PointDeChute()
    {
        Vector3 centre = transform.position;
        Vector3 dir; float dMax = distanceMin + largeurZone;
        if (versCible != null)
        {
            Vector3 v = versCible.position - centre; v.y = 0;
            dir = v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
            dir = Quaternion.Euler(0, Random.Range(-angleDispersion, angleDispersion), 0) * dir;
            dMax = Mathf.Min(dMax, v.magnitude - margePanier);       // ne pas atterrir dans le panier
        }
        else
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
        }
        float d = Random.Range(distanceMin, Mathf.Max(distanceMin, dMax));
        return centre + dir * d + Vector3.up * hauteurAuSol;
    }

    void Update()
    {
        if (!enProduction) return;
        auSol.RemoveAll(b => b == null);
        if (auSol.Count >= maxAuSol) return;

        minuteur += Time.deltaTime;
        if (minuteur >= Frequence)
        {
            minuteur = 0f;
            FaireTomber();
        }
    }

    public void FaireTomber()
    {
        if (prefabBanane == null) { Debug.LogWarning("Bananier : pas de prefab de banane."); return; }
        Vector3 pos = pointApparition.position;
        var b = Instantiate(prefabBanane, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        b.Initialiser(this, ValeurBanane, DureePourriture, PointDeChute());
        auSol.Add(b);
        if (arbre != null) { StopAllCoroutines(); StartCoroutine(Boing()); }
    }

    internal void Retirer(Banane b) { auSol.Remove(b); }

    // ---------- améliorations (à appeler depuis vos boutons / l'UI) ----------
    public bool AmeliorerFrequence() => Ameliorer(frequence);
    public bool AmeliorerPourriture() => Ameliorer(pourriture);
    public bool AmeliorerValeur() => Ameliorer(valeur);

    /// Monte la stat d'un niveau. Le paiement n'est pas géré ici (pas encore de système d'argent) :
    /// le prix est disponible dans stat.PrixAmelioration pour plus tard.
    bool Ameliorer(Stat s)
    {
        if (s.EstAuMax) return false;
        s.niveau++;
        return true;
    }

    IEnumerator Boing()
    {
        float t = 0;
        while (t < dureeBoing)
        {
            float k = Mathf.Sin(t / dureeBoing * Mathf.PI * 2f) * (1f - t / dureeBoing);
            arbre.localScale = new Vector3(echelleArbre.x * (1 + 0.12f * k), echelleArbre.y * (1 - 0.18f * k), echelleArbre.z * (1 + 0.12f * k));
            t += Time.deltaTime;
            yield return null;
        }
        arbre.localScale = echelleArbre;
    }
}
