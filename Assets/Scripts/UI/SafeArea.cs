using UnityEngine;

namespace MeowTactics.UI
{
    /// <summary>
    /// Ajusta um RectTransform para caber na Safe Area do aparelho (notch / Dynamic
    /// Island / indicador de home do iPhone). Coloque num container que envolve a HUD;
    /// os elementos ancorados às bordas passam a respeitar a área segura.
    /// No editor/PC a Safe Area é a tela inteira, então nada muda.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rt;
        private Rect lastSafe;
        private Vector2Int lastScreen;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Reaplica se a Safe Area mudar (rotação, split view, etc.).
            if (Screen.safeArea != lastSafe ||
                Screen.width != lastScreen.x || Screen.height != lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            if (rt == null) return;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            lastSafe = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = lastSafe.position;
            Vector2 max = lastSafe.position + lastSafe.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;

            // Segurança contra valores inválidos (evita NaN).
            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
