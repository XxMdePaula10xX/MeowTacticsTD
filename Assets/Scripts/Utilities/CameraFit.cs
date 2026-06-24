using UnityEngine;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Ajusta o orthographicSize da câmera para que o MAPA INTEIRO caiba sempre na
    /// tela (modo "contain"), em qualquer proporção (iPhone largo, iPad 4:3, etc.).
    /// Em telas mais largas que o mapa sobra faixa em cima/embaixo; em telas mais
    /// estreitas, sobra dos lados — mas nunca corta o caminho. As faixas mostram a
    /// cor de fundo (tema noturno) da câmera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [Tooltip("Tamanho do mapa em unidades de mundo (largura e altura).")]
        public float mapWorldWidth = 21.3f;
        public float mapWorldHeight = 12f;

        private Camera cam;
        private float lastAspect = -1f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Apply();
        }

        private void Update()
        {
            if (cam != null && !Mathf.Approximately(cam.aspect, lastAspect)) Apply();
        }

        private void Apply()
        {
            if (cam == null || !cam.orthographic) return;
            lastAspect = cam.aspect;

            float byHeight = mapWorldHeight * 0.5f;
            float byWidth = (mapWorldWidth * 0.5f) / Mathf.Max(0.01f, cam.aspect);
            cam.orthographicSize = Mathf.Max(byHeight, byWidth);
        }
    }
}
