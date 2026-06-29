using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Managers;

namespace MeowTactics.Map
{
    /// <summary>
    /// Permite múltiplos mapas numa única cena. A cena já vem montada com o mapa
    /// "padrão" (bakedMapId). Em runtime, se o jogador escolheu OUTRO mapa
    /// (SaveSystem.CurrentMapId), troca o fundo, os caminhos (1 a 3 trilhas) e os
    /// marcadores pelo selecionado. Tudo é serializado pelo editor (MeowSetup),
    /// então nada é carregado de disco em runtime.
    /// </summary>
    public class RuntimeMapBuilder : MonoBehaviour
    {
        [System.Serializable]
        public class MapDef
        {
            public string id;
            public Sprite background;
            public Vector2[] pathA;
            public Vector2[] pathB;
            public Vector2[] pathC;
        }

        [Tooltip("Mapa que já está montado na cena (não precisa reconstruir).")]
        public string bakedMapId = "bosque";
        public List<MapDef> maps = new List<MapDef>();

        private void Awake()
        {
            string id = SaveSystem.CurrentMapId;
            if (string.IsNullOrEmpty(id)) id = bakedMapId;
            if (id == bakedMapId) return; // a cena já está com o mapa certo

            MapDef def = Find(id);
            if (def == null) return; // mapa desconhecido: mantém o que está montado
            SwapTo(def);
        }

        private MapDef Find(string id)
        {
            foreach (var m in maps) if (m != null && m.id == id) return m;
            return null;
        }

        private void SwapTo(MapDef def)
        {
            // Remove o mapa montado (baked) da cena.
            DestroyRoot("Background");
            DestroyRoot("Path_A"); DestroyRoot("Path_B"); DestroyRoot("Path_C");
            DestroyRoot("SpawnPortal"); DestroyRoot("BaseCrystal");

            // Fundo.
            if (def.background != null)
            {
                var go = new GameObject("Background");
                go.transform.position = Vector3.zero;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = def.background;
                sr.sortingOrder = -100;
            }

            // Caminhos (1 a 3 trilhas).
            var lanes = new List<Vector2[]>();
            if (def.pathA != null && def.pathA.Length > 0) lanes.Add(def.pathA);
            if (def.pathB != null && def.pathB.Length > 0) lanes.Add(def.pathB);
            if (def.pathC != null && def.pathC.Length > 0) lanes.Add(def.pathC);

            var parents = new List<Transform>();
            char letter = 'A';
            foreach (var lane in lanes)
                parents.Add(MakePath("Path_" + (letter++), lane));

            var map = FindAnyObjectByType<MapManager>();
            if (map != null)
            {
                map.pathParents = parents;
                map.RebuildPaths();
            }

            // Marcadores: um portal por início de trilha e um cristal por fim
            // (deduplicados quando trilhas começam/terminam no mesmo lugar).
            var spawns = new List<Vector2>();
            var bases = new List<Vector2>();
            foreach (var lane in lanes)
            {
                if (lane.Length == 0) continue;
                PlaceMarkerDedup(lane[0], Marker.Kind.Spawn, "SpawnPortal", spawns);
                PlaceMarkerDedup(lane[lane.Length - 1], Marker.Kind.Base, "BaseCrystal", bases);
            }
        }

        private void PlaceMarkerDedup(Vector2 pos, Marker.Kind kind, string objName, List<Vector2> placed)
        {
            foreach (var p in placed) if (Vector2.Distance(p, pos) < 0.7f) return;
            placed.Add(pos);
            var go = new GameObject(objName);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<Marker>().kind = kind;
        }

        private static void DestroyRoot(string objName)
        {
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.transform.parent == null && go.name == objName)
                    Destroy(go);
        }

        private static Transform MakePath(string name, Vector2[] pts)
        {
            var parent = new GameObject(name).transform;
            for (int i = 0; i < pts.Length; i++)
            {
                var p = new GameObject("Point_" + i).transform;
                p.SetParent(parent, false);
                p.position = new Vector3(pts[i].x, pts[i].y, 0f);
            }
            return parent;
        }
    }
}
