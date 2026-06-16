using System;
using System.Collections.Generic;
using UnityEngine;
using MeowTactics.Core;
using MeowTactics.Cats;

namespace MeowTactics.Managers
{
    /// <summary>
    /// Banco de gatos comprados e ainda não posicionados.
    /// Gatos no banco contam para upgrade de estrela, mas NÃO atacam
    /// nem contam para sinergias.
    /// </summary>
    public class BenchManager : MonoBehaviour
    {
        public static BenchManager Instance { get; private set; }

        public int maxSlots = GameBalance.BenchSize;
        public readonly List<CatUnit> benchCats = new List<CatUnit>();

        public event Action OnBenchChanged;

        private void Awake()
        {
            Instance = this;
        }

        public bool HasSpace() => benchCats.Count < maxSlots;

        public void AddCatToBench(CatUnit cat)
        {
            if (cat == null) return;
            cat.IsPlaced = false;
            cat.gameObject.SetActive(false); // fica "guardado", não aparece no mapa
            if (!benchCats.Contains(cat)) benchCats.Add(cat);
            OnBenchChanged?.Invoke();
        }

        public void RemoveCatFromBench(CatUnit cat)
        {
            if (benchCats.Remove(cat))
                OnBenchChanged?.Invoke();
        }

        public void NotifyChanged() => OnBenchChanged?.Invoke();
    }
}
