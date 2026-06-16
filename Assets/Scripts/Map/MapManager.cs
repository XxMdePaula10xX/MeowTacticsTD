using System.Collections.Generic;
using UnityEngine;

namespace MeowTactics.Map
{
    /// <summary>
    /// Guarda o caminho dos inimigos e os slots de posicionamento do mapa.
    /// Os pontos do caminho são objetos filhos de "PathParent", em ordem.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [Header("Caminho (na ordem do início até a base)")]
        public Transform pathParent;
        [Tooltip("Preenchido automaticamente a partir dos filhos de pathParent, se vazio")]
        public List<Transform> pathPoints = new List<Transform>();

        [Header("Slots de posicionamento")]
        public List<MapSlot> placementSlots = new List<MapSlot>();

        private List<Vector3> cachedPath;

        private void Awake()
        {
            Instance = this;
            BuildPathPointsFromParent();
            CachePath();
        }

        private void BuildPathPointsFromParent()
        {
            if ((pathPoints == null || pathPoints.Count == 0) && pathParent != null)
            {
                pathPoints = new List<Transform>();
                foreach (Transform child in pathParent)
                    pathPoints.Add(child);
            }
        }

        private void CachePath()
        {
            cachedPath = new List<Vector3>();
            foreach (var p in pathPoints)
                if (p != null) cachedPath.Add(p.position);
        }

        /// <summary>Posições do caminho (início -> base).</summary>
        public IReadOnlyList<Vector3> GetPath()
        {
            if (cachedPath == null || cachedPath.Count == 0) CachePath();
            return cachedPath;
        }

        public Vector3 SpawnPoint =>
            (pathPoints != null && pathPoints.Count > 0 && pathPoints[0] != null)
                ? pathPoints[0].position : Vector3.zero;

        public Vector3 BasePoint =>
            (pathPoints != null && pathPoints.Count > 0 && pathPoints[pathPoints.Count - 1] != null)
                ? pathPoints[pathPoints.Count - 1].position : Vector3.zero;

        public List<MapSlot> GetAvailableSlots()
        {
            var free = new List<MapSlot>();
            foreach (var slot in placementSlots)
                if (slot != null && slot.IsEmpty) free.Add(slot);
            return free;
        }

        // Desenha o caminho no editor para facilitar o ajuste manual.
        private void OnDrawGizmos()
        {
            var pts = (pathPoints != null && pathPoints.Count > 0)
                ? pathPoints
                : null;
            if (pts == null && pathParent != null)
            {
                pts = new List<Transform>();
                foreach (Transform c in pathParent) pts.Add(c);
            }
            if (pts == null) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                if (pts[i] != null && pts[i + 1] != null)
                {
                    Gizmos.DrawLine(pts[i].position, pts[i + 1].position);
                    Gizmos.DrawSphere(pts[i].position, 0.15f);
                }
            }
        }
    }
}
