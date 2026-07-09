// 7 mapas PROCEDURAIS (desenhados no Canvas, sem PNG): cada um com paleta,
// caminhos suaves (Catmull-Rom), decoração e partículas ambientes.
// Empurra os mapas em MT.DATA.maps e expõe MT.mapart.{draw, thumb}.
(function (MT) {
  'use strict';

  // ---- Catmull-Rom: pontos de controle -> polilinha suave ----
  function smooth(pts, seg) {
    seg = seg || 14;
    if (pts.length < 3) return pts.map(p => ({ x: p[0], y: p[1] }));
    const P = [pts[0], ...pts, pts[pts.length - 1]].map(p => ({ x: p[0], y: p[1] }));
    const out = [];
    for (let i = 1; i < P.length - 2; i++) {
      const p0 = P[i - 1], p1 = P[i], p2 = P[i + 1], p3 = P[i + 2];
      for (let t = 0; t < seg; t++) {
        const s = t / seg, s2 = s * s, s3 = s2 * s;
        out.push({
          x: 0.5 * (2 * p1.x + (-p0.x + p2.x) * s + (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * s2 + (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * s3),
          y: 0.5 * (2 * p1.y + (-p0.y + p2.y) * s + (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * s2 + (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * s3),
        });
      }
    }
    out.push({ x: pts[pts.length - 1][0], y: pts[pts.length - 1][1] });
    return out;
  }

  // ---- RNG determinístico por mapa ----
  function hash(str) { let h = 2166136261; for (let i = 0; i < str.length; i++) { h ^= str.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; }
  function rng(seed) { let s = seed >>> 0 || 1; return () => { s = (Math.imul(s, 1664525) + 1013904223) >>> 0; return s / 4294967296; }; }

  // ---- Definição dos 7 mapas (pontos de controle por lane) ----
  // caminhos traçados sobre a arte pintada (overlay full-res).
  const DEFS = [
    { id: 'vulcao', name: 'Vulcão Ardente', theme: 'volcano', lanes: [
      [[-11.95,4.58],[-8.65,4.2],[-6.49,2.91],[-5.15,-0.71],[-3.73,-3.82],[-2.09,-5.32],[-0.61,-3.88],[0.06,-0.69],[0.48,2.36],[1.31,3.96],[2.45,3.09],[3.77,0.01],[4.52,-2.03],[5.61,-1.01],[8.15,-0.17],[11.95,-0.06]] ] },
    { id: 'deserto', name: 'Deserto Dourado', theme: 'desert', lanes: [
      [[-12, 2.6], [-7, 3.2], [-2, 2], [2, 3.2], [7, 2], [12, 2.6]],
      [[-12, -2.6], [-7, -2], [-2, -3.2], [2, -2], [7, -3.2], [12, -2.6]] ] },
    { id: 'geleira', name: 'Geleira Eterna', theme: 'ice', lanes: [
      [[-11.95,3.72],[-7.66,2.44],[-3.09,0.58],[0.2,0.01],[4.49,-1.28],[9.49,-3.14],[11.95,-3.56]],
      [[-11.95,-3.71],[-7.66,-2.42],[-3.09,-0.56],[0.2,0.01],[4.49,0.86],[9.49,3.01],[11.95,3.43]] ] },
    { id: 'costa', name: 'Costa Tropical', theme: 'beach', lanes: [
      [[-12, 4], [-7, 3], [-5, 0], [-1, -1], [2, -3], [6, -2], [8, 1], [12, 3]] ] },
    { id: 'cristal', name: 'Caverna de Cristal', theme: 'crystal', lanes: [
      [[-11.95,4.1],[-9.95,4.18],[-8.74,4.31],[-7.22,4.46],[-5.93,4.46],[-4.82,4.16],[-3.71,3.6],[-2.58,3.09],[-1.36,2.76],[-0.12,2.65],[1.15,2.37],[2.4,1.83],[3.63,1.23],[4.82,0.64],[5.72,0.35],[7.06,0]],
      [[-11.95,-0.1],[-10.12,-0.08],[-9.33,-0.05],[-8.06,-0.05],[-7.04,-0.04],[-5.48,-0.06],[-3.97,-0.1],[-2.96,-0.12],[-1.44,-0.12],[0.07,-0.08],[1.07,-0.07],[2.5,-0.06],[3.88,-0.03],[4.73,-0.03],[5.37,-0.05],[7.06,0]],
      [[-11.95,-3.93],[-9.9,-3.92],[-8.65,-3.92],[-7.13,-3.85],[-5.83,-3.79],[-4.81,-3.89],[-3.85,-4.36],[-3,-4.82],[-1.96,-4.86],[-0.92,-4.29],[0.28,-3.35],[1.5,-2.4],[2.84,-1.54],[4.26,-0.83],[5.34,-0.38],[7.06,0]] ] },
    { id: 'pantano', name: 'Pântano Sombrio', theme: 'swamp', lanes: [
      [[-11.95,2.36],[-10.26,2.37],[-9.25,2.5],[-7.81,2.9],[-6.41,3.38],[-5.01,3.51],[-3.51,3.07],[-1.91,2.54],[-0.26,2.56],[1.4,3.08],[3.03,3.39],[4.63,3],[6.33,2.29],[8.24,1.8],[9.67,1.77],[11.95,1.89]],
      [[-11.95,-3.44],[-10.07,-3.54],[-8.89,-3.5],[-7.17,-3.16],[-5.55,-2.62],[-3.99,-2.37],[-2.48,-2.67],[-0.95,-3.09],[0.65,-2.98],[2.33,-2.33],[4.02,-1.74],[5.67,-1.69],[7.33,-2.11],[8.98,-2.65],[10.12,-2.98],[11.95,-3.22]] ] },
    { id: 'ceu', name: 'Ilhas Flutuantes', theme: 'sky', lanes: [
      [[-11.95,-2.55],[-9.27,-2.98],[-6.59,-2.46],[-4.51,-0.04],[-3.29,2.12],[-2.18,3.14],[-0.65,1.93],[0.46,-0.55],[1.42,-2.59],[2.24,-1.75],[3.58,-0.06],[5.56,0.97],[7.04,0.58],[8.31,-0.28],[9.87,-0.84],[11.95,-0.97]] ] },
  ];

  // ---- Paletas / decoração / partículas por tema ----
  const T = {
    volcano: { bg: ['#2a1410', '#0d0605'], gIn: '#3a1a12', gOut: '#1a0c0a', pFill: '#6b2b1a', pEdge: '#ff7a3c', pGlow: 'rgba(255,110,50,.5)',
      decor: [{ e: '🌋', n: 2, s: 1.3 }, { e: '🪨', n: 7, s: 0.7 }, { e: '🔥', n: 4, s: 0.6 }], part: { kind: 'ember', c: 'rgba(255,150,60,', n: 40 },
      thumb: 'linear-gradient(160deg,#5a2015,#1a0b09)', ico: '🌋' },
    desert: { bg: ['#3a2c14', '#1c1408'], gIn: '#c9a760', gOut: '#8a6d34', pFill: '#7a5a2a', pEdge: '#d9b878', pGlow: null,
      decor: [{ e: '🌵', n: 8, s: 0.8 }, { e: '🏜️', n: 2, s: 1.2 }, { e: '🪨', n: 5, s: 0.6 }, { e: '🦂', n: 2, s: 0.5 }], part: { kind: 'dust', c: 'rgba(230,205,140,', n: 22 },
      thumb: 'linear-gradient(160deg,#d9b878,#7a5a2a)', ico: '🏜️' },
    ice: { bg: ['#14283a', '#08121e'], gIn: '#cfe6f5', gOut: '#7fa8c8', pFill: '#6f96b8', pEdge: '#e8f6ff', pGlow: 'rgba(160,220,255,.4)',
      decor: [{ e: '🏔️', n: 3, s: 1.2 }, { e: '❄️', n: 6, s: 0.6 }, { e: '🧊', n: 5, s: 0.7 }, { e: '🐧', n: 2, s: 0.6 }], part: { kind: 'snow', c: 'rgba(235,248,255,', n: 45 },
      thumb: 'linear-gradient(160deg,#dcefff,#6f96b8)', ico: '🏔️' },
    beach: { bg: ['#123244', '#081a24'], gIn: '#e6d29a', gOut: '#3aa0b8', pFill: '#c2a367', pEdge: '#efe0ad', pGlow: null, water: true,
      decor: [{ e: '🌴', n: 5, s: 1.1 }, { e: '🐚', n: 5, s: 0.5 }, { e: '⛱️', n: 2, s: 0.9 }, { e: '🦀', n: 2, s: 0.5 }], part: { kind: 'petal', c: 'rgba(255,240,200,', n: 16 },
      thumb: 'linear-gradient(160deg,#e6d29a,#3aa0b8)', ico: '🏝️' },
    crystal: { bg: ['#1a1230', '#0a0718'], gIn: '#2a2050', gOut: '#140e2c', pFill: '#4a3f78', pEdge: '#b98cff', pGlow: 'rgba(185,140,255,.55)',
      decor: [{ e: '💎', n: 7, s: 0.8 }, { e: '🔮', n: 3, s: 0.8 }, { e: '🪨', n: 5, s: 0.7 }, { e: '✨', n: 5, s: 0.5 }], part: { kind: 'fly', c: 'rgba(190,150,255,', n: 34 },
      thumb: 'linear-gradient(160deg,#4a3f78,#140e2c)', ico: '💎' },
    swamp: { bg: ['#16241a', '#0a130d'], gIn: '#20351f', gOut: '#0e1c12', pFill: '#4a4030', pEdge: '#8fa06a', pGlow: 'rgba(140,200,120,.3)', fog: true,
      decor: [{ e: '🌫️', n: 3, s: 1.4 }, { e: '🍄', n: 5, s: 0.6 }, { e: '🪵', n: 4, s: 0.8 }, { e: '🐸', n: 3, s: 0.5 }], part: { kind: 'fly', c: 'rgba(180,230,120,', n: 24 },
      thumb: 'linear-gradient(160deg,#2c4a2b,#0e1c12)', ico: '🐸' },
    sky: { bg: ['#3a5a8a', '#16233e'], gIn: '#8fb8e6', gOut: '#4a6ea8', pFill: '#c9b98a', pEdge: '#f0e6c8', pGlow: 'rgba(255,255,255,.3)', clouds: true,
      decor: [{ e: '☁️', n: 6, s: 1.2 }, { e: '⭐', n: 6, s: 0.5 }, { e: '🏝️', n: 2, s: 1.0 }, { e: '🦅', n: 2, s: 0.7 }], part: { kind: 'petal', c: 'rgba(255,255,255,', n: 18 },
      thumb: 'linear-gradient(160deg,#8fb8e6,#2a3e6a)', ico: '☁️' },
  };

  // registra os mapas — agora com ARTE pintada (bg PNG); paths traçados sobre a arte.
  DEFS.forEach(d => {
    MT.DATA.maps.push({ id: d.id, name: d.name, lanes: d.lanes.length, theme: d.theme,
      bg: 'assets/maps/mapa_' + d.id + '.png', proc: false,
      paths: d.lanes.map(l => smooth(l)) });
  });

  // decoração posicionada uma vez por mapa (fora dos caminhos)
  function decorFor(map) {
    if (map._decor) return map._decor;
    const th = T[map.theme], rand = rng(hash(map.id)), items = [];
    const dToPath = (x, y) => {
      let best = 1e9;
      for (const lane of map.paths) for (let i = 0; i < lane.length - 1; i += 2) {
        const a = lane[i], b = lane[i + 1] || lane[i];
        best = Math.min(best, Math.hypot(x - a.x, y - a.y));
      }
      return best;
    };
    for (const spec of th.decor) {
      let placed = 0, tries = 0;
      while (placed < spec.n && tries < 200) {
        tries++;
        const x = -11 + rand() * 22, y = -5.4 + rand() * 10.8;
        if (dToPath(x, y) < 1.3) continue;
        if (items.some(it => Math.hypot(it.x - x, it.y - y) < 1.2)) continue;
        items.push({ e: spec.e, x, y, s: spec.s * (0.85 + rand() * 0.3) });
        placed++;
      }
    }
    map._decor = items; return items;
  }

  // ---- desenho procedural do fundo ----
  function draw(map, ctx, cam, t) {
    const th = T[map.theme] || T.volcano, W = cam.w, H = cam.h;
    // ambiente (cobre tudo)
    let g = ctx.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, th.bg[0]); g.addColorStop(1, th.bg[1]);
    ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
    // campo de jogo (retângulo do mundo) com gradiente radial + moldura
    const A = cam.ART, x0 = cam.sx(-A.hw), y0 = cam.sy(A.hh), w = 2 * A.hw * cam.U, h = 2 * A.hh * cam.U;
    const cx = x0 + w / 2, cy = y0 + h / 2;
    const rg = ctx.createRadialGradient(cx, cy, Math.min(w, h) * 0.1, cx, cy, Math.max(w, h) * 0.7);
    rg.addColorStop(0, th.gIn); rg.addColorStop(1, th.gOut);
    ctx.save();
    roundRect(ctx, x0, y0, w, h, 18); ctx.clip();
    ctx.fillStyle = rg; ctx.fillRect(x0, y0, w, h);
    // água/lava/névoa: brilho animado sutil
    if (th.water || th.fog || th.clouds) {
      ctx.globalAlpha = 0.12;
      for (let i = 0; i < 5; i++) {
        ctx.fillStyle = th.pEdge;
        const yy = y0 + h * (0.15 + 0.17 * i) + Math.sin(t * 0.6 + i) * 6;
        ctx.fillRect(x0, yy, w, 2);
      }
      ctx.globalAlpha = 1;
    }
    // decoração
    for (const d of decorFor(map)) {
      const sx = cam.sx(d.x), sy = cam.sy(d.y);
      ctx.globalAlpha = 0.92; ctx.font = (d.s * cam.U) + 'px serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillText(d.e, sx, sy); ctx.globalAlpha = 1;
    }
    ctx.restore();
    // moldura suave
    ctx.strokeStyle = 'rgba(255,255,255,.06)'; ctx.lineWidth = 2; roundRect(ctx, x0, y0, w, h, 18); ctx.stroke();
    // partículas ambientes (sobre o campo)
    drawParticles(th, ctx, cam, t, x0, y0, w, h);
  }

  function drawParticles(th, ctx, cam, t, x0, y0, w, h) {
    const p = th.part; if (!p) return;
    for (let i = 0; i < p.n; i++) {
      const seed = i * 0.6180339, fx = (seed * 7.13) % 1, sp = 0.15 + (seed % 0.3);
      let px = x0 + fx * w, py, a, r = 1.6 + (i % 3);
      if (p.kind === 'ember') { const prog = 1 - ((t * sp + seed) % 1); py = y0 + prog * h; px += Math.sin(t * 2 + i) * 8; a = 0.7 * (1 - prog); }
      else if (p.kind === 'snow') { const prog = (t * sp * 0.6 + seed) % 1; py = y0 + prog * h; px += Math.sin(t + i) * 10; a = 0.8; }
      else if (p.kind === 'dust') { const prog = (t * sp + seed) % 1; px = x0 + prog * w; py = y0 + h * (0.3 + (seed % 0.6)) + Math.sin(t + i) * 6; a = 0.35; }
      else if (p.kind === 'petal') { const prog = (t * sp * 0.5 + seed) % 1; py = y0 + prog * h; px += Math.sin(t * 1.5 + i) * 16; a = 0.6; }
      else { const prog = (t * sp * 0.4 + seed) % 1; py = y0 + (0.2 + 0.6 * ((Math.sin(t * 0.7 + i) + 1) / 2)) * h; px = x0 + ((fx + Math.sin(t * 0.3 + i) * 0.04) % 1) * w; a = 0.5 + 0.4 * Math.sin(t * 3 + i); }
      if (py < y0 || py > y0 + h) continue;
      ctx.globalAlpha = Math.max(0, a); ctx.fillStyle = p.c + '1)';
      if (p.kind === 'fly') { ctx.shadowColor = p.c + '1)'; ctx.shadowBlur = 8; }
      ctx.beginPath(); ctx.arc(px, py, r, 0, 7); ctx.fill(); ctx.shadowBlur = 0;
    }
    ctx.globalAlpha = 1;
  }

  function roundRect(ctx, x, y, w, h, r) { ctx.beginPath(); ctx.moveTo(x + r, y); ctx.arcTo(x + w, y, x + w, y + h, r); ctx.arcTo(x + w, y + h, x, y + h, r); ctx.arcTo(x, y + h, x, y, r); ctx.arcTo(x, y, x + w, y, r); ctx.closePath(); }

  MT.mapart = {
    draw,
    theme: (id) => T[(MT.DATA.maps.find(m => m.id === id) || {}).theme] || null,
    thumb: (id) => { const m = MT.DATA.maps.find(x => x.id === id); const th = m && T[m.theme]; return th ? { grad: th.thumb, ico: th.ico } : null; },
  };
})(window.MT);
