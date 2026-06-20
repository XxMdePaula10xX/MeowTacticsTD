using UnityEngine;
using UnityEngine.EventSystems;

namespace MeowTactics.Map
{
    /// <summary>
    /// Colisor invisível que cobre a área jogável. Captura cliques/toques no mapa
    /// e repassa a posição de mundo para o PlacementManager (posicionamento livre).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BoardClicker : MonoBehaviour
    {
        private void OnMouseDown()
        {
            // Ignora se o toque está sobre algum elemento de UI.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 m = Input.mousePosition;
            m.z = -cam.transform.position.z;
            Vector3 world = cam.ScreenToWorldPoint(m);
            world.z = 0f;

            Managers.PlacementManager.Instance?.OnBoardClicked(world);
        }
    }
}
