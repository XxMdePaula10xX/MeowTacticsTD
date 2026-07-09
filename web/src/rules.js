// Regras puras do jogo, portadas 1:1 do C#: dano (triângulo), sinergias,
// buffs, geração de ondas (1-10 na mão, 11-50 procedural) e escalonamento.
(function (MT) {
  'use strict';
  const D = MT.DATA;
  const B = D.balance;

  // Índices rápidos por id
  const CAT = {}; D.cats.forEach(c => CAT[c.id] = c);
  const ENEMY = {}; D.enemies.forEach(e => ENEMY[e.id] = e);
  const ITEM = {}; D.items.forEach(i => ITEM[i.id] = i);
  const SYN = {}; D.synergies.forEach(s => SYN[s.id] = s);
  // tag -> definição de sinergia
  const SYN_BY_TAG = {}; D.synergies.forEach(s => SYN_BY_TAG[s.tag] = s);

  // ---------- ONDAS ----------
  // Constrói a onda de número n (1-based) com grupos {enemy,count,scale}.
  function buildWave(n) {
    if (n <= D.waves.length) {
      const w = D.waves[n - 1];
      return { n, spawnInterval: w.spawnInterval, boss: false,
        groups: w.groups.map(g => ({ enemy: g.enemy, count: g.count, scale: g.scale })) };
    }
    // Procedural 11-50
    const big = 8 + Math.floor(n / 4);
    const mid = 5 + Math.floor(n / 6);
    const small = 3 + Math.floor(n / 8);
    let interval = Math.max(0.35, 0.72 - 0.006 * (n - 11));
    let groups = [];
    switch (n % 5) {
      case 0: groups = [g('swift', big + 6), g('ghostling', mid)]; break;
      case 1: groups = [g('armored', big), g('bulwark', small), g('swift', mid)]; break;
      case 2: groups = [g('shadow', big), g('wraith', mid), g('swift', small)]; break;
      case 3: groups = [g('warden', mid), g('bulwark', small), g('wraith', mid)]; break;
      case 4: groups = [g('armored', mid), g('shadow', mid), g('swift', mid), g('warden', small)]; break;
    }
    let boss = false;
    if (n % 10 === 0) groups.unshift(g('king', 1, 0.38 + 0.05 * Math.floor(n / 10)));
    if (n === 50) { groups = [g('king', 1, 1.15), g('warden', 12), g('bulwark', 8), g('swift', 14)]; boss = true; interval = 0.486; }
    return { n, spawnInterval: interval, boss, groups };
  }
  function g(enemy, count, scale) { return { enemy, count, scale: scale == null ? 1 : scale }; }

  // Resolve a onda para um índice (com ciclagem no Infinito).
  function waveForIndex(idx, endless) {
    const total = D.waves.length; // ondas autorais até 50 (usamos buildWave para todas)
    const MAXN = 50;
    if (idx < MAXN) return buildWave(idx + 1);
    if (endless) {
      const span = Math.min(10, MAXN);
      const cycled = MAXN - span + (idx % span); // 40..49 -> ondas 41..50
      return buildWave(cycled + 1);
    }
    return null;
  }
  const TOTAL_WAVES = 50;

  // ---------- ESCALONAMENTO DE INIMIGOS ----------
  // w = índice de onda 0-based (cresce sem limite no Infinito).
  function enemyStats(enemyId, groupScale, w, run) {
    const e = ENEMY[enemyId];
    const hpScale = groupScale * (1 + 0.14 * w) * (run.hp || 1);
    const defScale = groupScale * (w >= 3 ? 1 + 0.04 * (w - 2) : 1);
    const spdScale = Math.min(1.3, 1 + 0.03 * w);
    return {
      data: e,
      hpMax: Math.round(e.hp * hpScale),
      armor: e.armor * defScale,
      mr: e.mr * defScale,
      speed: e.speed * spdScale * (run.spd || 1) * B.enemySpeedScale,
      bounty: e.bounty, leak: e.leak, boss: e.boss,
    };
  }

  // ---------- SINERGIAS ----------
  // Conta gatos ÚNICOS por tag no tabuleiro (emblemas de item concedem tag).
  function computeSynergies(board) {
    const count = {};
    for (const cat of board) {
      const tags = new Set(cat.data.tags);
      for (const it of cat.items) {
        const item = ITEM[it]; if (!item) continue;
        for (const k in item.eff) {
          const m = /^grantsSynergy_(.+)$/.exec(k);
          if (m) tags.add(m[1]);
        }
      }
      for (const t of tags) count[t] = (count[t] || 0) + 1;
    }
    const active = [];
    for (const tag in count) {
      const def = SYN_BY_TAG[tag]; if (!def) continue;
      const c = count[tag];
      let tier = -1;
      for (let i = 0; i < def.tiers.length; i++) if (c >= def.tiers[i].need) tier = i;
      active.push({ id: def.id, name: def.name, tag, count: c, tier,
        need: def.tiers.map(t => t.need), tiers: def.tiers,
        buffs: tier >= 0 ? def.tiers[tier].buffs : {},
        label: tier >= 0 ? def.tiers[tier].label : '' });
    }
    // ativos primeiro, depois por contagem desc
    active.sort((a, b) => (b.tier - a.tier) || (b.count - a.count));
    return active;
  }

  // ---------- STATS FINAIS DE UM GATO ----------
  // Acumula base + sinergias (por tag) + itens + relíquias (run) -> cat.cur.
  function catStats(cat, active, run) {
    const base = cat.data;
    const buff = { dmg: 0, atkSpeed: 0, range: 0, crit: 0, armorPen: 0, magicPen: 0, truePerHit: 0 };
    let area = base.area, slow = base.slow, slowDur = base.slowDur;

    // tags efetivas (com emblemas)
    const tags = new Set(base.tags);
    for (const it of cat.items) {
      const item = ITEM[it]; if (!item) continue;
      for (const k in item.eff) { const m = /^grantsSynergy_(.+)$/.exec(k); if (m) tags.add(m[1]); }
    }
    // sinergias que este gato carrega
    for (const s of active) {
      if (s.tier < 0) continue;
      if (!tags.has(s.tag)) continue;
      const bf = s.buffs;
      if (bf.dmg) buff.dmg += bf.dmg;
      if (bf.atkSpeed) buff.atkSpeed += bf.atkSpeed;
      if (bf.range) buff.range += bf.range;
      if (bf.crit) buff.crit += bf.crit;
      if (bf.armorPen) buff.armorPen += bf.armorPen;
      if (bf.magicPen) buff.magicPen += bf.magicPen;
    }
    // itens
    for (const it of cat.items) {
      const item = ITEM[it]; if (!item) continue;
      const e = item.eff;
      if (e.DamagePercent) buff.dmg += e.DamagePercent;
      if (e.AttackSpeedPercent) buff.atkSpeed += e.AttackSpeedPercent;
      if (e.RangePercent) buff.range += e.RangePercent;
      if (e.CritChancePercent) buff.crit += e.CritChancePercent;
      if (e.ArmorPenetrationFlat) buff.armorPen += e.ArmorPenetrationFlat;
      if (e.MagicPenetrationFlat) buff.magicPen += e.MagicPenetrationFlat;
      if (e.bonusTrueDamagePerHit) buff.truePerHit += e.bonusTrueDamagePerHit;
      if (e.grantsArea) area = Math.max(area, e.areaRadius || 0);
      if (e.grantsSlow) { slow = Math.max(slow, e.slowAmount || 0); slowDur = Math.max(slowDur, e.slowDuration || 0); }
    }
    // relíquias (RunMods) — pontos percentuais / flat
    buff.dmg += run.catDmg || 0;
    buff.range += run.catRange || 0;
    buff.atkSpeed += run.catAtkSpd || 0;
    buff.crit += run.catCrit || 0;
    buff.armorPen += run.catArmorPen || 0;
    buff.magicPen += run.catMagicPen || 0;

    cat.cur = {
      dmg: base.dmg * (1 + buff.dmg / 100),
      range: base.range * (1 + buff.range / 100),
      interval: base.interval / (1 + buff.atkSpeed / 100),
      crit: MT.util.clamp(base.crit + buff.crit, 0, 100),
      armorPen: buff.armorPen,
      magicPen: buff.magicPen,
      truePerHit: base.truePerHit + buff.truePerHit,
      type: base.type, area, slow, slowDur,
    };
    return cat.cur;
  }

  // ---------- DANO (triângulo) ----------
  // Retorna { dmg, crit } aplicando tipo + penetração + crítico.
  function computeHit(cat, enemy) {
    let dmg = cat.cur.dmg;
    let crit = false;
    if (cat.cur.crit > 0 && MT.util.rand() * 100 < cat.cur.crit) { dmg *= B.critMult; crit = true; }
    if (cat.cur.type === 'phys') {
      const armor = Math.max(0, enemy.armor - cat.cur.armorPen);
      dmg *= 100 / (100 + armor);
    } else if (cat.cur.type === 'magic') {
      const mr = Math.max(0, enemy.mr - cat.cur.magicPen);
      dmg *= 100 / (100 + mr);
    } // 'true' ignora defesas
    return { dmg: Math.max(1, dmg), crit };
  }

  // Recompensa de onda (mesma fórmula em todos os modos).
  function waveReward(waveNumber, bonusReward, run) {
    return B.coinsPerWaveBase + Math.floor(waveNumber / 2) + (bonusReward || 0) + (run.coinsPerWave || 0);
  }

  MT.rules = {
    CAT, ENEMY, ITEM, SYN, SYN_BY_TAG,
    buildWave, waveForIndex, TOTAL_WAVES, enemyStats,
    computeSynergies, catStats, computeHit, waveReward,
  };
})(window.MT);
