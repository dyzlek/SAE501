using UnityEngine;

namespace SAE
{
    // La grande porte de la cabane (le battant du modèle Blender, « Porte_Battant ») : elle s'ouvre toute seule
    // quand le joueur s'en approche ou quand une banane tombe du bananier (qui est dehors, derrière elle),
    // puis se referme quelques secondes après. Elle tourne autour de sa charnière (cet objet est la charnière).
    // openAngle : de combien tourner pour passer de fermée à ouverte (le signe dépend du sens du modèle, voir le générateur).
    public class DoorSwing : MonoBehaviour
    {
        public float openAngle = -100f;     // en degrés, autour de la verticale
        public Transform doorway;           // le milieu de l'embrasure (pour la distance du joueur)
        public Bananier bananier;           // une banane qui tombe ouvre la porte
        public float openDistance = 2.4f;   // en mètres, du joueur au milieu de la porte
        public float stayOpen = 3f;         // en secondes après la dernière raison de l'ouvrir
        public float speed = 1.5f;          // ouvertures par seconde (1 = s'ouvre en une seconde)

        Quaternion closed;
        float openness;                     // 0 = fermée, 1 = ouverte
        float openUntil;
        int bananasSeen;

        void Start()
        {
            // Le modèle est fait porte ouverte : on part de la position fermée
            closed = transform.localRotation * Quaternion.Euler(0f, -openAngle, 0f);
            openness = 1f;
            openUntil = Time.time + stayOpen;
        }

        void Update()
        {
            if (PlayerNear() || BananaArriving()) openUntil = Time.time + stayOpen;
            float target = Time.time < openUntil ? 1f : 0f;
            if (!Mathf.Approximately(openness, target))
            {
                if (openness == 0f || openness == 1f) Sfx.Play(Sfx.Sound.Whoosh, doorway ? doorway.position : transform.position, 0.4f, 0.6f);   // le grincement… en souffle
                openness = Mathf.MoveTowards(openness, target, speed * Time.deltaTime);
            }
            float eased = openness * openness * (3f - 2f * openness);   // démarre et s'arrête en douceur
            transform.localRotation = closed * Quaternion.Euler(0f, openAngle * eased, 0f);
        }

        bool PlayerNear()
        {
            var player = PlayerRig.Local;
            if (!player || !doorway) return false;
            var flat = player.head.position - doorway.position;
            flat.y = 0f;
            return flat.sqrMagnitude < openDistance * openDistance;
        }

        // Une nouvelle banane vient de tomber de l'arbre : elle passe par la porte pour rejoindre l'étal
        bool BananaArriving()
        {
            if (!bananier) return false;
            int count = bananier.BananesAuSol.Count;
            bool arriving = count > bananasSeen;
            bananasSeen = count;
            return arriving;
        }
    }
}
