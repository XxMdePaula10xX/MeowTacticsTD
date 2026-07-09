// Interface em DOM: HUD, loja, banco, sinergias, seleção e telas.
(function (MT) {
  'use strict';
  const U = MT.util, R = MT.rules, D = MT.DATA, B = MT.DATA.balance;
  const SYM = { phys: '▲', magic: '★', 'true': '◆' };
  const $ = (id) => document.getElementById(id);
  function el(tag, cls, html) { const e = document.createElement(tag); if (cls) e.className = cls; if (html != null) e.innerHTML = html; return e; }
  function asset(p) { return MT.assetURL ? MT.assetURL(p) : p; }

  let refs = {};

  function build() {
    refs = {
      lives: $('lives'), coins: $('coins'), wave: $('wave'),
      shop: $('shop'), bench: $('bench'), syn: $('synPanel'), sel: $('selPanel'),
      hint: $('placeHint'), banner: $('banner'), toast: $('toast'),
      start: $('startBtn'), reroll: $('rerollBtn'), speed: $('speedBtn'),
    };
    refs.start.addEventListener('click', () => MT.api.startWave());
    refs.reroll.addEventListener('click', () => MT.api.reroll());
    refs.speed.addEventListener('click', () => { const g = MT.game; g.speed = g.speed === 1 ? 2 : g.speed === 2 ? 3 : 1; refs.speed.textContent = g.speed + '×'; });

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
    const cont = $('btnContinue');
    if (MT.save.has()) { cont.style.display = ''; cont.addEventListener('click', continueRun); }
    // fim
    $('btnAgain').addEventListener('click', () => { hide('endScreen'); showMenu(); });
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
    const best = MT.progress.bestFor(g.mode, g.mapId);
    $('endLogo').textContent = won ? '🏆' : '💀';
    $('endTitle').textContent = won ? 'VITÓRIA!' : (g.mode === 'endless' ? 'FIM DA JORNADA' : 'DERROTA');
    $('endSub').textContent = won ? 'Você repeliu todos os pesadelos.' : 'Os pesadelos tomaram o reino.';
    $('endStats').innerHTML =
      stat(g.waveIndex + (won ? 1 : 0), 'Ondas') + stat(g.enemiesKilled, 'Abates') +
      stat(g.lives, 'Vidas') + stat(best, 'Recorde');
    show('endScreen');
  }
  function stat(v, label) { return '<div class="st"><b>' + U.fmt(v) + '</b><span>' + label + '</span></div>'; }

  // ---------- REFRESH ----------
  function refresh() {
    const g = MT.game;
    if (g.phase === 'menu') return;
    refs.lives.textContent = g.lives;
    refs.coins.textContent = g.coins;
    const wlabel = (g.waveIndex + 1) + (g.mode === 'endless' ? ' ∞' : '/' + R.TOTAL_WAVES);
    refs.wave.textContent = wlabel;
    // botão iniciar
    const running = g.phase === 'wave';
    refs.start.disabled = running;
    refs.start.classList.toggle('running', running);
    refs.start.textContent = running ? '🌊 ONDA EM CURSO…' : '⚔️ INICIAR ONDA';
    refs.reroll.textContent = '🎲 ' + B.rerollCost;
    refs.reroll.disabled = running;
    refs.hint.classList.toggle('show', !!(g.selected && g.selected.kind === 'bench'));

    buildShop(); buildBench(); buildSyn(); buildSel();
  }

  function buildShop() {
    const g = MT.game, wrap = refs.shop; wrap.innerHTML = '';
    g.shop.forEach((id, i) => {
      if (!id) { wrap.appendChild(el('div', 'cat-card empty', '<span>vendido</span>')); return; }
      const c = R.CAT[id], card = el('div', 'cat-card');
      if (c.cost > g.coins) card.classList.add('cant');
      card.innerHTML =
        '<div class="dmg-badge ' + c.type + '">' + SYM[c.type] + '</div>' +
        '<div class="cat-art"><img src="' + asset(c.sprite) + '" alt="" draggable="false"></div>' +
        '<div class="cat-name">' + shortName(c.name) + '</div>' +
        '<div class="cat-cost">🪙 ' + c.cost + '</div>';
      card.addEventListener('click', () => MT.api.buy(i));
      wrap.appendChild(card);
    });
  }
  function shortName(n) { return n.replace(/^Gat[oa] /, '').replace(/^Gata /, ''); }

  function buildBench() {
    const g = MT.game, wrap = refs.bench; wrap.innerHTML = '';
    const cap = B.benchSize;
    for (let i = 0; i < cap; i++) {
      const cat = g.bench[i];
      if (!cat) { wrap.appendChild(el('div', 'bench-slot', '')); continue; }
      const slot = el('div', 'bench-slot filled');
      if (g.selected && g.selected.cat === cat) slot.classList.add('picked');
      slot.innerHTML = '<img src="' + asset(cat.data.sprite) + '" alt="" draggable="false">' +
        '<span class="mini-badge ' + cat.data.type + '">' + SYM[cat.data.type] + '</span>';
      slot.addEventListener('click', () => { if (g.selected && g.selected.cat === cat) MT.api.deselect(); else MT.api.pick(cat, 'bench'); });
      wrap.appendChild(slot);
    }
  }

  function buildSyn() {
    const g = MT.game, wrap = refs.syn; if (!wrap) return; wrap.innerHTML = '';
    const active = g.synergies.filter(s => s.count > 0);
    if (active.length === 0) { wrap.innerHTML = '<div class="syn-empty">Sem sinergias<br><small>combine tipos de gato</small></div>'; return; }
    wrap.appendChild(el('div', 'syn-title', 'SINERGIAS'));
    active.slice(0, 8).forEach(s => {
      const on = s.tier >= 0;
      const row = el('div', 'syn-row' + (on ? ' on' : ''));
      const nextNeed = s.need.find(n => n > s.count);
      row.innerHTML =
        '<span class="syn-count">' + s.count + '</span>' +
        '<span class="syn-name">' + s.name + '</span>' +
        '<span class="syn-tiers">' + s.need.map(n => '<i class="' + (s.count >= n ? 'hit' : '') + '"></i>').join('') + '</span>';
      row.title = on ? s.label : ('Faltam ' + (nextNeed - s.count) + ' para ativar');
      wrap.appendChild(row);
    });
  }

  function buildSel() {
    const g = MT.game, wrap = refs.sel; if (!wrap) return;
    if (!(g.selected && g.selected.kind === 'board')) { wrap.classList.add('hide'); return; }
    const cat = g.selected.cat, cur = cat.cur || {};
    wrap.classList.remove('hide');
    const sellVal = Math.floor(cat.invested * (g.run.sellFull ? 1 : B.sellRatio));
    const tags = cat.data.tags.join(' · ');
    wrap.innerHTML =
      '<div class="sel-head"><img src="' + asset(cat.data.sprite) + '"><div><b>' + cat.data.name + '</b>' +
      '<span class="sel-type ' + cat.data.type + '">' + SYM[cat.data.type] + ' ' + typeName(cat.data.type) + '</span></div>' +
      '<button class="sel-x" id="selClose">✕</button></div>' +
      '<div class="sel-stats">' +
        st('DANO', Math.round(cur.dmg || cat.data.dmg)) + st('ALC', (cur.range || cat.data.range).toFixed(1)) +
        st('VEL', (1 / (cur.interval || cat.data.interval)).toFixed(2) + '/s') + st('CRIT', Math.round(cur.crit || cat.data.crit) + '%') +
      '</div>' +
      '<div class="sel-tags">' + tags + '</div>' +
      '<div class="sel-actions"><span class="sel-hint">toque no gramado p/ mover</span>' +
      '<button class="btn-sell" id="selSell">Vender 🪙' + sellVal + '</button></div>';
    $('selClose').onclick = () => MT.api.deselect();
    $('selSell').onclick = () => MT.api.sell(cat);
  }
  function st(l, v) { return '<div class="ss"><span>' + l + '</span><b>' + v + '</b></div>'; }
  function typeName(t) { return t === 'phys' ? 'Físico' : t === 'magic' ? 'Mágico' : 'Verdadeiro'; }

  // ---------- overlays por frame ----------
  function frameUI() {
    const g = MT.game;
    if (g.bannerT > 0 && g.banner) { refs.banner.querySelector('.b1').textContent = g.banner.t1; refs.banner.querySelector('.b2').textContent = g.banner.t2 || ''; refs.banner.classList.add('show'); }
    else refs.banner.classList.remove('show');
    if (g.toastT > 0) { refs.toast.textContent = g.toast; refs.toast.classList.add('show'); }
    else refs.toast.classList.remove('show');
  }

  MT.ui = { build, refresh, showMenu, showEnd, frameUI };
})(window.MT);
