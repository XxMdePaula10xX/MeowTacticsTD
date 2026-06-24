using System.Collections;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace MeowTactics.Managers
{
    /// <summary>
    /// Lembretes locais para o jogador voltar (retenção) + badge no ícone do app
    /// que SOME quando o jogo é aberto.
    ///
    /// iOS: usa o pacote "Mobile Notifications" (com.unity.mobile.notifications).
    ///   -> Instale em Window > Package Manager > Unity Registry > Mobile Notifications.
    ///   O código de iOS só é compilado no BUILD do device (não no editor/PC), então
    ///   o pacote só é necessário na hora de gerar o build iOS.
    ///
    /// Fluxo:
    ///   - Ao abrir/voltar ao app: limpa o badge e cancela lembretes pendentes.
    ///   - Ao mandar pro background: agenda lembretes (1, 3 e 7 dias depois).
    /// Auto-instancia sozinho (não precisa estar na cena).
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

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
            RequestAuthorization();
            OnEnterApp();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleReminders(); // foi pro background
            else OnEnterApp();               // voltou ao app
        }

        // Chamado quando o jogador (re)entra no app.
        private void OnEnterApp()
        {
            ClearBadge();
            CancelScheduled();
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

        private void RequestAuthorization()
        {
#if UNITY_IOS && !UNITY_EDITOR
            StartCoroutine(RequestAuthRoutine());
#endif
        }

        /// <summary>Agenda os lembretes "volte a jogar" (com badge no ícone).</summary>
        public void ScheduleReminders()
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            ScheduleOne("rem1", "Seus gatos sentem sua falta! 🐱",
                "Volte e defenda o reino contra os pesadelos.", 1 * 24 * 60);
            ScheduleOne("rem2", "Os pesadelos estão voltando… 👻",
                "Suas torres-gato precisam de você!", 3 * 24 * 60);
            ScheduleOne("rem3", "Que tal uma partida rápida? ⚔️",
                "Novas ondas esperam por você em Meow Tactics.", 7 * 24 * 60);
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

        private void ScheduleOne(string id, string title, string body, int minutes)
        {
            var n = new iOSNotification
            {
                Identifier = id,
                Title = title,
                Body = body,
                ShowInForeground = false,
                Badge = 1,
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
