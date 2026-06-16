using UnityEngine;
using MeowTactics.Cats;
using MeowTactics.Data;
using MeowTactics.Enemies;

namespace MeowTactics.Utilities
{
    /// <summary>
    /// Cria instâncias de gatos e inimigos. Se o asset tiver um prefab de arte,
    /// usa ele; senão, monta um objeto-placeholder colorido em código.
    /// </summary>
    public static class UnitFactory
    {
        private const int SortCat = 10;
        private const int SortEnemy = 20;
        private const int SortHealthBar = 25;
        private const int SortRange = 5;

        public static CatUnit CreateCat(CatData data, Vector3 position)
        {
            CatUnit cat;
            if (data.catPrefab != null)
            {
                var go = Object.Instantiate(data.catPrefab, position, Quaternion.identity);
                cat = go.GetComponent<CatUnit>();
                if (cat == null) cat = go.AddComponent<CatUnit>();
            }
            else
            {
                cat = BuildPlaceholderCat(data, position);
            }
            cat.Initialize(data);
            return cat;
        }

        private static CatUnit BuildPlaceholderCat(CatData data, Vector3 position)
        {
            var go = new GameObject("Cat_" + data.catName);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon != null ? data.icon : SpriteFactory.Circle;
            sr.color = data.placeholderColor;
            sr.sortingOrder = SortCat;

            // Indicador de alcance (anel), desligado por padrão
            var ring = new GameObject("RangeIndicator");
            ring.transform.SetParent(go.transform, false);
            var rsr = ring.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteFactory.Ring;
            rsr.color = new Color(1f, 1f, 1f, 0.35f);
            rsr.sortingOrder = SortRange;
            ring.SetActive(false);

            go.AddComponent<CatUnit>();
            return go.GetComponent<CatUnit>();
        }

        public static EnemyUnit CreateEnemy(EnemyData data, float scaling, Vector3 position)
        {
            EnemyUnit enemy;
            if (data.enemyPrefab != null)
            {
                var go = Object.Instantiate(data.enemyPrefab, position, Quaternion.identity);
                enemy = go.GetComponent<EnemyUnit>();
                if (enemy == null) enemy = go.AddComponent<EnemyUnit>();
            }
            else
            {
                enemy = BuildPlaceholderEnemy(data, position);
            }
            enemy.Initialize(data, scaling, MeowTactics.Map.MapManager.Instance.GetPath());
            return enemy;
        }

        private static EnemyUnit BuildPlaceholderEnemy(EnemyData data, Vector3 position)
        {
            var go = new GameObject("Enemy_" + data.enemyName);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon != null ? data.icon : SpriteFactory.Circle;
            sr.color = data.placeholderColor;
            sr.sortingOrder = SortEnemy;

            // Barra de vida
            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(go.transform, false);
            bar.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            var bg = new GameObject("BG");
            bg.transform.SetParent(bar.transform, false);
            var bgsr = bg.AddComponent<SpriteRenderer>();
            bgsr.sprite = SpriteFactory.Square;
            bgsr.color = Color.black;
            bgsr.sortingOrder = SortHealthBar;
            bg.transform.localScale = new Vector3(1.0f, 0.16f, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            var fsr = fill.AddComponent<SpriteRenderer>();
            fsr.sprite = SpriteFactory.Square;
            fsr.color = Color.green;
            fsr.sortingOrder = SortHealthBar + 1;
            fill.transform.localScale = new Vector3(0.92f, 0.10f, 1f);

            var hb = bar.AddComponent<HealthBar>();
            hb.SetFillTransform(fill.transform);

            go.AddComponent<EnemyUnit>();
            return go.GetComponent<EnemyUnit>();
        }
    }
}
