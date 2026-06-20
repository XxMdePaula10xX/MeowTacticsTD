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

        [Header("Slots de posicionamento (legado, não usado no modo livre)")]
        public List<MapSlot> placementSlots = new List<MapSlot>();

        [Header("Área jogável (posicionamento livre)")]
        public float boardLeft = -10f;
        public float boardRight = 10f;
        public float boardBottom = -3f;
        public float boardTop = 4.5f;
        [Tooltip("Quão perto da estrada um gato pode ser posto (raio de bloqueio)")]
        public float pathRadius = 0.9f;

        private List<Vector3> cachedPath;

        /// <summary>True se a posição está dentro da área onde se pode construir.</summary>
        public bool InsideBoard(Vector3 p) =>
            p.x >= boardLeft && p.x <= boardRight && p.y >= boardBottom && p.y <= boardTop;

        /// <summary>True se a posição está em cima (ou colada) na estrada dos inimigos.</summary>
        public bool IsOnPath(Vector3 p) => DistanceToPath(p) < pathRadius;

        /// <summary>Menor distância da posição até a linha do caminho.</summary>
        public float DistanceToPath(Vector3 p)
        {
            var path = GetPath();
            if (path == null || path.Count == 0) return 999f;
            if (path.Count == 1) return Vector3.Distance(p, path[0]);
            float best = float.MaxValue;
            for (int i = 0; i < path.Count - 1; i++)
                best = Mathf.Min(best, DistancePointSegment(p, path[i], path[i + 1]));
            return best;
        }

        private static float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 ap = new Vector2(p.x - a.x, p.y - a.y);
            Vector2 ab = new Vector2(b.x - a.x, b.y - a.y);
            float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(ap, ab) / len2) : 0f;
            Vector2 proj = new Vector2(a.x + ab.x * t, a.y + ab.y * t);
            return Vector2.Distance(new Vector2(p.x, p.y), proj);
        }

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
