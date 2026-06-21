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
            if (menuClip == null) menuClip = MusicLoop("menu", new[] { 523f, 659f, 784f, 659f, 587f, 494f, 440f, 392f }, 131f);
            gameClip = Resources.Load<AudioClip>("Audio/Music/game");
            if (gameClip == null) gameClip = MusicLoop("game", new[] { 392f, 330f, 294f, 247f, 262f, 294f, 330f, 392f }, 110f);
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

        // Melodia em loop (8 notas de 0.5s = 4s): cada nota é um "pluck" (ataque rápido +
        // decaimento) sobre um baixo contínuo. Loop perfeito: as notas terminam ~0 e o
        // baixo completa ciclos inteiros em 4s. Soa como música, não como zumbido.
        private AudioClip MusicLoop(string name, float[] notes, float bass)
        {
            const float noteDur = 0.5f;
            int per = (int)(Rate * noteDur);
            int n = per * notes.Length;
            var data = new float[n];

            // Baixo suave e contínuo.
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * bass * t) * 0.10f;
            }

            // Melodia dedilhada por cima.
            for (int k = 0; k < notes.Length; k++)
            {
                float phase = 0f;
                for (int i = 0; i < per; i++)
                {
                    float prog = (float)i / per;
                    phase += 2f * Mathf.PI * notes[k] / Rate;
                    float attack = prog < 0.02f ? prog / 0.02f : 1f;
                    float env = Mathf.Exp(-prog * 4.5f) * attack;
                    data[k * per + i] += Mathf.Sin(phase) * env * 0.22f;
                }
            }

            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
