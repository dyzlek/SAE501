using Unity.Netcode;
using UnityEngine;

namespace SAE
{
    // Le double réseau d'un ballon (NetworkObject). Le vrai ballon (Balloon) ne change pas : son double le suit.
    //   - Chez l'hôte, il recopie la position du ballon (le NetworkTransform du prefab l'envoie aux invités, qui glissent
    //     en douceur d'une position à l'autre), sa sorte et ses couches (NetworkVariable).
    //   - Chez l'invité, NGO le fait apparaître ; il y crée une copie du ballon (Balloon.Remote), posée sur lui :
    //     elle bouge avec lui. Un tir sur la copie est demandé à l'hôte (Coop.SendHit), qui perce le vrai ballon.
    //   - Le vrai ballon éclate ou sort de la piste : l'hôte le détruit, et son double avec (Balloon.OnDestroy).
    //     NGO le fait alors disparaître chez tout le monde, avec la copie.
    // Si l'hôte ferme le coop, les doubles disparaissent mais les vrais ballons restent : sa partie continue.
    public class CoopBalloon : NetworkBehaviour
    {
        readonly NetworkVariable<BalloonKind> kind = new NetworkVariable<BalloonKind>();
        readonly NetworkVariable<int> layers = new NetworkVariable<int>();
        readonly NetworkVariable<bool> camo = new NetworkVariable<bool>();

        // Hôte : le vrai ballon. Invité : sa copie.
        public Balloon Target { get; private set; }

        // Le nombre de doubles en jeu chez nous : écrit sur le panneau COOP (« 12 ballons partagés / reçus »)
        public static int Count { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCount() => Count = 0;

        // Hôte, avant Spawn : les valeurs de départ partent avec l'objet
        public void Follow(Balloon balloon)
        {
            Target = balloon;
            balloon.Proxy = this;
            kind.Value = balloon.Kind;
            layers.Value = balloon.Layers;
            camo.Value = balloon.Camo;
            transform.SetPositionAndRotation(balloon.transform.position, balloon.transform.rotation);
        }

        public override void OnNetworkSpawn()
        {
            DontDestroyOnLoad(gameObject);
            Count++;
        }

        public override void OnNetworkDespawn()
        {
            Count--;
            if (!Target) return;
            // On regarde le ballon, pas IsServer : quand l'hôte ferme le coop, NGO n'est déjà plus « serveur » ici
            if (Target.Remote) Destroy(Target.gameObject);   // invité : la copie part avec son double
            else Target.Proxy = null;                        // hôte : le vrai ballon continue sans double
        }

        void LateUpdate()
        {
            if (!IsSpawned) return;
            if (IsServer)
            {
                if (!Target) return;   // détruit dans cette image : NGO nous efface (Balloon.OnDestroy)
                transform.SetPositionAndRotation(Target.transform.position, Target.transform.rotation);
                layers.Value = Target.Layers;
                return;
            }

            // Invité : la copie, dès que la carte est chargée chez nous
            if (!Target)
            {
                var spawner = WaveSpawner.Instance;
                if (spawner) Target = spawner.SpawnRemote(kind.Value, layers.Value, this);
                if (Target && camo.Value) Target.MakeCamo();
                return;
            }
            Target.ApplyHostLayers(layers.Value);
        }
    }
}
