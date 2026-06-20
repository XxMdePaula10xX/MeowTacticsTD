using System.Collections.Generic;
using UnityEngine;

namespace MeowTactics.Managers
{
    public enum SfxType
    {
        Click, Buy, Error, Place, StartWave,
        AttackPhysical, AttackMagic, EnemyDeath, Coin,
        Synergy, Victory, Defeat
    }

    /// <summary>
    /// Gera efeitos sonoros simples EM CÓDIGO (pequenos blips sintetizados),
    /// então o jogo já tem som sem precisar baixar nada. Para usar sons reais
    /// depois, basta colocar arquivos em "Resources/Audio/SFX/&lt;tipo&gt;.wav"
    /// (ex.: Resources/Audio/SFX/Buy.wav) que eles substituem o placeholder.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Range(0f, 1f)] public float volume = 0.55f;

        private AudioSource source;
        private const int Rate = 44100;
        private readonly Dictionary<SfxType, AudioClip> clips = new Dictionary<SfxType, AudioClip>();
        private readonly Dictionary<SfxType, float> lastPlay = new Dictionary<SfxType, float>();

        private enum Wave { Sine, Square }

        private void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            BuildClips();
        }

        /// <summary>Toca um efeito (seguro contra null).</summary>
        public static void Play(SfxType type) { if (Instance != null) Instance.PlayInternal(type); }

        private void PlayInternal(SfxType type)
        {
            float cd = Cooldown(type);
            if (cd > 0f)
            {
                float last;
                if (lastPlay.TryGetValue(type, out last) && Time.unscaledTime - last < cd) return;
                lastPlay[type] = Time.unscaledTime;
            }

            AudioClip clip;
            if (clips.TryGetValue(type, out clip) && clip != null)
                source.PlayOneShot(clip, volume * TypeVolume(type));
        }

        // Eventos que podem disparar muito rápido têm um intervalo mínimo.
        private static float Cooldown(SfxType type)
        {
            switch (type)
            {
                case SfxType.AttackPhysical:
                case SfxType.AttackMagic: return 0.07f;
                case SfxType.Coin:
                case SfxType.EnemyDeath: return 0.04f;
                default: return 0f;
            }
        }

        private static float TypeVolume(SfxType type)
        {
            switch (type)
            {
                case SfxType.AttackPhysical:
                case SfxType.AttackMagic: return 0.35f;
                case SfxType.Coin: return 0.5f;
                case SfxType.EnemyDeath: return 0.6f;
                default: return 1f;
            }
        }

        private void BuildClips()
        {
            // Primeiro tenta carregar sons reais de Resources/Audio/SFX/<Tipo>.
            foreach (SfxType t in System.Enum.GetValues(typeof(SfxType)))
            {
                var loaded = Resources.Load<AudioClip>("Audio/SFX/" + t);
                if (loaded != null) { clips[t] = loaded; }
            }

            // Para os que não têm arquivo, gera um placeholder sintetizado.
            Set(SfxType.Click,          Tone("click", 880, 880, 0.05f, Wave.Square));
            Set(SfxType.Buy,            Arp("buy",  new[] { 660f, 990f }, 0.07f, Wave.Sine));
            Set(SfxType.Error,          Tone("error", 180, 110, 0.18f, Wave.Square));
            Set(SfxType.Place,          Tone("place", 320, 170, 0.10f, Wave.Sine));
            Set(SfxType.StartWave,      Tone("start", 440, 880, 0.25f, Wave.Sine));
            Set(SfxType.AttackPhysical, Tone("atkP", 240, 170, 0.05f, Wave.Square));
            Set(SfxType.AttackMagic,    Tone("atkM", 700, 1200, 0.09f, Wave.Sine));
            Set(SfxType.EnemyDeath,     Tone("death", 480, 110, 0.16f, Wave.Sine));
            Set(SfxType.Coin,           Tone("coin", 1180, 1340, 0.07f, Wave.Sine));
            Set(SfxType.Synergy,        Arp("syn",  new[] { 523f, 659f, 784f }, 0.09f, Wave.Sine));
            Set(SfxType.Victory,        Arp("win",  new[] { 523f, 659f, 784f, 1046f }, 0.13f, Wave.Sine));
            Set(SfxType.Defeat,         Arp("lose", new[] { 392f, 330f, 262f }, 0.16f, Wave.Sine));
        }

        // Só define se ainda não veio de um arquivo real.
        private void Set(SfxType type, AudioClip generated)
        {
            if (!clips.ContainsKey(type)) clips[type] = generated;
        }

        private AudioClip Tone(string name, float f0, float f1, float dur, Wave wf)
        {
            int n = Mathf.Max(1, (int)(Rate * dur));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float prog = (float)i / n;
                float f = Mathf.Lerp(f0, f1, prog);
                phase += 2f * Mathf.PI * f / Rate;
                float s = (wf == Wave.Sine) ? Mathf.Sin(phase) : Mathf.Sign(Mathf.Sin(phase));
                data[i] = s * Env(prog);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip Arp(string name, float[] notes, float noteDur, Wave wf)
        {
            int per = Mathf.Max(1, (int)(Rate * noteDur));
            int n = per * notes.Length;
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                float phase = 0f;
                for (int i = 0; i < per; i++)
                {
                    float prog = (float)i / per;
                    phase += 2f * Mathf.PI * notes[k] / Rate;
                    float s = (wf == Wave.Sine) ? Mathf.Sin(phase) : Mathf.Sign(Mathf.Sin(phase));
                    data[k * per + i] = s * Env(prog) * 0.9f;
                }
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Envelope: ataque rápido e decaimento suave (evita "clique").
        private static float Env(float prog)
        {
            const float attack = 0.02f;
            if (prog < attack) return prog / attack;
            return Mathf.Pow(1f - (prog - attack) / (1f - attack), 1.5f);
        }
    }
}
