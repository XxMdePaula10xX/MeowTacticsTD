using System.Collections;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace MeowTactics.Managers
{
    /// <summary>
    /// Notificações LOCAIS de retenção (lembretes "volte a jogar") + badge no ícone do
    /// app que SOME quando o jogo é aberto. Tudo no aparelho — sem servidor.
    ///
    /// iOS: usa o pacote "Mobile Notifications" (com.unity.mobile.notifications).
    ///   -> Instale em Window > Package Manager > Unity Registry > Mobile Notifications.
    ///   O código de iOS só compila no BUILD do device (não no editor/PC).
    ///
    /// Fluxo:
    ///   - Ao abrir/voltar ao app: limpa o badge e cancela lembretes pendentes.
    ///   - Ao ir pro background (se ativado): agenda lembretes (1, 3 e 7 dias).
    /// Auto-instancia sozinho (não precisa estar na cena).
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        /// <summary>Ligar/desligar notificações (Configurações). Persistente.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("notifs", 1) == 1;
            set { PlayerPrefs.SetInt("notifs", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("NotificationManager");
            go.AddComponent<NotificationManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (Enabled) RequestAuthorization();
            OnEnterApp(); // limpa o badge ao abrir (sempre)
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) { if (Enabled) ScheduleReminders(); } // foi pro background
            else OnEnterApp();                                // voltou ao app
        }

        // Chamado quando o jogador (re)entra no app.
        private void OnEnterApp()
        {
            ClearBadge();
            CancelScheduled();
        }

        /// <summary>Aplica a escolha do jogador no toggle das Configurações.</summary>
        public static void SetEnabled(bool on)
        {
            Enabled = on;
            if (Instance == null) return;
            if (on) Instance.RequestAuthorization();
            else { Instance.CancelScheduled(); Instance.ClearBadge(); }
        }

        /// <summary>Zera o badge do ícone e remove notificações já entregues.</summary>
        public void ClearBadge()
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.ApplicationBadge = 0;
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }

        public void CancelScheduled()
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }

        public void RequestAuthorization()
        {
#if UNITY_IOS && !UNITY_EDITOR
            StartCoroutine(RequestAuthRoutine());
#endif
        }

        /// <summary>Agenda os lembretes "volte a jogar" (com badge crescente no ícone).</summary>
        public void ScheduleReminders()
        {
            if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            ScheduleOne("rem1", "Seus gatos sentem sua falta! 🐱",
                "Volte e defenda o reino contra os pesadelos.", 1 * 24 * 60, 1);
            ScheduleOne("rem2", "Os pesadelos estão voltando… 👻",
                "Suas torres-gato precisam de você!", 3 * 24 * 60, 2);
            ScheduleOne("rem3", "Que tal uma partida rápida? ⚔️",
                "Novas ondas e o Desafio Diário esperam por você!", 7 * 24 * 60, 3);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private IEnumerator RequestAuthRoutine()
        {
            var options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
            using (var req = new AuthorizationRequest(options, false))
            {
                while (!req.IsFinished) yield return null;
            }
        }

        private void ScheduleOne(string id, string title, string body, int minutes, int badge)
        {
            var n = new iOSNotification
            {
                Identifier = id,
                Title = title,
                Body = body,
                ShowInForeground = false,
                Badge = badge,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = new System.TimeSpan(0, minutes, 0),
                    Repeats = false
                }
            };
            iOSNotificationCenter.ScheduleNotification(n);
        }
#endif
    }
}
