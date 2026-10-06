using System.Collections;
using UnityEngine;

/// Une banane tombée du bananier.
/// - Elle tombe au pied de l'arbre et attend que le joueur la ramasse.
/// - Elle brunit petit à petit ; au bout de « dureePourriture » secondes elle est pourrie :
///   elle ne vaut plus rien et disparaît après « delaiDisparition » (sauf si on la tient en main).
/// - Le joueur l'attrape (XR Grab Interactable) et la lâche dans un Panier : c'est le panier qui donne l'argent.
///
/// Branchement XR (dans l'Inspector du XR Grab Interactable de la banane) :
///   Select Entered -> Banane.Prise()     Select Exited -> Banane.Lachee()
/// Test sans casque : script TestSouris sur la caméra (clic sur une banane = dans le panier).
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Banane : MonoBehaviour
{
    [Header("Pourriture")]
    public Color couleurFraiche = Color.white;
    public Color couleurPourrie = new Color(0.35f, 0.22f, 0.1f);
    [Tooltip("Secondes avant de disparaître une fois pourrie (si personne ne la tient)")] public float delaiDisparition = 4f;

    [Header("Chute depuis l'arbre")]
    public float dureeChute = 0.6f;
    public float hauteurRebond = 0.25f;

    public int Valeur { get; private set; }
    public bool EstPourrie { get; private set; }
    public bool EnMain { get; private set; }
    public bool Deposee => deposee;
    public bool Posee => posee;      // arrivée au sol (fin de la chute depuis l'arbre)
    /// 0 = fraîche, 1 = pourrie
    public float Progression => dureePourriture <= 0 ? 0 : Mathf.Clamp01(age / dureePourriture);
    /// Ce que la banane rapporte si on la dépose maintenant.
    public int ValeurActuelle => EstPourrie ? 0 : Valeur;

    Bananier source;
    float dureePourriture, age, ageDisparition;
    bool posee, deposee, lachee;
    Rigidbody rb;
    Renderer[] rends;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;          // posée au sol, elle ne bouge pas
        rb.useGravity = false;
        if (!GetComponent<SAE.GrabReach>()) gameObject.AddComponent<SAE.GrabReach>();   // attrapable de loin, jusqu'à 6 m (GrabReach.Reach)
        rends = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void Initialiser(Bananier bananier, int valeur, float pourriture, Vector3 cible)
    {
        source = bananier; Valeur = valeur; dureePourriture = pourriture;
        StartCoroutine(Tomber(transform.position, cible));
    }

    IEnumerator Tomber(Vector3 de, Vector3 a)
    {
        float t = 0;
        while (t < dureeChute && !EnMain)
        {
            float k = t / dureeChute;
            Vector3 p = Vector3.Lerp(de, a, k);
            p.y = Mathf.Lerp(de.y, a.y, k * k) + Mathf.Sin(k * Mathf.PI) * hauteurRebond;
            transform.position = p;
            t += Time.deltaTime;
            yield return null;
        }
        if (!EnMain) transform.position = a;
        posee = true;
    }

    void Update()
    {
        if (deposee || !posee) return;
        age += Time.deltaTime;
        AppliquerCouleur(Color.Lerp(couleurFraiche, couleurPourrie, Progression));
        if (!EstPourrie && age >= dureePourriture) EstPourrie = true;
        if (EstPourrie && !EnMain)
        {
            ageDisparition += Time.deltaTime;
            if (ageDisparition >= delaiDisparition) Detruire();
        }
    }

    void AppliquerCouleur(Color c)
    {
        foreach (var r in rends)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }

    // ---------- appelé par le XR Grab Interactable ----------
    public void Prise()
    {
        EnMain = true; posee = true; lachee = false;
        StopAllCoroutines();
    }

    public void Lachee()
    {
        EnMain = false;
        lachee = true;
        rb.isKinematic = false;         // elle retombe (dans le panier ou par terre)
        rb.useGravity = true;
    }

    // le XR Grab Interactable peut remettre le Rigidbody en kinematic juste après le lâcher : on corrige
    void FixedUpdate()
    {
        if (lachee && !EnMain && rb.isKinematic) { rb.isKinematic = false; rb.useGravity = true; }
    }

    /// Appelé par le Panier. Renvoie ce que la banane rapporte (0 si pourrie).
    public int Deposer()
    {
        if (deposee) return 0;
        deposee = true;
        int gain = ValeurActuelle;
        Detruire();
        return gain;
    }

    void Detruire()
    {
        if (source != null) source.Retirer(this);
        Destroy(gameObject);
    }
}
