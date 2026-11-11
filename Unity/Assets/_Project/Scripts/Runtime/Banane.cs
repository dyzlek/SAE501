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
/// Banane dorée (rare, voir Bananier.chanceDoree) : toute dorée et brillante, avec une aura légère ; elle vaut beaucoup plus et pourrit moins vite.
/// Coop (voir SAE.CoopOrchard) : chaque banane a un numéro. Une banane « distante » est menée par une autre machine :
/// chez l'invité, la copie d'une banane de l'hôte ; chez l'hôte, une banane qu'un invité tient. Elle glisse vers la position
/// reçue, ne pourrit pas toute seule et le panier ne la compte pas : c'est celui qui la mène qui décide.
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
    public bool EstDoree { get; private set; }
    /// Tenue par un joueur (pas par un singe) : en coop, l'hôte dit ainsi à tous qui la tient
    public bool PriseParJoueur { get; private set; }

    // ---------- coop ----------
    public int Id { get; private set; }               // son numéro, le même chez tout le monde
    public bool Copie { get; private set; }           // invité : la copie d'une banane de l'hôte
    public bool Distante { get; private set; }        // menée par une autre machine (voir plus haut)
    public byte TenuePar { get; private set; } = SAE.CoopOrchard.Nobody;   // hôte : l'invité qui la tient
    public Rigidbody Corps => rb;
    static int dernierId;
    Vector3 ciblePos;
    Quaternion cibleRot;
    Collider[] colliders;
    /// La dernière main du joueur qui l'a tenue (pour la faire vibrer au dépôt). Vide si c'est un singe qui l'a reprise.
    public Transform DerniereMain { get; private set; }
    public bool Deposee => deposee;
    public bool Posee => posee;      // arrivée au sol (fin de la chute depuis l'arbre)
    /// 0 = fraîche, 1 = pourrie
    public float Progression => dureePourriture <= 0 ? 0 : Mathf.Clamp01(age / dureePourriture);
    /// Ce que la banane rapporte si on la dépose maintenant.
    public int ValeurActuelle => EstPourrie ? 0 : Valeur;

    Bananier source;
    public Bananier Source => source;   // l'arbre d'où elle est tombée (son étal est source.versCible)
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
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    static readonly Color Doree = new Color(1f, 0.68f, 0.05f);

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
        colliders = GetComponentsInChildren<Collider>();
        mpb = new MaterialPropertyBlock();
        SAE.PlayerRig.IgnoreCollisions(gameObject);   // on ne peut pas monter sur une banane tenue et s'envoler
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    // La main qui l'attrape vibre doucement, et on la retient pour la faire vibrer au dépôt dans le panier
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    void OnEnable() { if (grab) grab.selectEntered.AddListener(AttrapeeParLeJoueur); }
    void OnDisable() { if (grab) grab.selectEntered.RemoveListener(AttrapeeParLeJoueur); }

    void AttrapeeParLeJoueur(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        DerniereMain = args.interactorObject.transform;
        SAE.Haptics.Play(DerniereMain, SAE.Haptics.Grab);
    }

    public void Initialiser(Bananier bananier, int valeur, float pourriture, Vector3 cible)
    {
        source = bananier; Valeur = valeur; dureePourriture = pourriture;
        Id = ++dernierId;
        StartCoroutine(Tomber(transform.position, cible));
    }

    /// Invité : la copie de la banane « id » de l'hôte, déjà là où elle est chez lui
    public void InitialiserCopie(Bananier bananier, int id, int valeur, float pourriture)
    {
        source = bananier; Valeur = valeur; dureePourriture = pourriture;
        Id = id;
        Copie = Distante = true;
        posee = true;
        ciblePos = transform.position;
        cibleRot = transform.rotation;
    }

    /// Fait de cette banane une banane dorée : sa valeur est multipliée, elle pourrit 'pourritureX' fois moins vite.
    public void RendreDoree(int multiplicateur, float pourritureX)
    {
        if (EstDoree) return;
        EstDoree = true;
        Valeur *= multiplicateur;
        dureePourriture *= pourritureX;
        couleurFraiche = Doree;
        AppliquerCouleur(couleurFraiche);
        // Une aura légère (celle des raretés des singes), à la taille de la banane seule
        var corps = GetComponent<MeshRenderer>();
        if (corps) SAE.Aura.Add(gameObject, new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f, 0.35f)), corps.bounds.size);
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
        if (deposee) return;
        if (Distante) { SuivreCible(); return; }
        if (!posee) return;
        if (!EnMain) age += Time.deltaTime;   // tenue (par le joueur ou un singe), elle ne pourrit pas ; lâchée, ça reprend
        AppliquerCouleur(Color.Lerp(couleurFraiche, couleurPourrie, Progression));
        if (!EstPourrie && age >= dureePourriture) { EstPourrie = true; Odeur(); }
        AnimerPourriture();
        if (EstPourrie && !EnMain)
        {
            ageDisparition += Time.deltaTime;
            if (ageDisparition >= delaiDisparition) Detruire();
        }
    }

    // Distante : elle glisse vers la dernière position reçue (20 à 30 par seconde) pour que ça reste fluide,
    // et prend la couleur et la taille de sa pourriture, sans jamais disparaître d'elle-même
    void SuivreCible()
    {
        float t = 1f - Mathf.Exp(-15f * Time.deltaTime);
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, ciblePos, t), Quaternion.Slerp(transform.rotation, cibleRot, t));
        if (!EstPourrie && Progression >= 1f) { EstPourrie = true; Odeur(); }
        AppliquerCouleur(Color.Lerp(couleurFraiche, couleurPourrie, Progression));
        transform.localScale = tailleFraiche * Mathf.Lerp(1f, TailleFinale, Progression);
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
            if (EstDoree)
            {
                // Dorée : la texture jaune est remplacée par du blanc teinté en or, en métal brillant
                mpb.SetTexture(BaseMapId, Texture2D.whiteTexture);
                mpb.SetFloat(MetallicId, 0.5f);
                mpb.SetFloat(SmoothnessId, 0.85f);
            }
            r.SetPropertyBlock(mpb);
        }
    }

    // ---------- appelé par le XR Grab Interactable ----------
    public void Prise()
    {
        PrendreEnMain();
        PriseParJoueur = true;
        Distante = false;                 // invité : c'est lui qui la mène, tant qu'il la tient
        SAE.CoopOrchard.Taken(this);      // et il la demande à l'hôte
    }

    void PrendreEnMain()
    {
        EnMain = true; posee = true; lachee = false;
        vitesseMain.Clear();
        StopAllCoroutines();
    }

    /// Un singe récolteur la prend : comme Prise(), mais ce n'est plus le joueur qui la déposera.
    public void PriseParSinge()
    {
        DerniereMain = null;
        PrendreEnMain();
        PriseParJoueur = false;
    }

    /// Un singe récolteur l'a laissée par terre (lancer raté) : elle n'est plus tenue, elle se remet à pourrir.
    public void PoseeParTerre() => EnMain = false;

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
        if (Distante) { if (!rb.isKinematic) rb.isKinematic = true; return; }   // c'est l'autre machine qui la fait bouger
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
        SAE.CoopOrchard.Sold(this);   // coop : les autres l'entendent, l'hôte la retire pour tous
        Detruire();
        return gain;
    }

    /// Retirée sans passer par le panier (coop : vendue ou pourrie chez une autre machine)
    public void Effacer() => Detruire();

    // ---------- coop : l'hôte (voir SAE.CoopOrchard) ----------

    /// Hôte : un invité l'a prise. Elle ne pourrit plus, les singes la laissent, et elle suit la main de l'invité.
    public void PriseParInvite(byte joueur)
    {
        PrendreEnMain();
        PriseParJoueur = false;
        TenuePar = joueur;
        Distante = true;
        ciblePos = transform.position;
        cibleRot = transform.rotation;
        rb.isKinematic = true;
        Attrapable(false);   // l'hôte ne peut pas la lui prendre des mains
    }

    public void SuivreInvite(Vector3 pos, Quaternion rot) { ciblePos = pos; cibleRot = rot; }

    /// Hôte : l'invité l'a posée (lancer raté, lâchée par terre) : elle reprend sa vie ici, là où elle est chez lui
    public void PoseeParInvite(Vector3 pos, Quaternion rot)
    {
        FinPriseInvite();
        transform.SetPositionAndRotation(pos, rot);
        EnMain = false; lachee = false; elanADonner = false;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// Hôte : plus aucun invité ne la tient (posée, ou l'invité est parti)
    public void FinPriseInvite()
    {
        TenuePar = SAE.CoopOrchard.Nobody;
        Distante = false;
        Attrapable(true);
    }

    // ---------- coop : l'invité ----------

    /// Invité : la position que l'hôte lui donne, sa pourriture (0 à 1), et si on peut la prendre (personne ne la tient)
    public void SuivreHote(Vector3 pos, Quaternion rot, float progression, bool libre)
    {
        ciblePos = pos;
        cibleRot = rot;
        age = progression * dureePourriture;
        Attrapable(libre);
    }

    /// Invité : posée après son lancer, elle est rendue à l'hôte
    public void RendreDistante()
    {
        Distante = true;
        EnMain = false; lachee = false; elanADonner = false;
        ciblePos = transform.position;
        cibleRot = transform.rotation;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// Invité : l'hôte l'a donnée à un autre joueur (il l'avait prise juste avant) : on la lâche
    public void Reprendre()
    {
        RendreDistante();
        // En VR, on la retire de la main ; XRI appelle alors Lachee, qui la remet en physique : on refait RendreDistante
        if (grab && grab.isSelected && grab.interactionManager)
            grab.interactionManager.CancelInteractableSelection(
                (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        RendreDistante();
    }

    void Attrapable(bool oui)
    {
        colliders ??= GetComponentsInChildren<Collider>();
        foreach (var c in colliders) if (c && c.enabled != oui) c.enabled = oui;
    }

    void Detruire()
    {
        if (source != null) source.Retirer(this);
        Destroy(gameObject);
    }
}
