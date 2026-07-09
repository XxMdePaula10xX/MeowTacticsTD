// Renderização no Canvas: campo, caminhos, unidades (sprites), projéteis e FX.
(function (MT) {
  'use strict';
  const U = MT.util, cam = MT.cam;

  // cache de sprites
  const imgs = {};
  function img(path) {
    if (!path) return null;
    if (!imgs[path]) { const im = new Image(); im.src = MT.assetURL ? MT.assetURL(path) : path; imgs[path] = im; }
    const im = imgs[path];
    return (im.complete && im.naturalWidth) ? im : null;
  }
  MT.preload = function () { MT.DATA.cats.forEach(c => img(c.sprite)); MT.DATA.enemies.forEach(e => img(e.sprite)); MT.DATA.maps.forEach(m => img(m.bg)); };

  // temas de chão por mapa
  const THEME = {
    jardim: { a: '#20351f', b: '#1a2b1c', edge: '#2c4a2b' },
    bosque: { a: '#1c1836', b: '#161230', edge: '#2b2450' },
    ruinas: { a: '#1d2436', b: '#171d2e', edge: '#2b3550' },
  };
  const SYM = { phys: '▲', magic: '★', 'true': '◆' };
  const DMGCOL = { phys: '#ff9a4d', magic: '#b98cff', 'true': '#ffe8b0' };
  const PRIO_ICON = { first: '⏩', last: '⏪', strong: '💪', near: '📍' };

  let ctx = null;
  function setCtx(c) { ctx = c; }

  function draw(t) {
    const g = MT.game;
    ctx.clearRect(0, 0, cam.w, cam.h);
    ctx.save();
    if (g.shake.t > 0) {
      const k = g.shake.amt * (g.shake.t / g.shake.dur) * cam.U;
      ctx.translate((U.rand() * 2 - 1) * k, (U.rand() * 2 - 1) * k);
    }
    drawField();
    drawPaths(t);
    // preview de alcance ao posicionar / selecionar
    if (g.selected) drawPlacePreview();
    for (const c of g.board) drawRange(c);
    drawEnemies();
    drawCats(t);
    drawShots();
    drawFloats();
    drawParts();
    ctx.restore();
  }

  function W(wx) { return cam.sx(wx); }
  function H(wy) { return cam.sy(wy); }

  function drawField() {
    const g = MT.game, A = cam.ART;
    const bg = g.map ? img(g.map.bg) : null;
    // fundo ambiente (cobre o tabuleiro inteiro, escurecido — preenche as bordas)
    if (bg) {
      coverDraw(bg, 0, 0, cam.w, cam.h);
      ctx.fillStyle = 'rgba(9,9,20,.62)'; ctx.fillRect(0, 0, cam.w, cam.h);
    } else {
      const th = THEME[g.mapId] || THEME.jardim;
      ctx.fillStyle = th.b; ctx.fillRect(0, 0, cam.w, cam.h);
    }
    // arte principal, alinhada ao mundo (segue zoom/pan). Aqui os caminhos batem.
    if (bg) {
      const x = W(-A.hw), y = H(A.hh), w = 2 * A.hw * cam.U, h = 2 * A.hh * cam.U;
      // moldura suave
      ctx.save();
      ctx.shadowColor = 'rgba(0,0,0,.6)'; ctx.shadowBlur = 18;
      ctx.drawImage(bg, x, y, w, h);
      ctx.restore();
    }
  }
  // desenha uma imagem cobrindo (dx,dy,dw,dh) preservando proporção (cover)
  function coverDraw(im, dx, dy, dw, dh) {
    const ir = im.width / im.height, dr = dw / dh;
    let sw = im.width, sh = im.height, sx = 0, sy = 0;
    if (ir > dr) { sw = im.height * dr; sx = (im.width - sw) / 2; }
    else { sh = im.width / dr; sy = (im.height - sh) / 2; }
    ctx.drawImage(im, sx, sy, sw, sh, dx, dy, dw, dh);
  }

  function tracePath(lane) {
    ctx.beginPath();
    ctx.moveTo(W(lane[0].x), H(lane[0].y));
    for (let i = 1; i < lane.length; i++) ctx.lineTo(W(lane[i].x), H(lane[i].y));
  }
  function drawPaths(t) {
    ctx.lineCap = 'round'; ctx.lineJoin = 'round';
    if (MT.DEBUG_PATH) {
      for (const lane of MT.game.lanes) { ctx.strokeStyle = '#ff3b6b'; ctx.lineWidth = 3; tracePath(lane); ctx.stroke(); }
    }
    // trilha de fluxo sutil (pontos que correm no sentido do movimento) sobre a arte
    for (let li = 0; li < MT.game.lanes.length; li++) {
      const lane = MT.game.lanes[li], L = MT.game.laneLen[li];
      const spacing = 1.1, speed = 1.6;
      const off = (t * speed) % spacing;
      for (let d = off; d < L; d += spacing) {
        const p = MT.api.posAlong(li, d);
        const x = W(p.x), y = H(p.y);
        ctx.fillStyle = 'rgba(255,225,150,.35)';
        ctx.beginPath(); ctx.arc(x, y, 0.07 * cam.U, 0, 7); ctx.fill();
      }
    }
    // brilho suave na entrada de cada caminho
    for (const lane of MT.game.lanes) {
      const x = W(lane[0].x), y = H(lane[0].y), pulse = 0.5 + 0.5 * Math.sin(t * 3);
      ctx.save(); ctx.shadowColor = '#b98cff'; ctx.shadowBlur = 10 + pulse * 12;
      ctx.globalAlpha = 0.3; ctx.fillStyle = '#b98cff';
      ctx.beginPath(); ctx.arc(x, y, 0.22 * cam.U, 0, 7); ctx.fill(); ctx.restore();
    }
  }

  function drawRange(cat) {
    ctx.save();
    ctx.fillStyle = 'rgba(91,163,242,.10)';
    ctx.strokeStyle = cat.target ? 'rgba(255,210,79,.4)' : 'rgba(91,163,242,.35)';
    ctx.lineWidth = 1.5;
    ctx.beginPath(); ctx.arc(W(cat.x), H(cat.y), (cat.cur ? cat.cur.range : cat.data.range) * cam.U, 0, 7); ctx.fill(); ctx.stroke();
    ctx.restore();
  }
  function drawPlacePreview() {
    const g = MT.game;
    if (g.selected.kind === 'board') return;
    // segue o dedo/cursor
    if (MT.input && MT.input.hover) {
      const h = MT.input.hover, ok = MT.api.placeValid(h.wx, h.wy);
      const rng = (g.selected.cat.cur ? g.selected.cat.cur.range : g.selected.cat.data.range);
      ctx.save();
      ctx.fillStyle = ok ? 'rgba(79,199,110,.16)' : 'rgba(238,107,110,.16)';
      ctx.strokeStyle = ok ? 'rgba(79,199,110,.7)' : 'rgba(238,107,110,.7)';
      ctx.lineWidth = 2;
      ctx.beginPath(); ctx.arc(W(h.wx), H(h.wy), rng * cam.U, 0, 7); ctx.fill(); ctx.stroke();
      ctx.restore();
      drawUnitSprite(g.selected.cat.data.sprite, h.wx, h.wy, 0.62, g.selected.cat.data);
    }
  }

  function drawUnitSprite(path, wx, wy, sizeU, data) {
    const im = img(path), x = W(wx), y = H(wy), s = sizeU * cam.U;
    if (im) ctx.drawImage(im, x - s / 2, y - s / 2, s, s);
    else { ctx.font = (s * 0.8) + 'px serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('🐱', x, y); }
  }

  function drawCats(t) {
    for (const cat of MT.game.board) {
      const x = W(cat.x), y = H(cat.y), sc = 0.9 + 0.2 * cat.pop;
      const sel = MT.game.selected && MT.game.selected.cat === cat;
      // sombra + base
      ctx.save(); ctx.translate(x, y);
      ctx.fillStyle = 'rgba(0,0,0,.28)';
      ctx.beginPath(); ctx.ellipse(0, 0.16 * cam.U, 0.32 * cam.U, 0.16 * cam.U, 0, 0, 7); ctx.fill();
      ctx.fillStyle = 'rgba(43,40,86,.95)';
      ctx.strokeStyle = sel ? '#ffd24f' : 'rgba(255,210,79,.5)'; ctx.lineWidth = sel ? 3 : 2;
      ctx.beginPath(); ctx.arc(0, 0, 0.34 * cam.U, 0, 7); ctx.fill(); ctx.stroke();
      // seta de direção
      ctx.rotate(cat.ang); ctx.fillStyle = '#ffd24f';
      ctx.beginPath(); ctx.moveTo(0.32 * cam.U, 0); ctx.lineTo(0.2 * cam.U, -0.08 * cam.U); ctx.lineTo(0.2 * cam.U, 0.08 * cam.U); ctx.fill();
      ctx.restore();
      // sprite
      drawUnitSprite(cat.data.sprite, cat.x, cat.y - 0.04, 0.58 * sc, cat.data);
      // símbolo de tipo (canto superior direito)
      ctx.font = '900 ' + (0.22 * cam.U) + 'px ' + bodyFont(); ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillStyle = DMGCOL[cat.data.type];
      ctx.fillText(SYM[cat.data.type], x + 0.26 * cam.U, y - 0.26 * cam.U);
      // indicador de prioridade de alvo (canto superior esquerdo)
      ctx.font = (0.17 * cam.U) + 'px serif';
      ctx.globalAlpha = 0.9; ctx.fillText(PRIO_ICON[cat.priority || 'first'], x - 0.25 * cam.U, y - 0.25 * cam.U); ctx.globalAlpha = 1;
    }
  }

  function drawEnemies() {
    for (const en of MT.game.enemies) {
      if (en.d < 0) continue;
      const x = W(en.x), y = H(en.y), big = en.boss ? 1.5 : 1, Sz = 0.56 * big;
      // rastro nos velozes
      if (en.data.speed >= 1.5 && en.slowT <= 0) {
        for (let k = 1; k <= 2; k++) {
          const p = MT.api.posAlong(en.laneIdx, Math.max(0, en.d - k * 0.3));
          ctx.globalAlpha = 0.18 / k; drawUnitSprite(en.data.sprite, p.x, p.y, Sz, en.data); ctx.globalAlpha = 1;
        }
      }
      // sombra sob o personagem
      ctx.fillStyle = 'rgba(0,0,0,.34)';
      ctx.beginPath(); ctx.ellipse(x, y + 0.17 * cam.U * big, 0.26 * cam.U * big, 0.12 * cam.U * big, 0, 0, 7); ctx.fill();
      // aura por tipo (boss dourado, mágico roxo, blindado aço)
      let aura = null, ring = false;
      if (en.boss) aura = 'rgba(255,210,79,.9)';
      else if (en.data.mr >= 30) aura = 'rgba(185,140,255,.9)';
      else if (en.data.armor >= 30) { aura = 'rgba(170,190,220,.9)'; ring = true; }
      if (aura) {
        if (ring) { ctx.strokeStyle = aura; ctx.lineWidth = 2; ctx.globalAlpha = .8; ctx.beginPath(); ctx.arc(x, y, 0.32 * big * cam.U, 0, 7); ctx.stroke(); ctx.globalAlpha = 1; }
        else { ctx.globalAlpha = 0.16; ctx.fillStyle = aura; ctx.beginPath(); ctx.arc(x, y, 0.42 * big * cam.U, 0, 7); ctx.fill(); ctx.globalAlpha = 1; }
      }
      // sprite com glow de estado
      ctx.save();
      if (en.hit > 0) { ctx.shadowColor = '#fff'; ctx.shadowBlur = 20; }
      else if (en.slowT > 0) { ctx.shadowColor = '#7fd0ff'; ctx.shadowBlur = 14; }
      else if (aura && !ring) { ctx.shadowColor = aura; ctx.shadowBlur = 12 * big; }
      drawUnitSprite(en.data.sprite, en.x, en.y, Sz, en.data);
      ctx.restore();
      // barra de vida (boss mais destacada)
      if (en.hp < en.hpMax || en.boss) {
        const w = (en.boss ? 0.9 : 0.56) * cam.U * big, hp = Math.max(0, en.hp / en.hpMax);
        const bx = x - w / 2, by = y - (en.boss ? 0.44 : 0.34) * cam.U * big, h = en.boss ? 6 : 4;
        ctx.fillStyle = 'rgba(0,0,0,.6)'; rr(bx - 1, by - 1, w + 2, h + 2, 3); ctx.fill();
        ctx.fillStyle = hp > 0.5 ? '#4fc76e' : hp > 0.25 ? '#ffd24f' : '#ee6b6e'; rr(bx, by, w * hp, h, 2); ctx.fill();
      }
    }
  }
  function rr(x, y, w, h, r) { ctx.beginPath(); ctx.moveTo(x + r, y); ctx.arcTo(x + w, y, x + w, y + h, r); ctx.arcTo(x + w, y + h, x, y + h, r); ctx.arcTo(x, y + h, x, y, r); ctx.arcTo(x, y, x + w, y, r); ctx.closePath(); }

  function drawShots() {
    for (const s of MT.game.shots) {
      const x = W(s.x), y = H(s.y), ang = Math.atan2(s.ty - s.y, s.tx - s.x);
      ctx.save(); ctx.translate(x, y); ctx.rotate(-ang); // y invertido -> ângulo tela
      ctx.shadowColor = s.color; ctx.shadowBlur = 10; ctx.fillStyle = s.color;
      ctx.beginPath(); ctx.ellipse(0, 0, 0.14 * cam.U, 0.055 * cam.U, 0, 0, 7); ctx.fill();
      ctx.restore();
    }
  }
  function drawFloats() {
    for (const f of MT.game.floats) {
      const a = Math.max(0, f.life / 0.8), x = W(f.x), y = H(f.y);
      ctx.globalAlpha = a;
      const sz = (f.crit ? 0.40 : 0.26) * cam.U;
      ctx.font = '900 ' + sz + 'px ' + bodyFont(); ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillStyle = f.crit ? '#ffd24f' : (DMGCOL[f.type] || '#fff');
      ctx.strokeStyle = f.crit ? 'rgba(120,20,20,.9)' : 'rgba(0,0,0,.6)'; ctx.lineWidth = f.crit ? 4 : 3;
      const txt = (f.crit ? '✦' : '') + f.val;
      if (f.crit) { ctx.shadowColor = 'rgba(255,210,79,.8)'; ctx.shadowBlur = 10; }
      ctx.strokeText(txt, x, y); ctx.fillText(txt, x, y);
      ctx.shadowBlur = 0; ctx.globalAlpha = 1;
    }
  }
  function drawParts() {
    for (const p of MT.game.parts) {
      ctx.globalAlpha = Math.max(0, p.life / 0.9); ctx.fillStyle = p.color;
      ctx.beginPath(); ctx.arc(W(p.x), H(p.y), 2.5, 0, 7); ctx.fill();
    }
    ctx.globalAlpha = 1;
  }
  function bodyFont() { return 'ui-rounded, system-ui, sans-serif'; }

  MT.render = { draw, setCtx, THEME };
})(window.MT);
