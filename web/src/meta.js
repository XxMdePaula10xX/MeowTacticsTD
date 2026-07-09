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
      const enc = (c) => ({ id: c.data.id, x: c.x, y: c.y, items: c.items.slice(), invested: c.invested, priority: c.priority });
      const d = { mapId: g.mapId, coins: g.coins, lives: g.lives, waveIndex: g.waveIndex,
        board: g.board.map(enc), bench: g.bench.map(enc), inventory: g.inventory.slice(), run: g.run };
      set(SKEY, JSON.stringify(d));
    },
    restore() {
      const raw = get(SKEY, null); if (!raw) return false;
      let d; try { d = JSON.parse(raw); } catch (e) { return false; }
      const g = MT.game;
      MT.api.newRun('normal', d.mapId, { restoring: true });
      if (d.run) Object.assign(g.run, d.run);
      g.coins = d.coins; g.lives = d.lives; g.waveIndex = d.waveIndex;
      g.bench = []; g.board = [];
      const build = (s, onBoard) => {
        const c = MT.game && MT.rules.CAT[s.id] ? { uid: MT.util.uid(), data: MT.rules.CAT[s.id], items: s.items || [],
          x: s.x, y: s.y, cd: 0, ang: -Math.PI / 2, target: null, pop: 1, cur: null, invested: s.invested || MT.rules.CAT[s.id].cost, priority: s.priority || 'first' } : null;
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

  // ---------- ESTATÍSTICAS & CONQUISTAS ----------
  const stats = {
    get(m) { return parseInt(get('mt_stat_' + m, '0'), 10) || 0; },
    add(m, n) { set('mt_stat_' + m, String(this.get(m) + (n == null ? 1 : n))); },
    max(m, n) { if (n > this.get(m)) set('mt_stat_' + m, String(n)); },
    unlocked(a) { return this.get(a.metric) >= a.threshold; },
    unlockedCount() { return D.achievements.filter(a => this.unlocked(a)).length; },
    // Retorna conquistas recém-desbloqueadas (para exibir toast).
    checkNew() {
      let seen; try { seen = new Set(JSON.parse(get('mt_ach_seen', '[]'))); } catch (e) { seen = new Set(); }
      const now = D.achievements.filter(a => this.unlocked(a)).map(a => a.id);
      const fresh = now.filter(id => !seen.has(id));
      if (fresh.length) set('mt_ach_seen', JSON.stringify(now));
      return fresh.map(id => D.achievements.find(a => a.id === id));
    },
  };

  MT.progress = progress; MT.save = save; MT.daily = daily; MT.stats = stats;
})(window.MT);
