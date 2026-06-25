using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Managers;

namespace MeowTactics.Map
{
    /// <summary>
    /// Permite múltiplos mapas numa única cena. A cena já vem montada com o mapa
    /// "padrão" (bakedMapId). Em runtime, se o jogador escolheu OUTRO mapa
    /// (SaveSystem.CurrentMapId), este componente troca o fundo, os caminhos e os
    /// marcadores pelo mapa selecionado. Todos os dados (sprites + pontos) são
    /// serializados aqui pelo editor (MeowSetup), então nada é carregado de disco
    /// em runtime.
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
            DestroyRoot("Path_A");
            DestroyRoot("Path_B");
            DestroyRoot("SpawnPortal");
            DestroyRoot("BaseCrystal");

            // Fundo.
            if (def.background != null)
            {
                var go = new GameObject("Background");
                go.transform.position = Vector3.zero;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = def.background;
                sr.sortingOrder = -100;
            }

            // Caminhos.
            var parents = new List<Transform>();
            if (def.pathA != null && def.pathA.Length > 0) parents.Add(MakePath("Path_A", def.pathA));
            if (def.pathB != null && def.pathB.Length > 0) parents.Add(MakePath("Path_B", def.pathB));

            var map = FindObjectOfType<MapManager>();
            if (map != null)
            {
                map.pathParents = parents;
                map.RebuildPaths();
            }

            // Marcadores de início/fim (no primeiro caminho).
            if (def.pathA != null && def.pathA.Length > 0)
            {
                CreateMarker(def.pathA[0], Marker.Kind.Spawn, "SpawnPortal");
                CreateMarker(def.pathA[def.pathA.Length - 1], Marker.Kind.Base, "BaseCrystal");
            }
        }

        private static void DestroyRoot(string objName)
        {
            foreach (var go in FindObjectsOfType<GameObject>())
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

        private static void CreateMarker(Vector2 pos, Marker.Kind kind, string name)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.AddComponent<Marker>().kind = kind;
        }
    }
}
