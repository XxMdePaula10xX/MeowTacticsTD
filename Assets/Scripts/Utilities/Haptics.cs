using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>Vibração simples do aparelho (mobile), com liga/desliga nas configurações.</summary>
    public static class Haptics
    {
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("haptics", 1) == 1;
            set { PlayerPrefs.SetInt("haptics", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Buzz()
        {
            if (!Enabled) return;
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
