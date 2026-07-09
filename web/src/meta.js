// Persistência (localStorage), recordes e Desafio Diário.
(function (MT) {
  'use strict';
  const D = MT.DATA;
  const LS = window.localStorage;
  function get(k, def) { try { const v = LS.getItem(k); return v == null ? def : v; } catch (e) { return def; } }
  function set(k, v) { try { LS.setItem(k, v); } catch (e) {} }

  // ---------- RECORDES ----------
  const progress = {
    keyFor(mode, mapId) {
      if (mode === 'daily') return 'mt_bestDaily_' + MT.daily.todayKey();
      if (mode === 'endless') return 'mt_bestE_' + mapId;
      return 'mt_best_' + mapId;
    },
    bestFor(mode, mapId) { return parseInt(get(this.keyFor(mode, mapId), '0'), 10) || 0; },
    recordBest(wave) {
      const g = MT.game, k = this.keyFor(g.mode, g.mapId);
      if (wave > (parseInt(get(k, '0'), 10) || 0)) set(k, String(wave));
    },
    onWave(waveNumber, kills) {
      // totais (base para conquistas futuras)
      set('mt_totWaves', String((parseInt(get('mt_totWaves', '0'), 10) || 0) + 1));
    },
  };

  // ---------- SAVE (só modo normal) ----------
  const SKEY = 'mt_save';
  const save = {
    has() { return get(SKEY, null) != null; },
    clear() { try { LS.removeItem(SKEY); } catch (e) {} },
    write() {
      const g = MT.game;
      if (g.mode !== 'normal') return;
      const enc = (c) => ({ id: c.data.id, x: c.x, y: c.y, items: c.items.slice(), invested: c.invested });
      const d = { mapId: g.mapId, coins: g.coins, lives: g.lives, waveIndex: g.waveIndex,
        board: g.board.map(enc), bench: g.bench.map(enc), inventory: g.inventory.slice() };
      set(SKEY, JSON.stringify(d));
    },
    restore() {
      const raw = get(SKEY, null); if (!raw) return false;
      let d; try { d = JSON.parse(raw); } catch (e) { return false; }
      const g = MT.game;
      MT.api.newRun('normal', d.mapId);
      g.coins = d.coins; g.lives = d.lives; g.waveIndex = d.waveIndex;
      g.bench = []; g.board = [];
      const build = (s, onBoard) => {
        const c = MT.game && MT.rules.CAT[s.id] ? { uid: MT.util.uid(), data: MT.rules.CAT[s.id], items: s.items || [],
          x: s.x, y: s.y, cd: 0, ang: -Math.PI / 2, target: null, pop: 1, cur: null, invested: s.invested || MT.rules.CAT[s.id].cost } : null;
        if (!c) return; if (onBoard) g.board.push(c); else g.bench.push(c);
      };
      (d.bench || []).forEach(s => build(s, false));
      (d.board || []).forEach(s => build(s, true));
      g.inventory = d.inventory || [];
      MT.api.recompute();
      return true;
    },
  };

  // ---------- DESAFIO DIÁRIO ----------
  function dayOfYear(dt) {
    const start = new Date(dt.getFullYear(), 0, 0);
    return Math.floor((dt - start) / 86400000);
  }
  const daily = {
    _seed() { const n = new Date(); return n.getFullYear() * 1000 + dayOfYear(n); },
    todaySeed() { return this._seed(); },
    todayKey() { const n = new Date(); return n.getFullYear() + '-' + dayOfYear(n); },
    mod() { return D.daily[this._seed() % D.daily.length]; },
    mapId() { const m = D.balance.dailyMaps; return m[this._seed() % m.length]; },
    name() { return this.mod().name; },
    desc() { return this.mod().desc; },
    apply(run) {
      const m = this.mod();
      run.coinMult = m.coin; run.spd = m.spd; run.hp = m.hp;
      run.livesBonus = m.lives; run.coinsBonus = m.coins; run.label = m.name;
    },
  };

  MT.progress = progress; MT.save = save; MT.daily = daily;
})(window.MT);
