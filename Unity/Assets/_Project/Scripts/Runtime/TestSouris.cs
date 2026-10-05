using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Test sans casque : à mettre sur la caméra. Simule la main VR à la souris.
/// - Clic maintenu sur une banane = on la prend en main (elle suit le curseur, au-dessus du décor).
/// - On la déplace au-dessus du panier et on relâche = elle tombe dedans (même chemin qu'en VR : Prise() / Lachee()).
/// Marche avec le nouvel Input System (Unity 6) comme avec l'ancien.
public class TestSouris : MonoBehaviour
{
    public float distanceMax = 50f;
    [Tooltip("Hauteur de la banane au-dessus de ce que vise la souris pendant qu'on la tient")]
    public float hauteurEnMain = 0.5f;
    [Tooltip("Vitesse à laquelle la banane rejoint le curseur")]
    public float suivi = 20f;

    Camera cam;
    Banane tenue;
    int[] calquesOrigine;
    Transform[] parties;
    const int CalqueIgnoreRaycast = 2;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;
        Vector2 pos = PositionSouris();

        if (tenue == null)
        {
            if (Appuye()) Prendre(pos);
            return;
        }

        // la banane a pu disparaître (pourrie, récoltée...)
        if (tenue == null || !tenue) { tenue = null; return; }

        // la banane suit le curseur, posée au-dessus de ce qu'on vise
        Ray ray = cam.ScreenPointToRay(pos);
        Vector3 cible;
        if (Physics.Raycast(ray, out RaycastHit hit, distanceMax, ~(1 << CalqueIgnoreRaycast), QueryTriggerInteraction.Ignore))
            cible = hit.point + Vector3.up * hauteurEnMain;
        else
            cible = ray.GetPoint(2f);
        tenue.transform.position = Vector3.Lerp(tenue.transform.position, cible, 1f - Mathf.Exp(-suivi * Time.deltaTime));

        if (Relache()) Lacher();
    }

    void Prendre(Vector2 pos)
    {
        // on ignore les triggers (sinon la zone du panier bloque le clic)
        if (!Physics.Raycast(cam.ScreenPointToRay(pos), out RaycastHit hit, distanceMax, ~0, QueryTriggerInteraction.Ignore)) return;
        var b = hit.collider.GetComponentInParent<Banane>();
        if (b == null || b.Deposee) return;

        tenue = b;
        tenue.Prise();
        // la banane tenue ne doit pas bloquer le rayon de la souris
        parties = tenue.GetComponentsInChildren<Transform>();
        calquesOrigine = new int[parties.Length];
        for (int i = 0; i < parties.Length; i++) { calquesOrigine[i] = parties[i].gameObject.layer; parties[i].gameObject.layer = CalqueIgnoreRaycast; }
    }

    void Lacher()
    {
        if (tenue != null)
        {
            for (int i = 0; i < parties.Length; i++) if (parties[i] != null) parties[i].gameObject.layer = calquesOrigine[i];
            tenue.Lachee();   // elle retombe : dans le panier si on est au-dessus, sinon par terre
        }
        tenue = null;
    }

    void OnDisable() { Lacher(); }

    // ---------- entrées souris (nouvel et ancien Input System) ----------
    static Vector2 PositionSouris()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    static bool Appuye()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }

    static bool Relache()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonUp(0)) return true;
#endif
        return false;
    }
}
