using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// Panier à bananes : quand une banane tombe dedans, elle est comptée et disparaît.
/// Le panier garde le total gagné (« Total »). Plus tard, brancher l'événement « onDepot »
/// (ou OnDepotCode) sur le vrai système d'argent du jeu.
///
/// Mise en place : sur l'objet Zone_Depot du modèle Panier.fbx (ou un enfant vide),
/// ajouter un Box Collider en « Is Trigger » qui remplit l'intérieur du panier, puis ce script.
[RequireComponent(typeof(Collider))]
public class Panier : MonoBehaviour
{
    [Serializable] public class DepotEvent : UnityEvent<int, int> { }   // (gain, total)

    [Tooltip("Appelé à chaque banane déposée : (gain de cette banane, total du panier)")]
    public DepotEvent onDepot = new DepotEvent();
    /// Même chose pour le code : (gain, total)
    public event Action<int, int> OnDepotCode;

    [Header("Effets (optionnels)")]
    public GameObject effetDepot;
    public AudioClip sonDepot;
    public AudioClip sonPourrie;

    public int Total { get; private set; }
    public int NbBananes { get; private set; }

    static readonly List<Panier> tous = new List<Panier>();

    void OnEnable() { tous.Add(this); }
    void OnDisable() { tous.Remove(this); }

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        var b = other.GetComponentInParent<Banane>();
        if (b != null && !b.EnMain) Recevoir(b);
    }

    // une banane lâchée pile dedans ne déclenche pas toujours OnTriggerEnter (déjà à l'intérieur)
    void OnTriggerStay(Collider other)
    {
        var b = other.GetComponentInParent<Banane>();
        if (b != null && !b.EnMain) Recevoir(b);
    }

    /// Ajoute la banane au panier et renvoie ce qu'elle a rapporté.
    public int Recevoir(Banane b) { return RecevoirPart(b, 1f); }

    /// Version utilisée par le récolteur : seule une part de la valeur est comptée.
    public int RecevoirPart(Banane b, float part)
    {
        if (b == null || b.Deposee) return 0;
        bool pourrie = b.EstPourrie;
        int gain = Mathf.RoundToInt(b.Deposer() * part);
        Total += gain; NbBananes++;
        SAE.Economy.Earn(gain, transform.position);   // l'argent du jeu (une seule bourse)
        if (effetDepot != null) Instantiate(effetDepot, transform.position, Quaternion.identity);
        var son = pourrie ? sonPourrie : sonDepot;
        if (son != null) AudioSource.PlayClipAtPoint(son, transform.position);
        onDepot.Invoke(gain, Total);
        OnDepotCode?.Invoke(gain, Total);
        return gain;
    }

    /// Vide le panier et renvoie ce qu'il contenait (pour plus tard : « encaisser »).
    public int Vider()
    {
        int t = Total; Total = 0; NbBananes = 0;
        return t;
    }

    public static Panier LePlusProche(Vector3 pos)
    {
        Panier best = null; float d = float.MaxValue;
        foreach (var p in tous)
        {
            float dd = (p.transform.position - pos).sqrMagnitude;
            if (dd < d) { d = dd; best = p; }
        }
        return best;
    }
}
