using UnityEngine;

namespace SAE
{
    // Fait avancer le tutoriel (Tutorial.Tick) et promène la flèche dorée au-dessus de ce qu'il faut toucher.
    // Un seul, dans la scène Hub (toujours chargée : c'est celle du joueur).
    // La flèche : un chevron doré pointé vers le bas, qui rebondit et tourne doucement.
    // Elle montre la cible la plus proche du joueur dans le niveau où il est (il y a un tableau au hub et un dans la
    // bananeraie), ou, pour le premier singe, sa case dans la bibliothèque (LibrarySlot).
    public class TutorialRunner : MonoBehaviour
    {
        public Transform pointer;          // la flèche (construite par PrototypeGenerator)

        const float BounceHeight = 0.12f, BounceSpeed = 4f, SpinSpeed = 90f;
        const float SlotHeight = 0.3f;     // au-dessus de la case du singe

        void Update()
        {
            Tutorial.Tick();
            if (!pointer) return;

            var position = Vector3.zero;
            bool show = Tutorial.Active && Where(Tutorial.Spot(Tutorial.Current), out position);
            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
            if (!show) return;

            float bounce = Mathf.Abs(Mathf.Sin(Time.time * BounceSpeed)) * BounceHeight;
            pointer.position = position + Vector3.up * bounce;
            pointer.rotation = Quaternion.Euler(0f, Time.time * SpinSpeed, 0f);
        }

        // Où poser la flèche pour cet endroit (false : rien à montrer dans le niveau où est le joueur)
        static bool Where(TutorialSpot spot, out Vector3 position)
        {
            position = default;
            if (spot == TutorialSpot.None) return false;
            if (spot == TutorialSpot.FirstMonkey)
            {
                var slot = LibrarySlot.Find(new Monkey(MonkeyType.Classique, Rarity.Gris));
                if (!slot || Levels.Current != Level.Hub) return false;
                position = slot.transform.position + Vector3.up * SlotHeight;
                return true;
            }
            var target = TutorialTarget.Nearest(spot, Levels.Current, PlayerRig.Local ? PlayerRig.Local.HeadPosition : Vector3.zero);
            if (!target) return false;
            position = target.transform.position + Vector3.up * target.height;
            return true;
        }
    }
}
