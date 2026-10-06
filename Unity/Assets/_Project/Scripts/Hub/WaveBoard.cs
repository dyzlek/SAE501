using UnityEngine;

namespace SAE
{
    // Tableau dans le décor (hub et carte) : vague, vies et état de la vague.
    // Remplace l'affichage collé à l'écran, qui donne la nausée en VR.
    public class WaveBoard : MonoBehaviour
    {
        public WaveSpawner spawner;
        public TextMesh text;

        void Update()
        {
            if (!spawner || !text) return;
            string lives = spawner.Lives <= 5 ? $"<color=#FF5555>{spawner.Lives}</color>" : spawner.Lives.ToString();
            string boss = spawner.BossCount > 1 ? $"{spawner.BossCount} boss" : "1 boss";
            text.text = $"<b>VAGUE {spawner.Wave}</b>  ({boss})\nVies : {lives}\n<size=34>{spawner.Status}</size>";
        }
    }
}
