using UnityEngine;

namespace SAE
{
    // Empêche les bananes de rester coincées dans le bananier (sous les feuilles, dans la terre du bac) :
    // toute banane posée ou lâchée trop près du tronc est remise au sol, juste à l'extérieur du bac,
    // là où le joueur peut la prendre. À mettre sur le même objet que le Bananier.
    [RequireComponent(typeof(Bananier))]
    public class BananaGuard : MonoBehaviour
    {
        public float margin = 0.1f;      // en plus du bord du bac (Bananier.distanceMin)
        public float groundY = 0.1f;     // hauteur d'une banane posée au sol

        Bananier bananier;

        void Awake() => bananier = GetComponent<Bananier>();

        void LateUpdate()
        {
            float radius = bananier.distanceMin + margin;
            var center = transform.position;

            foreach (var b in FindObjectsByType<Banane>())
            {
                if (b.EnMain || b.Deposee) continue;
                // on attend qu'elle soit immobile : finie sa chute depuis l'arbre, ou retombée après un lâcher
                var body = b.GetComponent<Rigidbody>();
                bool resting = body == null || (body.isKinematic ? b.Posee : body.linearVelocity.sqrMagnitude < 0.05f);
                if (!resting) continue;
                var flat = b.transform.position - center;
                flat.y = 0f;
                if (flat.sqrMagnitude >= radius * radius) continue;

                // Dehors, dans la même direction (ou vers le joueur si elle est pile au centre)
                var dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : -transform.forward;
                var pos = center + dir * radius;
                pos.y = groundY;

                var rb = b.GetComponent<Rigidbody>();
                if (rb && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.position = pos;
                }
                b.transform.position = pos;
            }
        }
    }
}
