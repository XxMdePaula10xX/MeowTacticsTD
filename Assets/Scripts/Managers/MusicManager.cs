using UnityEngine;

namespace MeowTactics.Managers
{
    public enum MusicTrack { None, Menu, Game }

    /// <summary>
    /// Música de fundo. Gera trilhas ambiente simples EM CÓDIGO (pads suaves em
    /// loop perfeito), então já há música sem precisar baixar nada. Para usar
    /// música real, coloque "Resources/Audio/Music/menu" e ".../game" (.ogg/.wav).
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Range(0f, 1f)] public float volume = 0.45f;

        private AudioSource src;
        private AudioClip menuClip, gameClip;
        private MusicTrack current = MusicTrack.None;
        private const int Rate = 44100;

        private void Awake()
        {
            Instance = this;
            volume = PlayerPrefs.GetFloat("musicVol", 0.45f);

            src = gameObject.AddComponent<AudioSource>();
            src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f;

            menuClip = Resources.Load<AudioClip>("Audio/Music/menu");
            if (menuClip == null) menuClip = Pad("menu", new[] { 130f, 195f, 260f, 390f }); // acorde mais claro
            gameClip = Resources.Load<AudioClip>("Audio/Music/game");
            if (gameClip == null) gameClip = Pad("game", new[] { 110f, 165f, 220f }); // acorde grave/calmo
        }

        public static void Play(MusicTrack t) { if (Instance != null) Instance.PlayInternal(t); }

        private void PlayInternal(MusicTrack t)
        {
            if (t == current) return;
            current = t;
            AudioClip clip = t == MusicTrack.Menu ? menuClip : (t == MusicTrack.Game ? gameClip : null);
            src.clip = clip;
            src.volume = volume;
            if (clip != null) src.Play(); else src.Stop();
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            if (src != null) src.volume = volume;
            PlayerPrefs.SetFloat("musicVol", volume);
        }

        // Pad ambiente em loop perfeito: 4s, frequências harmônicas (ciclos inteiros),
        // tremolo de 0.25Hz (1 ciclo em 4s) — não dá "clique" ao repetir.
        private AudioClip Pad(string name, float[] freqs)
        {
            const float dur = 4f;
            int n = (int)(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float s = 0f;
                foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
                s /= freqs.Length;
                float tremolo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                data[i] = s * tremolo * 0.28f;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
