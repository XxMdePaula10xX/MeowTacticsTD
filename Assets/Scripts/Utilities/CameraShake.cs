using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Tremor de câmera (game juice). Adicione na câmera; chame CameraShake.Shake(força, duração)
    /// nos momentos importantes (boss, perder vida, game over). Usa tempo NÃO escalado, então
    /// funciona mesmo com o jogo pausado/acelerado.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance;

        private Vector3 basePos;
        private float amount;
        private float timer;
        private float duration;

        private void Awake()
        {
            Instance = this;
            basePos = transform.localPosition;
        }

        public static void Shake(float amount, float duration)
        {
            if (Instance != null) Instance.Begin(amount, duration);
        }

        private void Begin(float a, float d)
        {
            amount = Mathf.Max(amount, a); // mantém o tremor mais forte vigente
            duration = Mathf.Max(0.01f, d);
            timer = duration;
        }

        private void LateUpdate()
        {
            if (timer <= 0f)
            {
                if (transform.localPosition != basePos) transform.localPosition = basePos;
                return;
            }

            timer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(timer / duration);
            float mag = amount * k;
            Vector2 off = Random.insideUnitCircle * mag;
            transform.localPosition = basePos + new Vector3(off.x, off.y, 0f);

            if (timer <= 0f) { transform.localPosition = basePos; amount = 0f; }
        }
    }
}
