using System.Collections;
using UnityEngine;

/// Une banane tombée du bananier.
/// - Elle tombe au pied de l'arbre et attend que le joueur la ramasse.
/// - Elle brunit petit à petit ; au bout de « dureePourriture » secondes elle est pourrie :
///   elle ne vaut plus rien et disparaît après « delaiDisparition » (sauf si on la tient en main).
/// - Le joueur l'attrape (XR Grab Interactable) et la lâche ou la LANCE dans un Panier : c'est le panier qui donne l'argent.
///   Au lâcher, elle part avec la vitesse de la main (ThrowVelocity).
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
    bool elanADonner;               // lâchée : il faut lui donner l'élan de la main (voir FixedUpdate)
    Vector3 elan;
    Rigidbody rb;
    SAE.ThrowVelocity vitesseMain;
    Renderer[] rends;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    Vector3 tailleFraiche;          // sa taille d'origine (elle se ratatine en pourrissant)

    void Awake()
    {
        tailleFraiche = transform.localScale;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;          // posée au sol, elle ne bouge pas
        rb.useGravity = false;
        if (!GetComponent<SAE.GrabReach>()) gameObject.AddComponent<SAE.GrabReach>();   // attrapable de loin, jusqu'à 6 m (GrabReach.Reach)
        vitesseMain = gameObject.AddComponent<SAE.ThrowVelocity>();   // pour la lancer
        gameObject.AddComponent<SAE.ThrowTrail>();                     // et la traînée dorée quand elle vole
        rends = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        SAE.PlayerRig.IgnoreCollisions(gameObject);   // on ne peut pas monter sur une banane tenue et s'envoler
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
        if (!EstPourrie && age >= dureePourriture) { EstPourrie = true; Odeur(); }
        AnimerPourriture();
        if (EstPourrie && !EnMain)
        {
            ageDisparition += Time.deltaTime;
            if (ageDisparition >= delaiDisparition) Detruire();
        }
    }

    // ---------- l'animation de la pourriture ----------
    // En pourrissant, la banane se ratatine (jusqu'à 80 % de sa taille). Pourrie, elle dégage une petite odeur verdâtre
    // qui monte (Odeur), tremblote, puis se dégonfle et disparaît pendant ses dernières secondes au lieu de s'effacer d'un coup.
    const float TailleFinale = 0.8f;      // taille d'une banane tout juste pourrie (part de la taille fraîche)
    const float Degonflement = 0.6f;      // en secondes : la fin, où elle rétrécit jusqu'à rien

    void AnimerPourriture()
    {
        float taille = Mathf.Lerp(1f, TailleFinale, Progression);
        if (EstPourrie && !EnMain)
        {
            float reste = delaiDisparition - ageDisparition;
            if (reste < Degonflement) taille *= Mathf.Clamp01(reste / Degonflement);   // elle se dégonfle
            else transform.Rotate(0f, Mathf.Sin(Time.time * 12f) * 40f * Time.deltaTime, 0f, Space.World);   // elle tremblote
        }
        transform.localScale = tailleFraiche * taille;
    }

    // De petites bulles d'odeur verdâtres qui montent doucement au-dessus de la banane pourrie (elles suivent la banane)
    void Odeur()
    {
        var go = new GameObject("Odeur");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.15f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(0.55f, 0.65f, 0.2f, 0.6f);
        main.gravityModifier = -0.05f;    // elles montent
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 4f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = SAE.Visuals.LineMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    void AppliquerCouleur(Color c)
    {
        // Créés ici au besoin, si Awake ne les a pas encore créés
        mpb ??= new MaterialPropertyBlock();
        rends ??= GetComponentsInChildren<Renderer>();
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
        vitesseMain.Clear();
        StopAllCoroutines();
    }

    public void Lachee()
    {
        EnMain = false;
        lachee = true;
        elan = vitesseMain.Velocity;    // la vitesse de la main au moment du lâcher : on peut la lancer dans le panier
        elanADonner = true;
        rb.isKinematic = false;         // elle retombe (dans le panier ou par terre)
        rb.useGravity = true;
    }

    // le XR Grab Interactable peut remettre le Rigidbody en kinematic juste après le lâcher : on corrige,
    // puis on lui donne l'élan de la main (une seule fois)
    void FixedUpdate()
    {
        if (lachee && !EnMain && rb.isKinematic) { rb.isKinematic = false; rb.useGravity = true; }
        if (elanADonner && !rb.isKinematic)
        {
            rb.linearVelocity = elan;
            elanADonner = false;
        }
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
