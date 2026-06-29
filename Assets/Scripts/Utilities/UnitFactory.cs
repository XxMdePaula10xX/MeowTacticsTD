using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Cats;
using MeowTactics.Data;
using MeowTactics.Enemies;
using MeowTactics.UI;

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

        /// <summary>Cria uma sombrinha (objeto separado) que segue a unidade por baixo.</summary>
        private static void CreateShadow(Transform target, int sortingOrder, float width, float offsetY)
        {
            var s = new GameObject("Shadow");
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle;
            sr.color = new Color(0f, 0f, 0f, 0.28f);
            sr.sortingOrder = sortingOrder;
            s.transform.localScale = new Vector3(width, width * 0.4f, 1f);
            var p = target.position;
            s.transform.position = new Vector3(p.x, p.y + offsetY, p.z + 0.01f);
            s.AddComponent<ShadowFollow>().Setup(target, offsetY);
        }

        /// <summary>Base de tabuleiro sob o gato: placa escura + borda colorida por classe +
        /// brilho de seleção (desligado por padrão). Devolve o SpriteRenderer do brilho.</summary>
        private static SpriteRenderer CreateCatPad(Transform target, int sortingOrder, float width, float offsetY, Color color)
        {
            var root = new GameObject("BasePad");
            var p = target.position;
            root.transform.position = new Vector3(p.x, p.y + offsetY, p.z + 0.01f);
            root.AddComponent<ShadowFollow>().Setup(target, offsetY);

            // Brilho radial de seleção (acende quando o gato é selecionado).
            var glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            var gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = UISprites.Glow;
            gsr.color = new Color(color.r, color.g, color.b, 0.85f);
            gsr.sortingOrder = sortingOrder - 1;
            glow.transform.localScale = new Vector3(width * 2.1f, width * 1.0f, 1f);
            gsr.enabled = false;

            // Placa escura (dá contraste com o gramado).
            var plate = new GameObject("Plate");
            plate.transform.SetParent(root.transform, false);
            var psr = plate.AddComponent<SpriteRenderer>();
            psr.sprite = UISprites.Disc;
            psr.color = new Color(0f, 0f, 0f, 0.32f);
            psr.sortingOrder = sortingOrder;
            plate.transform.localScale = new Vector3(width, width * 0.42f, 1f);

            // Borda colorida pela classe (tipo de dano).
            var rim = new GameObject("Rim");
            rim.transform.SetParent(root.transform, false);
            var rsr = rim.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteFactory.Ring;
            rsr.color = new Color(color.r, color.g, color.b, 0.9f);
            rsr.sortingOrder = sortingOrder + 1;
            rim.transform.localScale = new Vector3(width, width * 0.42f, 1f);

            return gsr;
        }

        private static Color DamageGlow(DamageType t)
        {
            switch (t)
            {
                case DamageType.Physical: return new Color(1f, 0.6f, 0.3f);
                case DamageType.Magical:  return new Color(0.72f, 0.5f, 1f);
                case DamageType.True:     return new Color(1f, 0.9f, 0.55f);
                default: return Color.white;
            }
        }

        /// <summary>Pequeno marcador de tipo acima do inimigo (blindado / resist. mágica / rápido).</summary>
        private static void CreateEnemyTrait(Transform parent, EnemyData data)
        {
            Color c; Sprite spr; float rot = 0f;
            if (data.armor >= 30f) { c = new Color(0.78f, 0.82f, 0.88f); spr = SpriteFactory.Square; }            // blindado: placa cinza
            else if (data.magicResistance >= 30f) { c = new Color(0.72f, 0.5f, 1f); spr = SpriteFactory.Square; rot = 45f; } // sombra: gema roxa (losango)
            else if (data.moveSpeed >= 1.5f) { c = new Color(0.4f, 0.9f, 1f); spr = SpriteFactory.Circle; }       // rápido: ciano
            else return;

            var go = new GameObject("Trait");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.42f, 0.5f, 0f);
            go.transform.localScale = Vector3.one * 0.26f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr; sr.color = c; sr.sortingOrder = SortHealthBar + 2;
        }

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

            // Ícones de itens equipados acima do gato (objeto separado, segue o gato).
            var badges = new GameObject("ItemBadges");
            badges.transform.position = cat.transform.position + new Vector3(0f, 0.78f, 0f);
            badges.AddComponent<ItemBadges>().Setup(cat);

            return cat;
        }

        private static CatUnit BuildPlaceholderCat(CatData data, Vector3 position)
        {
            var go = new GameObject("Cat_" + data.catName);
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon != null ? data.icon : SpriteFactory.Circle;
            sr.color = data.icon != null ? Color.white : data.placeholderColor;
            sr.sortingOrder = SortCat;

            // Indicador de alcance (anel), desligado por padrão
            var ring = new GameObject("RangeIndicator");
            ring.transform.SetParent(go.transform, false);
            var rsr = ring.AddComponent<SpriteRenderer>();
            rsr.sprite = SpriteFactory.Ring;
            rsr.color = new Color(1f, 1f, 1f, 0.35f);
            rsr.sortingOrder = SortRange;
            ring.SetActive(false);

            // (sem sombra embaixo do gato — só a base de tabuleiro colorida)
            var padGlow = CreateCatPad(go.transform, SortCat - 1, 0.66f, -0.40f, DamageGlow(data.damageType));

            go.AddComponent<JuiceVisual>();
            var cu = go.AddComponent<CatUnit>();
            cu.SetSelectionGlow(padGlow);
            return cu;
        }

        public static EnemyUnit CreateEnemy(EnemyData data, float scaling, Vector3 position,
            IReadOnlyList<Vector3> path = null)
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
            enemy.Initialize(data, scaling, path ?? MeowTactics.Map.MapManager.Instance.GetPath());
            return enemy;
        }

        private static EnemyUnit BuildPlaceholderEnemy(EnemyData data, Vector3 position)
        {
            var go = new GameObject("Enemy_" + data.enemyName);
            go.transform.position = position;

            // Visual num filho separado, que GIRA na direção do movimento — assim a
            // barra de vida e o marcador de tipo (filhos da raiz) NÃO giram junto.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = data.icon != null ? data.icon : SpriteFactory.Circle;
            sr.color = data.icon != null ? Color.white : data.placeholderColor;
            sr.sortingOrder = SortEnemy;
            var vjuice = visual.AddComponent<JuiceVisual>();
            vjuice.destroyRoot = go.transform; // o "poof" destrói o inimigo inteiro

            // Barra de vida (boss tem barra maior e mais alta = destacada)
            float barW = data.isBoss ? 1.5f : 1.0f;
            float barH = data.isBoss ? 0.22f : 0.16f;
            float barY = data.isBoss ? 0.78f : 0.6f;

            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(go.transform, false);
            bar.transform.localPosition = new Vector3(0f, barY, 0f);

            var bg = new GameObject("BG");
            bg.transform.SetParent(bar.transform, false);
            var bgsr = bg.AddComponent<SpriteRenderer>();
            bgsr.sprite = SpriteFactory.Square;
            bgsr.color = data.isBoss ? new Color(0.25f, 0.18f, 0.02f) : Color.black; // moldura dourada no boss
            bgsr.sortingOrder = SortHealthBar;
            bg.transform.localScale = new Vector3(barW, barH, 1f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            var fsr = fill.AddComponent<SpriteRenderer>();
            fsr.sprite = SpriteFactory.Square;
            fsr.color = Color.green;
            fsr.sortingOrder = SortHealthBar + 1;
            fill.transform.localScale = new Vector3(barW * 0.92f, barH * 0.62f, 1f);

            var hb = bar.AddComponent<HealthBar>();
            hb.SetFillTransform(fill.transform);

            // (sem sombra embaixo do inimigo)
            CreateEnemyTrait(go.transform, data);

            go.AddComponent<EnemyUnit>();
            return go.GetComponent<EnemyUnit>();
        }
    }
}
