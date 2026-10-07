using UnityEngine;

namespace SAE
{
    // La traînée d'un objet lancé (banane, singe) : un trait doré qui le suit tant qu'il vole vite,
    // pour voir la trajectoire de son lancer (idée de Maxens). Elle s'arrête quand l'objet ralentit ou se pose.
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowTrail : MonoBehaviour
    {
        const float MinSpeed = 1.5f;      // en m/s : en dessous, l'objet tombe ou roule, ce n'est plus un lancer

        Rigidbody body;
        TrailRenderer trail;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = Visuals.LineMaterial;
            trail.time = 0.35f;
            trail.widthMultiplier = 0.04f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.startColor = new Color(1f, 0.85f, 0.3f, 0.8f);
            trail.endColor = new Color(1f, 0.6f, 0.1f, 0f);
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
        }

        void Update()
        {
            bool flying = !body.isKinematic && body.linearVelocity.sqrMagnitude > MinSpeed * MinSpeed;
            if (flying && !trail.emitting) trail.Clear();   // pas de trait qui relie l'ancien lancer au nouveau
            trail.emitting = flying;
        }
    }
}
