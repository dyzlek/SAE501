using System.Collections;
using UnityEngine;
using Sae501.Coffres;

namespace SAE
{
    // Ce que donne le coffre : à l'ouverture, chaque singe gagné sort du coffre avec son aura (Visuals.MonkeyPiece) de la couleur
    // de sa rareté, flotte un instant, puis vole jusqu'à sa case de la bibliothèque, où il s'ajoute à l'inventaire.
    // Le coffre tire la rareté et, grâce à rollType, le TYPE parmi les types débloqués avec des bananes (MonkeyOdds) :
    // les deux sont connus avant la roulette, qui montre ainsi le vrai singe gagné.
    public class ChestReward : MonoBehaviour
    {
        public ChestController chest;
        public MonkeyOddsSettings typeOdds = new MonkeyOddsSettings();
        public float pieceSize = 0.45f;    // taille du singe une fois sorti (il part de 30 % de cette taille), en mètres
        public float riseHeight = 1.0f;
        public float riseTime = 1.0f;
        public float hoverTime = 1f;
        public float flyTime = 1.2f;

        public float[] CurrentTypeOdds() => MonkeyOdds.Compute(typeOdds);

        void OnEnable()
        {
            if (!chest) return;
            chest.Opened += OnOpened;
            chest.rollType = () => (int)MonkeyOdds.Roll(CurrentTypeOdds());   // le coffre tire le type avant la roulette
        }
        void OnDisable() { if (chest) chest.Opened -= OnOpened; }

        void OnOpened(System.Collections.Generic.List<Sae501.Coffres.Rarity> results)
        {
            Sfx.Play(Sfx.Sound.Fanfare, chest.transform.position);
            var typeChances = CurrentTypeOdds();
            for (int i = 0; i < results.Count; i++)
            {
                // le type déjà tiré par le coffre (celui que la roulette a montré), sinon on le tire ici
                var type = i < chest.LastTypes.Count ? (MonkeyType)chest.LastTypes[i] : MonkeyOdds.Roll(typeChances);
                var level = (Rarity)(int)results[i];   // mêmes raretés de Gris à Rouge dans les deux listes
                StartCoroutine(Deliver(new Monkey(type, level), i * 0.4f, i));
            }
        }

        IEnumerator Deliver(Monkey monkey, float delay, int index)
        {
            yield return new WaitForSeconds(delay);

            // Sortie du coffre : le singe part du milieu de la caisse, petit, puis monte en tournant et en grossissant
            var lid = chest.GetComponent<ChestLid>();
            var start = lid && lid.glowAnchor ? lid.glowAnchor.position : chest.transform.position + Vector3.up * 0.4f;
            var top = start + Vector3.up * riseHeight;
            var piece = Visuals.MonkeyPiece(monkey, null, start, pieceSize, withLabel: false);
            piece.name = $"Récompense {monkey}";
            piece.transform.position = start;

            for (float t = 0; t < 1f; t += Time.deltaTime / riseTime)
            {
                float k = 1f - (1f - t) * (1f - t);   // part vite, ralentit en haut
                piece.transform.position = Vector3.Lerp(start, top, k);
                piece.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
                piece.transform.Rotate(0, 540f * (1f - k * 0.6f) * Time.deltaTime, 0);
                yield return null;
            }
            piece.transform.localScale = Vector3.one;
            for (float t = 0; t < hoverTime; t += Time.deltaTime)
            {
                piece.transform.position = top + Vector3.up * 0.05f * Mathf.Sin(t * 6f);
                piece.transform.Rotate(0, 180f * Time.deltaTime, 0);
                yield return null;
            }

            // Vol en arc jusqu'à sa case de la bibliothèque, en rétrécissant
            var slot = LibrarySlot.Find(monkey);
            if (slot)
            {
                var from = piece.transform.position;
                var to = slot.transform.position;
                var scale = piece.transform.localScale;
                for (float t = 0; t < 1f; t += Time.deltaTime / flyTime)
                {
                    float k = t * t * (3f - 2f * t);   // démarre et arrive en douceur
                    piece.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                    piece.transform.localScale = scale * Mathf.Lerp(1f, 0.7f, k);
                    yield return null;
                }
            }

            Destroy(piece);
            GameState.AddToInventory(monkey);
        }
    }
}
