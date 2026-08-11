using UnityEngine;

namespace IceReversi.Unity
{
    public sealed class AudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip placeClip;
        [SerializeField] private AudioClip flipClip;
        [SerializeField] private AudioClip actionClip;
        [SerializeField] private AudioClip gameOverClip;

        public void Configure(
            AudioSource audioSource,
            AudioClip place,
            AudioClip flip,
            AudioClip action,
            AudioClip gameOver)
        {
            source = audioSource;
            placeClip = place;
            flipClip = flip;
            actionClip = action;
            gameOverClip = gameOver;
        }

        public void PlayMove(int flippedCount)
        {
            Play(placeClip);
            if (flippedCount > 0)
            {
                Play(flipClip);
            }
        }

        public void PlayPlacement() => Play(placeClip);
        public void PlayFlip() => Play(flipClip);

        public void PlayAction() => Play(actionClip);
        public void PlayGameOver() => Play(gameOverClip);

        private void Play(AudioClip clip)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
            }
        }
    }
}
