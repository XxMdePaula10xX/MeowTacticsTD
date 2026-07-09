// Interface em DOM: HUD, loja, banco, sinergias, seleção e telas.
(function (MT) {
  'use strict';
  const U = MT.util, R = MT.rules, D = MT.DATA, B = MT.DATA.balance;
  const SYM = { phys: '▲', magic: '★', 'true': '◆' };
  const ITEM_ICON = { claw: '🗡️', paw: '🐾', scope: '🔭', tiger: '🐯', piercer: '🔩', rune: '🔮', phantom: '👻',
    frost: '❄️', herb: '💣', emblem_ninja: '🥷', emblem_sniper: '🎯', emblem_mystic: '✨' };
  const RELIC_ICON = { claws: '⚔️', eagle: '🦅', reflex: '🐆', instinct: '🗡️', piercer: '🔩', rune: '📜', backpack: '🎒',
    purse: '💰', merchant: '🤝', luck: '🍀', heart: '❤️', treasure: '💎', ward: '🛡️' };
  const TAG_ICON = { Ninja: '🥷', Shadow: '🌑', Assassin: '🗡️', Hunter: '🏹', Forest: '🌲', Adventurer: '🧭',
    Sniper: '🎯', Technology: '⚙️', Mystic: '🔮', Elemental: '✨', Support: '💗', Guardian: '🛡️', Star: '⭐' };
  const ITEM = {}; MT.DATA.items.forEach(i => ITEM[i.id] = i);
  const RELIC = {}; MT.DATA.relics.forEach(r => RELIC[r.id] = r);
  const SYND = {}; MT.DATA.synergies.forEach(s => SYND[s.id] = s);
  const CATD = {}; MT.DATA.cats.forEach(c => CATD[c.id] = c);
  const $ = (id) => document.getElementById(id);
  function el(tag, cls, html) { const e = document.createElement(tag); if (cls) e.className = cls; if (html != null) e.innerHTML = html; return e; }
  function asset(p) { return MT.assetURL ? MT.assetURL(p) : p; }

  let refs = {};
  let collTab = 'cats';

  function build() {
    refs = {
      lives: $('lives'), coins: $('coins'), wave: $('wave'),
      shop: $('shop'), bench: $('bench'), syn: $('synPanel'), sel: $('selPanel'),
      hint: $('placeHint'), banner: $('banner'), toast: $('toast'), relicStrip: $('relicStrip'),
      start: $('startBtn'), reroll: $('rerollBtn'), speed: $('speedBtn'),
    };
    refs.start.addEventListener('click', () => MT.api.startWave());
    refs.reroll.addEventListener('click', () => MT.api.reroll());
    refs.speed.addEventListener('click', () => { const g = MT.game; g.speed = g.speed === 1 ? 2 : g.speed === 2 ? 3 : 1; refs.speed.textContent = '⏩ ' + g.speed + '×'; });

    // ícones de HUD (arte real)
    const life = $('icoLife'), coin = $('icoCoin');
    if (life) life.src = asset('assets/ui/ui_vida.png');
    if (coin) coin.src = asset('assets/ui/ui_moeda.png');
    // controles de zoom
    const zc = MT.cam;
    const bz = (id, fn) => { const b = $(id); if (b) b.addEventListener('click', fn); };
    bz('zoomIn', () => zc.zoomBy(1.25));
    bz('zoomOut', () => zc.zoomBy(0.8));
    bz('zoomReset', () => zc.reset());

    // menu
    $('btnNormal').addEventListener('click', () => showMapSelect('normal'));
    $('btnEndless').addEventListener('click', () => showMapSelect('endless'));
    $('btnDaily').addEventListener('click', () => startRun('daily', null));
    $('btnCollection').addEventListener('click', showCollection);
    $('btnSound').addEventListener('click', toggleSound);
    const cont = $('btnContinue');
    if (MT.save.has()) { cont.style.display = ''; cont.addEventListener('click', continueRun); }
    // coleção
    $('collBack').addEventListener('click', () => { hide('collection'); showMenu(); });
    [...$('collTabs').children].forEach(t => t.addEventListener('click', () => selectCollTab(t.dataset.tab)));
    // style guide
    $('btnStyle').addEventListener('click', showStyleGuide);
    $('sgBack').addEventListener('click', () => { hide('styleguide'); showMenu(); });
    // fim
    $('btnAgain').addEventListener('click', () => { hide('endScreen'); showMenu(); });
    $('btnReplay').addEventListener('click', () => { hide('endScreen'); MT.api.newRun(MT.game.mode, MT.game.mapId); refresh(); });
    $('btnMaps').addEventListener('click', () => { hide('endScreen'); showMapSelect(MT.game.mode === 'daily' ? 'normal' : MT.game.mode); });
    updateSoundLabel();
    // tooltips (hover no desktop; toque fixa por alguns segundos)
    document.addEventListener('mouseover', e => { const n = e.target.closest && e.target.closest('[data-tip]'); if (n) { unpinTip(); const r = n.getBoundingClientRect(); tipShow(resolveTip(n), r.left + r.width / 2, r.bottom); } });
    document.addEventListener('mouseout', e => { if (e.target.closest && e.target.closest('[data-tip]')) tipHide(); });
  }

  // ---------- TELAS ----------
  function hide(id) { $(id).classList.add('hide'); }
  function show(id) { $(id).classList.remove('hide'); }
  function showMenu() {
    MT.game.phase = 'menu';
    const m = $('menu');
    m.style.backgroundImage = 'url(' + asset('assets/ui/menu_bg.png') + ')';
    show('menu'); hide('mapSelect'); hide('endScreen');
    const c = $('btnContinue'); c.style.display = MT.save.has() ? '' : 'none';
    // recordes no menu
    const rec = $('menuRecords');
    if (rec) rec.textContent = 'Melhor (Jardim): onda ' + MT.progress.bestFor('normal', 'jardim');
  }
  function showMapSelect(mode) {
    hide('menu');
    const wrap = $('mapList'); wrap.innerHTML = '';
    D.maps.forEach(m => {
      const best = MT.progress.bestFor(mode, m.id);
      const card = el('div', 'map-card');
      card.innerHTML =
        '<div class="map-thumb" style="background-image:url(' + asset(m.bg) + ')"></div>' +
        '<div class="map-info"><b>' + m.name + '</b><span>' + laneLabel(m.lanes) + '</span>' +
        '<span class="map-best">Recorde: onda ' + best + '</span></div>';
      card.addEventListener('click', () => startRun(mode, m.id));
      wrap.appendChild(card);
    });
    $('mapTitle').textContent = mode === 'endless' ? 'INFINITO — escolha o mapa' : 'NOVO JOGO — escolha o mapa';
    show('mapSelect');
    $('mapBack').onclick = showMenu;
  }
  function laneLabel(n) { return n === 1 ? '1 caminho · Fácil' : n === 2 ? '2 caminhos · Médio' : '3 caminhos · Difícil'; }

  function startRun(mode, mapId) {
    hide('menu'); hide('mapSelect'); hide('endScreen');
    MT.api.newRun(mode, mapId);
    if (mode === 'daily') MT.game.speed = MT.game.speed || 1;
    refresh();
  }
  function continueRun() { hide('menu'); hide('mapSelect'); MT.save.restore(); refresh(); }

  function showEnd(won) {
    const g = MT.game;
    const recBest = MT.progress.bestFor(g.mode, g.mapId);
    $('endLogo').textContent = won ? '🏆' : '💀';
    $('endTitle').textContent = won ? 'VITÓRIA!' : (g.mode === 'endless' ? 'FIM DA JORNADA' : 'DERROTA');
    $('endSub').textContent = won ? 'Você repeliu todos os pesadelos.' : 'Os pesadelos tomaram o reino.';
    $('endStats').innerHTML =
      stat(g.waveIndex + (won ? 1 : 0), 'Ondas') + stat(g.enemiesKilled, 'Abates') +
      stat(g.lives, 'Vidas') + stat(recBest, 'Recorde');
    let ex = '';
    const best = g.board.slice().sort((a, b) => (b.dmgDealt || 0) - (a.dmgDealt || 0))[0];
    if (best && best.dmgDealt) ex += '<div class="end-best"><img src="' + asset(best.data.sprite) + '"><div><b>' + best.data.name + '</b><br><span>' + MT.util.fmt(Math.round(best.dmgDealt)) + ' de dano · melhor gato</span></div></div>';
    const syn = g.synergies.filter(s => s.tier >= 0).map(s => (TAG_ICON[s.tag] || '') + ' ' + s.name);
    if (syn.length) ex += '<div class="end-synrow">Sinergias ativas: ' + syn.join(' · ') + '</div>';
    $('endExtra').innerHTML = ex;
    show('endScreen');
  }
  function stat(v, label) { return '<div class="st"><b>' + U.fmt(v) + '</b><span>' + label + '</span></div>'; }

  // ---------- REFRESH ----------
  let lastCoins = null, lastLives = null;
  function refresh() {
    const g = MT.game;
    if (g.phase === 'menu') return;
    if (g.freshRun) { lastCoins = null; lastLives = null; g.freshRun = false; } // run nova: sem delta falso
    // feedback de economia (pop + flutuante +X/-X)
    if (lastCoins != null && g.coins !== lastCoins) { bumpChip('coins'); coinFloat(g.coins - lastCoins); }
    if (lastLives != null && g.lives !== lastLives && g.lives < lastLives) bumpChip('lives');
    lastCoins = g.coins; lastLives = g.lives;
    refs.lives.textContent = g.lives;
    refs.coins.textContent = g.coins;
    refs.wave.textContent = (g.waveIndex + 1) + (g.mode === 'endless' ? ' ∞' : '/' + R.TOTAL_WAVES);
    // botão iniciar (estados)
    const running = g.phase === 'wave';
    refs.start.disabled = running;
    refs.start.classList.toggle('running', running);
    refs.start.textContent = running ? '🌊 ONDA EM ANDAMENTO' : '⚔️ INICIAR ONDA';
    refs.reroll.textContent = '🎲 ' + B.rerollCost;
    refs.reroll.disabled = running || g.coins < B.rerollCost;
    refs.hint.classList.toggle('show', !!(g.selected && g.selected.kind === 'bench'));
    const dock = document.querySelector('.dock');
    if (dock) { dock.classList.toggle('placing', !!(g.selected && g.selected.kind === 'bench')); dock.classList.toggle('wave-lock', running); }
    const sh = $('shopHint'); if (sh) sh.textContent = '· reroll ' + B.rerollCost + '🪙';

    buildShop(); buildBench(); buildSyn(); buildSel(); buildRelicStrip();
  }
  function bumpChip(cls) { const c = document.querySelector('.chip.' + cls); if (!c) return; c.classList.remove('bump'); void c.offsetWidth; c.classList.add('bump'); }
  function coinFloat(delta) {
    const wrap = $('coinFloat'); if (!wrap || !delta) return;
    const s = el('span', delta > 0 ? 'gain' : 'loss', (delta > 0 ? '+' : '') + delta + ' 🪙');
    wrap.appendChild(s); setTimeout(() => s.remove(), 1000);
  }

  function buildRelicStrip() {
    const g = MT.game, wrap = refs.relicStrip; if (!wrap) return;
    wrap.innerHTML = '';
    for (const id of g.run.takenRelics) {
      const r = RELIC[id];
      const c = el('div', 'relic', RELIC_ICON[id] || '🏺');
      c.title = r ? (r.name + ' — ' + (r.desc || '')) : id;
      wrap.appendChild(c);
    }
  }

  function buildShop() {
    const g = MT.game, wrap = refs.shop; wrap.innerHTML = '';
    g.shop.forEach((id, i) => {
      if (!id) { wrap.appendChild(el('div', 'cat-card empty', '<span>vendido</span>')); return; }
      const c = R.CAT[id], card = el('div', 'cat-card');
      if (c.cost > g.coins) card.classList.add('cant');
      card.dataset.tip = 'cat'; card.dataset.arg = c.id;
      const tags = c.tags.slice(0, 2).map(t => '<span title="' + t + '">' + (TAG_ICON[t] || '•') + '</span>').join('');
      card.innerHTML =
        '<div class="dmg-badge ' + c.type + '">' + SYM[c.type] + '</div>' +
        '<div class="cat-art"><img src="' + asset(c.sprite) + '" alt="" draggable="false"></div>' +
        '<div class="cat-name">' + shortName(c.name) + '</div>' +
        '<div class="cat-cost">🪙 ' + c.cost + '</div>' +
        '<div class="cat-tags">' + tags + '</div>';
      card.addEventListener('click', () => {
        if (MT.game.phase !== 'prep') return; // loja travada durante a onda
        if (c.cost > g.coins) { shakeEl(card); MT.sfx && MT.sfx.play('error'); tipHide(); return; }
        card.classList.add('pop'); MT.api.buy(i);
      });
      wrap.appendChild(card);
    });
  }
  function shakeEl(node) { node.classList.remove('shake'); void node.offsetWidth; node.classList.add('shake'); }
  function shortName(n) { return n.replace(/^Gat[oa] /, '').replace(/^Gata /, ''); }

  let lastBench = 0;
  function buildBench() {
    const g = MT.game, wrap = refs.bench; wrap.innerHTML = '';
    const cap = B.benchSize, count = g.bench.length, grew = count > lastBench;
    for (let i = 0; i < cap; i++) {
      const cat = g.bench[i];
      if (!cat) { wrap.appendChild(el('div', 'bench-slot', '')); continue; }
      const slot = el('div', 'bench-slot filled');
      if (g.selected && g.selected.cat === cat) slot.classList.add('picked');
      if (grew && i === count - 1) slot.classList.add('lit');
      slot.innerHTML = '<img src="' + asset(cat.data.sprite) + '" alt="" draggable="false">' +
        '<span class="mini-badge ' + cat.data.type + '">' + SYM[cat.data.type] + '</span>';
      slot.addEventListener('click', () => { if (MT.game.phase !== 'prep') return; if (g.selected && g.selected.cat === cat) MT.api.deselect(); else MT.api.pick(cat, 'bench'); });
      wrap.appendChild(slot);
    }
    lastBench = count;
  }

  let lastSynKey = '';
  function buildSyn() {
    const g = MT.game, wrap = refs.syn; if (!wrap) return; wrap.innerHTML = '';
    const active = g.synergies.filter(s => s.count > 0);
    if (active.length === 0) { wrap.innerHTML = '<div class="syn-empty">Sem sinergias<br><small>combine tipos de gato</small></div>'; lastSynKey = ''; return; }
    wrap.appendChild(el('div', 'syn-title', '✨ SINERGIAS'));
    const changedKey = active.map(s => s.tag + s.count).join('|');
    const changed = changedKey !== lastSynKey; lastSynKey = changedKey;
    active.slice(0, 8).forEach(s => {
      const on = s.tier >= 0;
      const nextNeed = s.need.find(n => n > s.count);
      const near = !on && nextNeed != null && (nextNeed - s.count) <= 1;
      const far = !on && !near;
      const row = el('div', 'syn-row' + (on ? ' on' : near ? ' near' : ' far') + (changed ? ' blink' : ''));
      row.dataset.tip = 'syn'; row.dataset.arg = s.id;
      row.innerHTML =
        '<span class="syn-ico">' + (TAG_ICON[s.tag] || '•') + '</span>' +
        '<span class="syn-count">' + s.count + '</span>' +
        '<span class="syn-name">' + s.name + '</span>' +
        '<span class="syn-tiers">' + s.need.map(n => '<i class="' + (s.count >= n ? 'hit' : '') + '"></i>').join('') + '</span>';
      row.addEventListener('click', (e) => { pinTip(synTip(s.id), e.clientX, e.clientY); });
      wrap.appendChild(row);
    });
  }

  function buildSel() {
    const g = MT.game, wrap = refs.sel; if (!wrap) return;
    const showing = !!(g.selected && g.selected.kind === 'board');
    const zc = document.querySelector('.zoom-ctl'); if (zc) zc.style.display = showing ? 'none' : 'flex';
    if (!showing) { wrap.classList.add('hide'); return; }
    const cat = g.selected.cat, cur = cat.cur || {};
    wrap.classList.remove('hide');
    const sellVal = Math.floor(cat.invested * (g.run.sellFull ? 1 : B.sellRatio));
    const tags = cat.data.tags.join(' · ');
    const maxS = MT.api.maxSlots(cat);
    let slotsHtml = '';
    for (let i = 0; i < maxS; i++) {
      const it = cat.items[i];
      slotsHtml += it ? '<div class="sel-item" title="' + (ITEM[it] ? ITEM[it].name : it) + '">' + (ITEM_ICON[it] || '❔') + '</div>'
        : '<div class="sel-item empty"></div>';
    }
    let invHtml = '';
    if (g.inventory.length) {
      invHtml = '<div class="inv-title">INVENTÁRIO (toque p/ equipar)</div><div class="inv-list">' +
        g.inventory.map((it, i) => '<div class="inv-item" data-inv="' + i + '" title="' + (ITEM[it] ? ITEM[it].name + ' — ' + ITEM[it].desc : it) + '">' + (ITEM_ICON[it] || '❔') + '</div>').join('') + '</div>';
    } else {
      invHtml = '<div class="inv-title">INVENTÁRIO</div><div class="inv-empty">vazio — ganhe itens nas ondas pares</div>';
    }
    const b = cat.data, buffs = [];
    const pct = (a, base) => Math.round((a / base - 1) * 100);
    if (cur.dmg > b.dmg + 0.01) buffs.push('🗡️ +' + pct(cur.dmg, b.dmg) + '% dano');
    if (cur.range > b.range + 0.01) buffs.push('🔭 +' + pct(cur.range, b.range) + '% alcance');
    if (cur.interval && cur.interval < b.interval - 0.001) buffs.push('🐾 +' + Math.round((b.interval / cur.interval - 1) * 100) + '% vel. ataque');
    if (cur.crit > b.crit + 0.01) buffs.push('🐯 +' + Math.round(cur.crit - b.crit) + '% crítico');
    if (cur.armorPen > 0) buffs.push('🔩 +' + Math.round(cur.armorPen) + ' pen. armadura');
    if (cur.magicPen > 0) buffs.push('🔮 +' + Math.round(cur.magicPen) + ' pen. mágica');
    if (cur.truePerHit > 0) buffs.push('👻 +' + cur.truePerHit + ' dano verdadeiro');
    const buffsHtml = buffs.length ? '<div class="sel-sep"></div><div class="inv-title">BUFFS ATIVOS</div><div class="sel-buffs">' +
      buffs.map(t => '<div class="sel-buff">' + t + '</div>').join('') + '</div>' : '';
    const tagsIco = cat.data.tags.map(t => (TAG_ICON[t] || '') + ' ' + t).join(' · ');
    wrap.innerHTML =
      '<div class="sel-head"><img src="' + asset(cat.data.sprite) + '"><div><b>' + cat.data.name + '</b>' +
      '<span class="sel-type ' + cat.data.type + '">' + SYM[cat.data.type] + ' ' + typeName(cat.data.type) + '</span></div>' +
      '<button class="sel-x" id="selClose">✕</button></div>' +
      '<div class="sel-tags">' + tagsIco + '</div>' +
      '<div class="sel-sep"></div>' +
      '<div class="sel-stats">' +
        st('DANO', Math.round(cur.dmg || cat.data.dmg)) + st('ALC', (cur.range || cat.data.range).toFixed(1)) +
        st('VEL', (1 / (cur.interval || cat.data.interval)).toFixed(2) + '/s') + st('CRIT', Math.round(cur.crit || cat.data.crit) + '%') +
      '</div>' + buffsHtml +
      '<div class="sel-sep"></div>' +
      '<div class="inv-title">ITENS (' + cat.items.length + '/' + maxS + ')</div>' +
      '<div class="sel-items">' + slotsHtml + '</div>' +
      invHtml +
      '<div class="sel-sep"></div>' +
      '<div class="sel-actions">' +
        '<button class="btn-sell" id="selSell">Vender 🪙' + sellVal + '</button>' +
        '<button class="mini-btn" id="selMove">Mover</button>' +
        '<button class="mini-btn" id="selCloseB">Fechar</button>' +
      '</div>';
    $('selClose').onclick = () => MT.api.deselect();
    $('selCloseB').onclick = () => MT.api.deselect();
    $('selMove').onclick = () => { MT.game.toast = 'Toque no gramado para reposicionar'; MT.game.toastT = 1.6; };
    $('selSell').onclick = () => MT.api.sell(cat);
    wrap.querySelectorAll('.inv-item').forEach(node => node.onclick = () => {
      const i = +node.dataset.inv, itemId = g.inventory[i];
      if (itemId != null && MT.api.equipItem(cat, itemId)) { MT.sfx && MT.sfx.play && MT.sfx.play('place'); }
    });
  }
  function st(l, v) { return '<div class="ss"><span>' + l + '</span><b>' + v + '</b></div>'; }
  function typeName(t) { return t === 'phys' ? 'Físico' : t === 'magic' ? 'Mágico' : 'Verdadeiro'; }

  // ---------- DRAFT (itens / relíquias) ----------
  const DMGCOL = { phys: 'var(--dmg-phys)', magic: 'var(--dmg-magic)', 'true': 'var(--dmg-true)' };
  const RELIC_DESC = {
    claws: '+25% de dano em todos os gatos', eagle: '+25% de alcance', reflex: '+25% de velocidade de ataque',
    instinct: '+20% de chance de crítico', piercer: '+40 de penetração de armadura', rune: '+40 de penetração mágica',
    backpack: '+1 slot de item por gato', purse: '+4 moedas ao fim de cada onda', merchant: 'venda devolve 100% do investido',
    luck: 'ganho de moedas ×1.3', heart: '+5 vidas imediatamente', treasure: '+15 moedas imediatamente', ward: '−10% de HP dos inimigos',
  };
  function itemDesc(it) { return it.desc || ''; }
  function showDraft() {
    const g = MT.game, d = g.pendingDraft; if (!d) return;
    const isRelic = d.type === 'relic';
    $('draftTitle').textContent = isRelic ? '✨ ESCOLHA UMA RELÍQUIA' : '🎁 ESCOLHA UM ITEM';
    $('draftSub').textContent = isRelic ? 'Bônus permanente para esta partida' : 'Guarde no inventário e equipe num gato';
    const wrap = $('draftCards'); wrap.innerHTML = '';
    d.choices.forEach(id => {
      const info = isRelic ? RELIC[id] : ITEM[id];
      const icon = isRelic ? (RELIC_ICON[id] || '🏺') : (ITEM_ICON[id] || '❔');
      const desc = isRelic ? (RELIC_DESC[id] || (info && info.desc) || '') : itemDesc(info);
      const card = el('div', 'draft-card' + (isRelic ? ' relic' : ''));
      card.innerHTML = '<div class="draft-icon">' + icon + '</div><div class="draft-name">' + (info ? info.name : id) + '</div><div class="draft-desc">' + desc + '</div>';
      card.addEventListener('click', () => pickDraft(id));
      wrap.appendChild(card);
    });
    show('draft');
  }
  function pickDraft(id) {
    const g = MT.game, d = g.pendingDraft; if (!d) { hide('draft'); return; }
    if (d.type === 'relic') MT.api.takeRelic(id); else MT.api.takeItem(id);
    MT.sfx && MT.sfx.play('coin');
    hide('draft'); refresh();
  }

  // ---------- COLEÇÃO ----------
  function showCollection() { hide('menu'); collTab = 'cats'; setActiveTab(); buildColl(); show('collection'); }
  function selectCollTab(t) { collTab = t; setActiveTab(); buildColl(); }
  function setActiveTab() { [...$('collTabs').children].forEach(b => b.classList.toggle('active', b.dataset.tab === collTab)); }
  function buildColl() {
    const body = $('collBody'); body.innerHTML = '';
    if (collTab === 'cats') D.cats.forEach(c => body.appendChild(collCat(c)));
    else if (collTab === 'enemies') D.enemies.forEach(e => body.appendChild(collEnemy(e)));
    else if (collTab === 'synergies') D.synergies.forEach(s => body.appendChild(collSyn(s)));
    else buildAch(body);
  }
  function collCat(c) {
    const card = el('div', 'coll-card');
    card.innerHTML = '<img src="' + asset(c.sprite) + '"><b>' + shortName(c.name) + ' <span class="badge" style="color:' + DMGCOL[c.type] + '">' + SYM[c.type] + '</span></b>' +
      '<small>🪙' + c.cost + ' · dano ' + c.dmg + ' · alc ' + c.range + '<br>' + c.tags.join(' · ') + '</small>';
    return card;
  }
  function collEnemy(e) {
    const card = el('div', 'coll-card');
    card.innerHTML = '<img src="' + asset(e.sprite) + '"><b>' + e.name + (e.boss ? ' 👑' : '') + '</b>' +
      '<small>HP ' + e.hp + ' · arm ' + e.armor + ' · resist ' + e.mr + '<br>🪙' + e.bounty + '</small>';
    return card;
  }
  function collSyn(s) {
    const card = el('div', 'coll-card'); card.style.textAlign = 'left';
    const tiers = s.tiers.map(t => '<div><b style="color:var(--gold)">' + t.need + '</b> ' + t.label + '</div>').join('');
    card.innerHTML = '<b>' + s.name + '</b><small>' + tiers + '</small>';
    return card;
  }
  function buildAch(body) {
    const done = MT.stats.unlockedCount();
    const head = el('div', 'coll-card'); head.style.gridColumn = '1 / -1'; head.style.textAlign = 'center';
    head.innerHTML = '<b>' + done + ' / ' + D.achievements.length + ' conquistas</b>';
    body.appendChild(head);
    D.achievements.forEach(a => {
      const on = MT.stats.unlocked(a);
      const card = el('div', 'coll-card ach' + (on ? ' on' : ' locked'));
      card.innerHTML = '<div class="badge">' + (on ? '🏆' : '🔒') + '</div><b>' + a.name + '</b><small>' + (a.desc || '') + '</small>';
      body.appendChild(card);
    });
  }

  // ---------- SOM ----------
  function toggleSound() { const on = !MT.sfx.enabled.get(); MT.sfx.enabled.set(on); if (on) { MT.sfx.resume(); MT.sfx.play('coin'); } updateSoundLabel(); }
  function updateSoundLabel() { const b = $('btnSound'); if (b) b.textContent = MT.sfx.enabled.get() ? '🔊 SOM' : '🔇 SOM'; }

  // ---------- TOOLTIP ----------
  let tipPinned = false, tipTimer = null;
  function tt(title, lines) { return '<div class="tt-title">' + title + '</div>' + lines.map(l => '<div class="tt-line">' + l + '</div>').join(''); }
  function synTip(id) {
    const s = SYND[id]; if (!s) return '';
    const cur = MT.game.synergies.find(x => x.id === id);
    const lines = s.tiers.map(t => { const on = cur && cur.count >= t.need; return '<b>' + (on ? '✔ ' : '') + t.need + ':</b> ' + t.label; });
    return tt((TAG_ICON[s.tag] || '') + ' ' + s.name + (cur ? ' (' + cur.count + ')' : ''), lines);
  }
  function catTip(id) {
    const c = CATD[id]; if (!c) return '';
    return tt(SYM[c.type] + ' ' + c.name, ['<b>' + typeName(c.type) + '</b> · 🪙' + c.cost,
      'Dano <b>' + c.dmg + '</b> · Alcance <b>' + c.range + '</b>', 'Crítico <b>' + c.crit + '%</b>', c.tags.join(' · ')]);
  }
  const STATIC_TIP = {
    lives: () => tt('Vidas', ['Perde vidas quando um inimigo alcança a base.', 'Chega a 0 = derrota.']),
    coins: () => tt('Moedas', ['Ganhe abatendo inimigos e ao fim da onda.', 'Gaste na loja e no reroll.']),
    wave: () => tt('Onda', ['Onda atual / total. No Infinito, joga até perder.']),
    speed: () => tt('Velocidade', ['Acelera a onda em andamento (1× · 2× · 3×).']),
    reroll: () => tt('Reroll', ['Troca as ofertas da loja por ' + B.rerollCost + ' 🪙.']),
  };
  function resolveTip(node) {
    const kind = node.dataset.tip, arg = node.dataset.arg;
    if (kind === 'syn') return synTip(arg);
    if (kind === 'cat') return catTip(arg);
    return STATIC_TIP[kind] ? STATIC_TIP[kind]() : '';
  }
  function positionTip(x, y) {
    const t = $('tip'), r = t.getBoundingClientRect();
    let nx = x + 14, ny = y + 14;
    if (nx + r.width > innerWidth - 8) nx = x - r.width - 14;
    if (ny + r.height > innerHeight - 8) ny = y - r.height - 14;
    t.style.left = Math.max(8, nx) + 'px'; t.style.top = Math.max(8, ny) + 'px';
  }
  function tipShow(html, x, y) { if (!html) return; const t = $('tip'); t.innerHTML = html; t.classList.remove('hide'); positionTip(x, y); }
  function tipHide() { if (tipPinned) return; $('tip').classList.add('hide'); }
  function pinTip(html, x, y) { tipPinned = false; tipShow(html, x, y); tipPinned = true; clearTimeout(tipTimer); tipTimer = setTimeout(() => { tipPinned = false; $('tip').classList.add('hide'); }, 2600); }
  function unpinTip() { tipPinned = false; clearTimeout(tipTimer); }

  // ---------- STYLE GUIDE ----------
  function showStyleGuide() { hide('menu'); buildStyleGuide(); show('styleguide'); }
  function buildStyleGuide() {
    const body = $('sgBody'); body.innerHTML = '';
    const item = (title, html) => { const d = el('div', 'sg-item'); d.innerHTML = '<h4>' + title + '</h4>' + html; body.appendChild(d); };
    item('Paleta', '<div class="sg-swatches">' +
      ['--bg', '--panel', '--card', '--gold', '--green', '--red', '--azure', '--dmg-phys', '--dmg-magic', '--dmg-true']
        .map(v => '<div class="sg-sw" style="background:var(' + v + ')" title="' + v + '"></div>').join('') + '</div>');
    item('Botões', '<button class="btn primary" style="width:100%;margin-bottom:6px">PRIMÁRIO</button>' +
      '<button class="btn ghost" style="width:100%">SECUNDÁRIO</button>');
    item('HUD Pill', '<div class="chip coins" style="display:inline-flex">🪙 <span>27</span></div> ' +
      '<div class="chip lives" style="display:inline-flex">❤️ <span>20</span></div>');
    item('Card da Loja', '<div class="cat-card" style="width:96px"><div class="dmg-badge magic">★</div>' +
      '<div class="cat-art"><img src="' + asset(D.cats[3].sprite) + '"></div><div class="cat-name">Mago</div>' +
      '<div class="cat-cost">🪙 4</div><div class="cat-tags"><span>🔮</span><span>✨</span></div></div>');
    item('Slot de Banco', '<div style="display:flex;gap:6px"><div class="bench-slot filled"><img src="' + asset(D.cats[0].sprite) + '"></div><div class="bench-slot"></div></div>');
    item('Linha de Sinergia', '<div class="synergy-panel" style="position:static;width:auto">' +
      '<div class="syn-row on"><span class="syn-ico">🔮</span><span class="syn-count">5</span><span class="syn-name">Místico</span><span class="syn-tiers"><i class="hit"></i><i class="hit"></i></span></div>' +
      '<div class="syn-row near"><span class="syn-ico">🏹</span><span class="syn-count">1</span><span class="syn-name">Caçador</span><span class="syn-tiers"><i></i><i></i></span></div></div>');
    item('Tooltip', '<div class="tip" style="position:static;max-width:none">' + synTip('Mystic') + '</div>');
    item('Tipos de Dano', '<div style="font-weight:800"><span style="color:var(--dmg-phys)">▲ Físico</span> · <span style="color:var(--dmg-magic)">★ Mágico</span> · <span style="color:var(--dmg-true)">◆ Verdadeiro</span></div>');
    item('Barra de Vida', '<div style="background:rgba(0,0,0,.5);border-radius:4px;height:8px;width:100%"><div style="background:var(--green);height:8px;width:65%;border-radius:4px"></div></div>');
  }

  // ---------- overlays por frame ----------
  function frameUI() {
    const g = MT.game;
    // HUD ao vivo durante a partida (vidas/moedas mudam no combate sem passar por refresh)
    if ((g.phase === 'wave' || g.phase === 'prep') && refs.lives) { refs.lives.textContent = g.lives; refs.coins.textContent = g.coins; }
    if (g.bannerT > 0 && g.banner) { refs.banner.querySelector('.b1').textContent = g.banner.t1; refs.banner.querySelector('.b2').textContent = g.banner.t2 || ''; refs.banner.classList.add('show'); }
    else refs.banner.classList.remove('show');
    if (g.toastT > 0) { refs.toast.textContent = g.toast; refs.toast.classList.add('show'); }
    else refs.toast.classList.remove('show');
  }

  MT.ui = { build, refresh, showMenu, showEnd, frameUI, showDraft, showCollection };
})(window.MT);
