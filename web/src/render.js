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
  MT.preload = function () { MT.DATA.cats.forEach(c => img(c.sprite)); MT.DATA.enemies.forEach(e => img(e.sprite)); };

  // temas de chão por mapa
  const THEME = {
    jardim: { a: '#20351f', b: '#1a2b1c', edge: '#2c4a2b' },
    bosque: { a: '#1c1836', b: '#161230', edge: '#2b2450' },
    ruinas: { a: '#1d2436', b: '#171d2e', edge: '#2b3550' },
  };
  const SYM = { phys: '▲', magic: '★', 'true': '◆' };
  const DMGCOL = { phys: '#ff9a4d', magic: '#b98cff', 'true': '#ffe8b0' };

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
    const th = THEME[MT.game.mapId] || THEME.jardim;
    // fundo geral
    ctx.fillStyle = th.b; ctx.fillRect(0, 0, cam.w, cam.h);
    // xadrez sutil dentro da vista de mundo
    const step = 1; // 1 unidade
    for (let gx = -12; gx < 12; gx += step) {
      for (let gy = -6; gy < 7; gy += step) {
        const on = (((gx + 24) + (gy + 12)) & 1) === 0;
        ctx.fillStyle = on ? th.a : th.b;
        ctx.fillRect(W(gx), H(gy + step), cam.U * step + 1, cam.U * step + 1);
      }
    }
  }

  function tracePath(lane) {
    ctx.beginPath();
    ctx.moveTo(W(lane[0].x), H(lane[0].y));
    for (let i = 1; i < lane.length; i++) ctx.lineTo(W(lane[i].x), H(lane[i].y));
  }
  function drawPaths(t) {
    ctx.lineCap = 'round'; ctx.lineJoin = 'round';
    for (const lane of MT.game.lanes) {
      ctx.strokeStyle = '#4a4368'; ctx.lineWidth = 0.66 * cam.U; tracePath(lane); ctx.stroke();
      ctx.strokeStyle = '#3b3550'; ctx.lineWidth = 0.52 * cam.U; tracePath(lane); ctx.stroke();
      ctx.strokeStyle = 'rgba(255,255,255,.10)'; ctx.lineWidth = 0.5 * cam.U;
      ctx.setLineDash([6, 20]); ctx.lineDashOffset = -(t * 46) % 26; tracePath(lane); ctx.stroke(); ctx.setLineDash([]);
    }
    // marcadores de entrada/saída
    for (const lane of MT.game.lanes) {
      marker(lane[0].x, lane[0].y, '#b98cff', '🕳️', t);
      marker(lane[lane.length - 1].x, lane[lane.length - 1].y, '#ee6b6e', '🏰', t);
    }
  }
  function marker(wx, wy, color, glyph, t) {
    const x = W(wx), y = H(wy), pulse = 0.5 + 0.5 * Math.sin(t * 3);
    ctx.save(); ctx.shadowColor = color; ctx.shadowBlur = 12 + pulse * 10;
    ctx.globalAlpha = 0.25; ctx.fillStyle = color;
    ctx.beginPath(); ctx.arc(x, y, 0.42 * cam.U, 0, 7); ctx.fill(); ctx.restore();
    ctx.font = (0.6 * cam.U) + 'px serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(glyph, x, y);
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
      // símbolo de tipo
      ctx.font = '900 ' + (0.22 * cam.U) + 'px ' + bodyFont(); ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillStyle = DMGCOL[cat.data.type];
      ctx.fillText(SYM[cat.data.type], x + 0.26 * cam.U, y - 0.26 * cam.U);
    }
  }

  function drawEnemies() {
    for (const en of MT.game.enemies) {
      if (en.d < 0) continue;
      const x = W(en.x), y = H(en.y), big = en.boss ? 1.5 : 1;
      ctx.save(); ctx.translate(x, y);
      ctx.fillStyle = 'rgba(0,0,0,.3)';
      ctx.beginPath(); ctx.ellipse(0, 0.16 * cam.U * big, 0.26 * cam.U * big, 0.13 * cam.U * big, 0, 0, 7); ctx.fill();
      if (en.hit > 0) { ctx.shadowColor = '#fff'; ctx.shadowBlur = 18; }
      if (en.slowT > 0) { ctx.shadowColor = '#7fd0ff'; ctx.shadowBlur = 14; }
      ctx.restore();
      drawUnitSprite(en.data.sprite, en.x, en.y, 0.56 * big, en.data);
      // barra de vida
      if (en.hp < en.hpMax) {
        const w = 0.56 * cam.U * big, hp = Math.max(0, en.hp / en.hpMax);
        const bx = x - w / 2, by = y - 0.34 * cam.U * big;
        ctx.fillStyle = 'rgba(0,0,0,.55)'; rr(bx - 1, by - 1, w + 2, 6, 3); ctx.fill();
        ctx.fillStyle = hp > 0.5 ? '#4fc76e' : hp > 0.25 ? '#ffd24f' : '#ee6b6e'; rr(bx, by, w * hp, 4, 2); ctx.fill();
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
      const sz = (f.crit ? 0.34 : 0.26) * cam.U;
      ctx.font = '900 ' + sz + 'px ' + bodyFont(); ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillStyle = DMGCOL[f.type] || '#fff'; ctx.strokeStyle = 'rgba(0,0,0,.6)'; ctx.lineWidth = 3;
      const txt = (f.crit ? '' : '') + f.val;
      ctx.strokeText(txt, x, y); ctx.fillText(txt, x, y);
      ctx.globalAlpha = 1;
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
