using System.Collections.Generic;
using UnityEngine;

namespace MeowTactics.Map
{
    /// <summary>
    /// Guarda os CAMINHOS dos inimigos (um ou mais) e os limites da área jogável.
    /// Cada caminho é um objeto-pai cujos filhos são os pontos, em ordem.
    /// Com vários caminhos, os inimigos se dividem entre eles.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [Header("Caminhos (cada pai = um caminho; filhos = pontos em ordem)")]
        public List<Transform> pathParents = new List<Transform>();
        [Tooltip("Compatibilidade: caminho único (usado se a lista acima estiver vazia)")]
        public Transform pathParent;

        [Header("Slots de posicionamento (legado, não usado no modo livre)")]
        public List<MapSlot> placementSlots = new List<MapSlot>();

        [Header("Área jogável (posicionamento livre)")]
        public float boardLeft = -10f;
        public float boardRight = 10f;
        public float boardBottom = -3f;
        public float boardTop = 4.5f;
        [Tooltip("Quão perto da estrada um gato pode ser posto (raio de bloqueio)")]
        public float pathRadius = 0.9f;

        private List<List<Vector3>> cachedPaths;

        private void Awake()
        {
            Instance = this;
            CachePaths();
        }

        /// <summary>Recalcula o cache dos caminhos (após trocar pathParents em runtime).</summary>
        public void RebuildPaths() => CachePaths();

        private void CachePaths()
        {
            cachedPaths = new List<List<Vector3>>();

            var parents = new List<Transform>();
            if (pathParents != null)
                foreach (var p in pathParents) if (p != null) parents.Add(p);
            if (parents.Count == 0 && pathParent != null) parents.Add(pathParent);

            foreach (var parent in parents)
            {
                var pts = new List<Vector3>();
                foreach (Transform child in parent) pts.Add(child.position);
                if (pts.Count > 0) cachedPaths.Add(pts);
            }
        }

        /// <summary>Quantos caminhos existem (>=1).</summary>
        public int PathCount
        {
            get { if (cachedPaths == null) CachePaths(); return Mathf.Max(1, cachedPaths.Count); }
        }

        /// <summary>Pontos do caminho de índice 'index' (início -> base).</summary>
        public IReadOnlyList<Vector3> GetPath(int index)
        {
            if (cachedPaths == null || cachedPaths.Count == 0) CachePaths();
            if (cachedPaths.Count == 0) return new List<Vector3>();
            index = Mathf.Clamp(index, 0, cachedPaths.Count - 1);
            return cachedPaths[index];
        }

        /// <summary>Caminho principal (índice 0) — compatibilidade.</summary>
        public IReadOnlyList<Vector3> GetPath() => GetPath(0);

        public Vector3 SpawnPoint
        {
            get { var p = GetPath(0); return p.Count > 0 ? p[0] : Vector3.zero; }
        }

        public Vector3 BasePoint
        {
            get { var p = GetPath(0); return p.Count > 0 ? p[p.Count - 1] : Vector3.zero; }
        }

        /// <summary>True se a posição está dentro da área onde se pode construir.</summary>
        public bool InsideBoard(Vector3 p) =>
            p.x >= boardLeft && p.x <= boardRight && p.y >= boardBottom && p.y <= boardTop;

        /// <summary>True se a posição está em cima (ou colada) em QUALQUER caminho.</summary>
        public bool IsOnPath(Vector3 p) => DistanceToPath(p) < pathRadius;

        /// <summary>Menor distância da posição até a linha de qualquer caminho.</summary>
        public float DistanceToPath(Vector3 p)
        {
            if (cachedPaths == null || cachedPaths.Count == 0) CachePaths();
            float best = 999f;
            foreach (var path in cachedPaths)
            {
                if (path.Count == 0) continue;
                if (path.Count == 1) { best = Mathf.Min(best, Vector3.Distance(p, path[0])); continue; }
                for (int i = 0; i < path.Count - 1; i++)
                    best = Mathf.Min(best, DistancePointSegment(p, path[i], path[i + 1]));
            }
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

        public List<MapSlot> GetAvailableSlots()
        {
            var free = new List<MapSlot>();
            foreach (var slot in placementSlots)
                if (slot != null && slot.IsEmpty) free.Add(slot);
            return free;
        }

        // Desenha os caminhos no editor para facilitar o ajuste manual dos pontos.
        private void OnDrawGizmos()
        {
            var parents = new List<Transform>();
            if (pathParents != null)
                foreach (var p in pathParents) if (p != null) parents.Add(p);
            if (parents.Count == 0 && pathParent != null) parents.Add(pathParent);

            Color[] cols = { Color.yellow, Color.cyan };
            for (int pi = 0; pi < parents.Count; pi++)
            {
                Gizmos.color = cols[pi % cols.Length];
                var pts = new List<Transform>();
                foreach (Transform c in parents[pi]) pts.Add(c);
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
}
