// Núcleo da partida: estado, entidades e sistemas (ondas, combate, economia).
(function (MT) {
  'use strict';
  const U = MT.util, R = MT.rules, D = MT.DATA, B = MT.DATA.balance;

  function defaultRun() {
    return { coinMult: 1, spd: 1, hp: 1, livesBonus: 0, coinsBonus: 0,
      catDmg: 0, catRange: 0, catAtkSpd: 0, catCrit: 0, catArmorPen: 0, catMagicPen: 0,
      extraSlots: 0, coinsPerWave: 0, sellFull: false, label: '', takenRelics: [] };
  }

  const game = {
    mode: 'normal', mapId: 'jardim', map: null, lanes: [], laneLen: [],
    lives: 20, coins: 7, waveIndex: 0, phase: 'menu',
    shop: [], bench: [], board: [], inventory: [],
    enemies: [], shots: [], floats: [], parts: [],
    selected: null, run: defaultRun(), synergies: [],
    spawnQueue: [], spawnTimer: 0, spawnLane: 0, aliveCount: 0,
    removedThisWave: 0, enemiesTotal: 0, finishedSpawning: false, waveRunning: false,
    enemiesKilled: 0, shake: { amt: 0, t: 0, dur: 1 },
    banner: null, bannerT: 0, toast: '', toastT: 0, speed: 1,
    pendingDraft: null,
  };

  // ---------- MAPA / CAMINHOS ----------
  function resolveMap(mapId) {
    const m = D.maps.find(x => x.id === mapId) || D.maps[0];
    game.map = m; game.mapId = m.id;
    game.lanes = m.paths.map(lane => lane.map(p => ({ x: p.x, y: p.y })));
    game.laneLen = game.lanes.map(lane => {
      let L = 0; for (let i = 0; i < lane.length - 1; i++) L += U.dist(lane[i].x, lane[i].y, lane[i + 1].x, lane[i + 1].y);
      return L;
    });
  }
  // posição no caminho (unidades de mundo) a uma distância d
  function posAlong(laneIdx, d) {
    const lane = game.lanes[laneIdx];
    let rem = d;
    for (let i = 0; i < lane.length - 1; i++) {
      const seg = U.dist(lane[i].x, lane[i].y, lane[i + 1].x, lane[i + 1].y);
      if (rem <= seg || i === lane.length - 2) {
        const t = seg > 0 ? U.clamp(rem / seg, 0, 1) : 0;
        return { x: U.lerp(lane[i].x, lane[i + 1].x, t), y: U.lerp(lane[i].y, lane[i + 1].y, t),
          ang: Math.atan2(lane[i + 1].y - lane[i].y, lane[i + 1].x - lane[i].x) };
      }
      rem -= seg;
    }
    const last = lane[lane.length - 1];
    return { x: last.x, y: last.y, ang: 0 };
  }
  // menor distância (mundo) de um ponto a qualquer caminho
  function distToPaths(wx, wy) {
    let best = 1e9;
    for (const lane of game.lanes) {
      for (let i = 0; i < lane.length - 1; i++) {
        const a = lane[i], b = lane[i + 1];
        const dx = b.x - a.x, dy = b.y - a.y, l2 = dx * dx + dy * dy || 1;
        let t = ((wx - a.x) * dx + (wy - a.y) * dy) / l2; t = U.clamp(t, 0, 1);
        best = Math.min(best, U.dist(wx, wy, a.x + dx * t, a.y + dy * t));
      }
    }
    return best;
  }

  // ---------- GATOS ----------
  function makeCat(catId) {
    const data = R.CAT[catId];
    return { uid: U.uid(), data, items: [], x: 0, y: 0, cd: 0, ang: -Math.PI / 2,
      target: null, pop: 1, cur: null, invested: data.cost, priority: 'first' };
  }
  const PRIORITY_ORDER = ['first', 'last', 'strong', 'near'];
  function cyclePriority(cat) {
    if (!cat) return null;
    cat.priority = PRIORITY_ORDER[(PRIORITY_ORDER.indexOf(cat.priority) + 1) % PRIORITY_ORDER.length];
    MT.sfx && MT.sfx.play('place');
    saveNow();
    MT.ui && MT.ui.refresh && MT.ui.refresh();
    return cat.priority;
  }

  function recompute() {
    game.synergies = R.computeSynergies(game.board);
    for (const c of game.board) R.catStats(c, game.synergies, game.run);
    for (const c of game.bench) R.catStats(c, [], game.run); // bench: sem sinergia
    if (MT.stats) { MT.stats.max('maxCatsPlaced', game.board.length); MT.stats.max('maxSynergiesInMatch', game.synergies.filter(s => s.tier >= 0).length); }
    MT.ui && MT.ui.refresh && MT.ui.refresh();
  }

  // ---------- ECONOMIA ----------
  function addCoins(n) { if (n > 0) { n = Math.round(n * game.run.coinMult); MT.stats && MT.stats.add('coins', n); } game.coins += n; }
  function checkAch() {
    if (!MT.stats) return;
    const fresh = MT.stats.checkNew();
    if (fresh.length) { toast('🏆 ' + fresh[0].name); MT.sfx && MT.sfx.play('ach'); }
  }
  function canAfford(n) { return game.coins >= n; }

  function generateShop() {
    game.shop = [];
    for (let i = 0; i < B.shopSize; i++) game.shop.push(U.pick(D.cats).id);
  }
  // custo escala com o avanço das rondas (fica mais caro, mais desafiador).
  function costOf(data) { return data.cost + Math.floor(game.waveIndex / 4); }
  function buy(slot) {
    if (game.phase !== 'prep') return;
    const id = game.shop[slot]; if (!id) return;
    const cost = costOf(R.CAT[id]);
    if (!canAfford(cost)) { toast('Moedas insuficientes'); return; }
    if (game.bench.filter(Boolean).length >= B.benchSize) { toast('Banco cheio'); return; }
    game.coins -= cost;
    const c = makeCat(id); c.invested = cost; game.bench.push(c);
    game.shop[slot] = null;
    MT.stats && MT.stats.add('catsBought', 1);
    MT.sfx && MT.sfx.play('place');
    recompute(); saveNow();
  }
  function reroll() {
    if (game.phase !== 'prep') return;
    if (!canAfford(B.rerollCost)) { toast('Moedas insuficientes'); SFXerr(); return; }
    game.coins -= B.rerollCost; generateShop();
    MT.sfx && MT.sfx.play('coin'); MT.stats && MT.stats.add('rerolls', 1);
    recompute(); saveNow();
  }
  function SFXerr() { MT.sfx && MT.sfx.play('error'); }
  function sell(cat) {
    const ratio = game.run.sellFull ? 1 : B.sellRatio;
    game.coins += Math.floor(cat.invested * ratio); // valor fixo (venda não usa coinMult)
    for (const it of cat.items) game.inventory.push(it); // itens voltam ao inventário
    removeCat(cat);
    if (game.selected && game.selected.cat === cat) game.selected = null;
    recompute(); saveNow();
  }
  function removeCat(cat) {
    let i = game.board.indexOf(cat); if (i >= 0) game.board.splice(i, 1);
    i = game.bench.indexOf(cat); if (i >= 0) game.bench.splice(i, 1);
  }

  // ---------- COLOCAÇÃO ----------
  function pick(cat, from) { game.moveMode = false; game.selected = { kind: from, cat }; MT.ui && MT.ui.refresh && MT.ui.refresh(); }
  function deselect() { game.moveMode = false; game.selected = null; MT.ui && MT.ui.refresh && MT.ui.refresh(); }
  function placeValid(wx, wy, except) {
    except = except || (game.selected && game.selected.cat) || null;
    if (wx < -11.6 || wx > 11.6 || wy < -6.2 || wy > 6.2) return false;
    if (distToPaths(wx, wy) < 0.75) return false;
    for (const c of game.board) if (c !== except && U.dist(c.x, c.y, wx, wy) < 0.85) return false;
    return true;
  }
  function repositionCat(cat, wx, wy) { cat.x = wx; cat.y = wy; cat.pop = 0.7; recompute(); saveNow(); }
  function placeAt(wx, wy) {
    if (game.phase !== 'prep' || !game.selected) return;
    if (!placeValid(wx, wy)) { toast('Local inválido (perto do caminho/gato)'); SFXerr(); return; }
    if (game.board.length >= B.placementSlots && game.selected.kind === 'bench') { toast('Máximo de gatos no tabuleiro'); SFXerr(); return; }
    const cat = game.selected.cat;
    if (game.selected.kind === 'bench') { removeCat(cat); game.board.push(cat); }
    cat.x = wx; cat.y = wy; cat.pop = 0.6;
    game.selected = null;
    fxBurst(wx, wy, '#ffd24f', 8);
    MT.sfx && MT.sfx.play('place');
    recompute();
  }
  function selectBoard(cat) { if (game.phase === 'prep') pick(cat, 'board'); }

  // ---------- ONDAS ----------
  function startWave() {
    if (game.phase !== 'prep' || game.waveRunning) return;
    if (game.board.length === 0) { toast('Posicione ao menos um gato!'); SFXerr(); return; }
    const wave = R.waveForIndex(game.waveIndex, game.mode === 'endless');
    if (!wave) return;
    MT.sfx && MT.sfx.play('wave');
    game.selected = null;
    game.spawnQueue = [];
    for (const grp of wave.groups) for (let i = 0; i < grp.count; i++) game.spawnQueue.push({ enemy: grp.enemy, scale: grp.scale });
    game.enemiesTotal = game.spawnQueue.length;
    game.removedThisWave = 0; game.aliveCount = 0; game.finishedSpawning = false;
    game.spawnTimer = 0.2; game.spawnLane = 0; game.curWave = wave;
    game.waveRunning = true; game.phase = 'wave';
    banner('ONDA ' + (game.waveIndex + 1), waveDesc(wave));
    MT.ui && MT.ui.refresh && MT.ui.refresh();
  }
  function waveDesc(wave) {
    const byId = {}; wave.groups.forEach(g => byId[g.enemy] = (byId[g.enemy] || 0) + g.count);
    return Object.keys(byId).map(id => byId[id] + '× ' + R.ENEMY[id].name).join(' · ');
  }
  function spawnOne(entry) {
    const laneCount = game.lanes.length;
    const lane = laneCount > 0 ? (game.spawnLane % laneCount) : 0;
    game.spawnLane++;
    const st = R.enemyStats(entry.enemy, entry.scale, game.waveIndex, game.run);
    const p = posAlong(lane, 0);
    game.enemies.push({ ...st, hp: st.hpMax, laneIdx: lane, d: -U.rand() * 0.6, x: p.x, y: p.y, ang: p.ang,
      hit: 0, slowT: 0, slowAmt: 0 });
    game.aliveCount++;
    if (st.boss) {
      shakeCam(0.32, 0.5); MT.sfx && MT.sfx.play('boss');
      game.slowmoT = 1.1; // slow-mo cinematográfico
      MT.ui && MT.ui.showBossIntro && MT.ui.showBossIntro(st.data.name);
    }
  }
  function endWave() {
    game.waveRunning = false;
    const waveNumber = game.waveIndex + 1;
    const reward = R.waveReward(waveNumber, game.curWave ? game.curWave.bonusReward : 0, game.run);
    addCoins(reward);
    MT.sfx && MT.sfx.play('coin');
    MT.progress && MT.progress.onWave && MT.progress.onWave(waveNumber, game.enemiesKilled);
    if (MT.stats) { MT.stats.add('waves', 1); MT.stats.max('bestWave', waveNumber); MT.stats.add('synergiesActivated', game.synergies.filter(s => s.tier >= 0).length); }

    const wasLast = game.mode !== 'endless' && game.waveIndex >= R.TOTAL_WAVES - 1;
    if (wasLast) { win(); return; }
    game.waveIndex++;
    game.phase = 'prep';
    generateShop();
    recompute();
    banner('ONDA VENCIDA!  +' + reward + ' 🪙', 'Prepare a próxima defesa');
    if (game.mode === 'normal') MT.save && MT.save.write && MT.save.write();
    MT.ui && MT.ui.refresh && MT.ui.refresh();
    checkAch();

    // Drafts: RELÍQUIA nas ondas 5/15/25/35/45; ITEM nas ondas pares (sem colidir).
    game.pendingDraft = null;
    let offeredRelic = false;
    if (waveNumber % 10 === 5) {
      const rc = getRelicChoices(3);
      if (rc.length) { game.pendingDraft = { type: 'relic', choices: rc }; offeredRelic = true; }
    }
    if (!offeredRelic && B.itemDropEveryN > 0 && waveNumber % B.itemDropEveryN === 0) {
      const ic = getItemChoices(B.itemDraftChoices);
      if (ic.length) game.pendingDraft = { type: 'item', choices: ic };
    }
    if (game.pendingDraft) MT.ui && MT.ui.showDraft && MT.ui.showDraft();
  }

  // ---------- ITENS & RELÍQUIAS ----------
  function getItemChoices(n) {
    const pool = MT.util.shuffle(D.items);
    return pool.slice(0, n).map(it => it.id);
  }
  function maxSlots(cat) { return B.maxItemsPerCat + game.run.extraSlots; }
  function canEquip(cat) { return cat.items.length < maxSlots(cat); }
  function equipItem(cat, itemId) {
    if (!canEquip(cat)) { toast('Gato sem espaço de item'); return false; }
    const idx = game.inventory.indexOf(itemId);
    if (idx < 0) return false;
    game.inventory.splice(idx, 1);
    cat.items.push(itemId);
    MT.stats && MT.stats.add('itemsEquipped', 1);
    recompute(); saveNow();
    return true;
  }
  function takeItem(itemId) { game.inventory.push(itemId); game.pendingDraft = null; recompute(); saveNow(); }

  const RELIC_FX = {
    claws: r => r.catDmg += 25, eagle: r => r.catRange += 25, reflex: r => r.catAtkSpd += 25,
    instinct: r => r.catCrit += 20, piercer: r => r.catArmorPen += 40, rune: r => r.catMagicPen += 40,
    backpack: r => r.extraSlots += 1, purse: r => r.coinsPerWave += 4, merchant: r => r.sellFull = true,
    luck: r => r.coinMult *= 1.3, ward: r => r.hp *= 0.9,
    heart: r => { game.lives += 5; if (GM_lives) GM_lives(); }, treasure: r => { game.coins += 15; },
  };
  function GM_lives() { MT.ui && MT.ui.refresh && MT.ui.refresh(); }
  function getRelicChoices(n) {
    const avail = D.relics.filter(r => game.run.takenRelics.indexOf(r.id) < 0);
    return MT.util.shuffle(avail).slice(0, n).map(r => r.id);
  }
  function takeRelic(relicId) {
    const fx = RELIC_FX[relicId];
    if (fx) fx(game.run);
    game.run.takenRelics.push(relicId);
    game.pendingDraft = null;
    recompute(); saveNow();
  }

  // ---------- COMBATE ----------
  function killEnemy(en) {
    const i = game.enemies.indexOf(en); if (i < 0) return;
    game.enemies.splice(i, 1);
    game.aliveCount = Math.max(0, game.aliveCount - 1);
    game.removedThisWave++;
    addCoins(Math.max(1, Math.round(en.bounty * 0.5))); // recompensa por abate reduzida (economia mais apertada)
    game.enemiesKilled++;
    if (MT.stats) { MT.stats.add('kills', 1); if (en.boss) MT.stats.add('bossKills', 1); }
    fxBurst(en.x, en.y, en.boss ? '#ffd24f' : '#b98cff', en.boss ? 24 : 7);
    if (en.boss) banner('CHEFE DERROTADO! 👑', '+' + en.bounty + ' 🪙');
    checkWaveEnd();
  }
  function leak(en) {
    const i = game.enemies.indexOf(en); if (i < 0) return;
    game.enemies.splice(i, 1);
    game.aliveCount = Math.max(0, game.aliveCount - 1);
    game.removedThisWave++;
    game.lives -= en.leak; if (game.lives < 0) game.lives = 0;
    shakeCam(0.15, 0.25);
    if (game.lives <= 0) { lose(); return; }
    checkWaveEnd();
  }
  function damageEnemy(en, cat) {
    const h = R.computeHit(cat, en);
    en.hp -= h.dmg; en.hit = 0.14;
    cat.dmgDealt = (cat.dmgDealt || 0) + h.dmg;
    addFloat(en.x, en.y - 0.35, Math.round(h.dmg), cat.cur.type, h.crit);
    if (en.hp > 0 && cat.cur.truePerHit > 0) {
      en.hp -= cat.cur.truePerHit; cat.dmgDealt += cat.cur.truePerHit; addFloat(en.x + 0.15, en.y - 0.15, cat.cur.truePerHit, 'true', false);
    }
    if (cat.cur.slow > 0) { en.slowT = cat.cur.slowDur; en.slowAmt = cat.cur.slow; }
    if (en.hp <= 0) killEnemy(en);
  }
  function hitTarget(cat, target) {
    if (!game.enemies.includes(target)) return;
    if (cat.cur.area > 0) {
      const hx = target.x, hy = target.y;
      const inRange = game.enemies.filter(e => U.dist(e.x, e.y, hx, hy) <= cat.cur.area);
      for (const e of inRange) damageEnemy(e, cat);
    } else {
      damageEnemy(target, cat);
    }
  }

  // ---------- FX ----------
  function addFloat(wx, wy, val, type, crit) { game.floats.push({ x: wx, y: wy, val, type, crit, life: 0.8 }); }
  function fxBurst(wx, wy, color, n) {
    for (let i = 0; i < n; i++) {
      const a = U.rand() * Math.PI * 2, s = 0.6 + U.rand() * 1.8;
      game.parts.push({ x: wx, y: wy, vx: Math.cos(a) * s, vy: Math.sin(a) * s + 0.6, life: 0.5 + U.rand() * 0.4, color });
    }
  }
  function shakeCam(amt, dur) { game.shake.amt = amt; game.shake.dur = dur; game.shake.t = dur; }
  function banner(t1, t2) { game.banner = { t1, t2 }; game.bannerT = 1.8; }
  function toast(m) { game.toast = m; game.toastT = 1.5; }

  // ---------- FIM ----------
  // só o modo Normal tem save; limpar em outros modos apagaria o progresso Normal.
  function clearSaveIfNormal() { if (game.mode === 'normal') MT.save && MT.save.clear && MT.save.clear(); }
  function win() {
    game.phase = 'win'; recordBest(); clearSaveIfNormal();
    MT.stats && MT.stats.add('wins', 1); MT.sfx && MT.sfx.play('win'); checkAch();
    MT.ui && MT.ui.showEnd && MT.ui.showEnd(true);
  }
  function lose() {
    if (game.phase === 'over' || game.phase === 'win') return; // evita disparo múltiplo no mesmo frame
    game.phase = 'over'; game.waveRunning = false; shakeCam(0.4, 0.5);
    recordBest(); clearSaveIfNormal();
    MT.sfx && MT.sfx.play('lose'); checkAch();
    MT.ui && MT.ui.showEnd && MT.ui.showEnd(false);
  }
  function recordBest() { MT.progress && MT.progress.recordBest && MT.progress.recordBest(game.waveIndex + 1); }

  function checkWaveEnd() {
    if (!game.waveRunning || !game.finishedSpawning) return;
    if (game.aliveCount > 0) return;
    if (game.phase === 'over') return;
    endWave();
  }

  // ---------- UPDATE ----------
  function update(dt, dtReal) {
    if (game.bannerT > 0) game.bannerT -= dtReal;
    if (game.toastT > 0) game.toastT -= dtReal;
    if (game.shake.t > 0) game.shake.t -= dtReal;
    if (game.phase === 'menu' || game.phase === 'over' || game.phase === 'win') { updateFx(dtReal); return; }

    // spawn
    if (game.waveRunning && game.spawnQueue.length) {
      game.spawnTimer -= dt;
      if (game.spawnTimer <= 0) { spawnOne(game.spawnQueue.shift()); game.spawnTimer = game.curWave.spawnInterval; }
    } else if (game.waveRunning && !game.finishedSpawning && game.spawnQueue.length === 0) {
      game.finishedSpawning = true; checkWaveEnd();
    }

    // inimigos
    for (let i = game.enemies.length - 1; i >= 0; i--) {
      const en = game.enemies[i];
      if (en.hit > 0) en.hit -= dt;
      let spd = en.speed;
      if (en.slowT > 0) { en.slowT -= dt; spd *= (1 - en.slowAmt); }
      en.d += spd * dt;
      if (en.d < 0) { const p = posAlong(en.laneIdx, 0); en.x = p.x; en.y = p.y; continue; }
      if (en.d >= game.laneLen[en.laneIdx]) { leak(en); continue; }
      const p = posAlong(en.laneIdx, en.d);
      let da = p.ang - en.ang; while (da > Math.PI) da -= 2 * Math.PI; while (da < -Math.PI) da += 2 * Math.PI;
      en.ang += da * Math.min(1, dt * 10);
      en.x = p.x; en.y = p.y;
    }

    // gatos: mira + tiro
    for (const cat of game.board) {
      if (cat.pop < 1) cat.pop = Math.min(1, cat.pop + dt * 4);
      cat.cd -= dt;
      // Seleção de alvo conforme a PRIORIDADE do gato.
      let best = null, metric = 0;
      for (const en of game.enemies) {
        if (en.d < 0) continue;
        const dd = U.dist(cat.x, cat.y, en.x, en.y);
        if (dd > cat.cur.range) continue;
        let m;
        switch (cat.priority) {
          case 'last': m = -en.d; break;          // mais atrás (recém-chegado)
          case 'strong': m = en.hp; break;         // maior vida
          case 'near': m = -dd; break;             // mais próximo do gato
          default: m = en.d;                       // 'first': mais à frente (perto da base)
        }
        if (best === null || m > metric) { best = en; metric = m; }
      }
      cat.target = best;
      if (best) {
        const ta = Math.atan2(best.y - cat.y, best.x - cat.x);
        let da = ta - cat.ang; while (da > Math.PI) da -= 2 * Math.PI; while (da < -Math.PI) da += 2 * Math.PI;
        cat.ang += da * Math.min(1, dt * 12);
        if (cat.cd <= 0) { fire(cat, best); cat.cd = cat.cur.interval; cat.pop = 0.75; }
      }
    }

    // projéteis
    for (let i = game.shots.length - 1; i >= 0; i--) {
      const s = game.shots[i];
      s.life -= dt;
      const alive = game.enemies.includes(s.target);
      const tx = alive ? s.target.x : s.tx, ty = alive ? s.target.y : s.ty;
      s.tx = tx; s.ty = ty;
      const dx = tx - s.x, dy = ty - s.y, d = Math.hypot(dx, dy), step = s.speed * dt;
      if (d <= step + 0.12) {
        if (alive) hitTarget(s.cat, s.target); else fxBurst(s.x, s.y, s.color, 3);
        game.shots.splice(i, 1); continue;
      }
      s.x += dx / d * step; s.y += dy / d * step;
      if (s.life <= 0) game.shots.splice(i, 1);
    }
    updateFx(dtReal);
  }
  function fire(cat, target) {
    const color = cat.cur.type === 'phys' ? '#ff9a4d' : cat.cur.type === 'magic' ? '#b98cff' : '#ffe8b0';
    game.shots.push({ x: cat.x, y: cat.y, tx: target.x, ty: target.y, target, cat,
      speed: 15, color, type: cat.cur.type, life: 1.4 });
  }
  function updateFx(dtReal) {
    for (let i = game.floats.length - 1; i >= 0; i--) { const f = game.floats[i]; f.y += 0.9 * dtReal; f.life -= dtReal; if (f.life <= 0) game.floats.splice(i, 1); }
    for (let i = game.parts.length - 1; i >= 0; i--) { const p = game.parts[i]; p.life -= dtReal; p.vy -= 4 * dtReal; p.x += p.vx * dtReal; p.y += p.vy * dtReal; if (p.life <= 0) game.parts.splice(i, 1); }
  }

  // ---------- NOVA RUN ----------
  function newRun(mode, mapId, opts) {
    opts = opts || {};
    game.mode = mode || 'normal';
    if (!opts.restoring) MT.stats && MT.stats.add('games', 1);
    MT.sfx && MT.sfx.resume();
    game.run = defaultRun();
    if (game.mode === 'daily') MT.daily && MT.daily.apply && MT.daily.apply(game.run);
    resolveMap(game.mode === 'daily' && MT.daily ? MT.daily.mapId() : (mapId || 'jardim'));
    game.lives = Math.max(1, B.startingLives + game.run.livesBonus);
    game.coins = B.startingCoins + game.run.coinsBonus;
    game.waveIndex = 0; game.enemiesKilled = 0;
    game.bench = []; game.board = []; game.inventory = [];
    game.enemies = []; game.shots = []; game.floats = []; game.parts = [];
    game.selected = null; game.phase = 'prep'; game.pendingDraft = null;
    // zera TODO o estado de onda (senão startWave pode ficar travado por waveRunning obsoleto)
    game.waveRunning = false; game.finishedSpawning = false; game.spawnQueue = [];
    game.aliveCount = 0; game.removedThisWave = 0; game.enemiesTotal = 0; game.spawnTimer = 0; game.curWave = null;
    game.freshRun = true; // faz o HUD ignorar o delta de moedas da run anterior
    if (MT.cam) MT.cam.reset();
    generateShop(); recompute();
    if (opts.restoring) return; // Continuar restaura o estado real depois; sem banner nova-run
    if (game.mode === 'daily' && MT.daily) banner('📅 ' + MT.daily.name(), MT.daily.desc());
    else banner('DEFENDA O REINO 🏰', 'Compre um gato e posicione no gramado');
  }
  function saveNow() { if (game.mode === 'normal') MT.save && MT.save.write && MT.save.write(); }

  MT.game = game;
  MT.api = {
    newRun, update, buy, reroll, sell, pick, deselect, placeAt, selectBoard,
    startWave, recompute, resolveMap, distToPaths, placeValid, posAlong, addCoins,
    takeItem, takeRelic, equipItem, canEquip, maxSlots, getItemChoices, getRelicChoices,
    cyclePriority, repositionCat, costOf,
  };
})(window.MT);
