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
    { id: 'vulcao', name: 'Vulcão Ardente', theme: 'volcano', lanes: [[[-10.4,5.1],[-9.28,3.76],[-6.1,3.59],[-5.38,2.56],[-5.91,1.17],[-6.38,-0.17],[-5.77,-2.12],[-4.06,-4.18],[-1.02,-4.71],[-0.52,-3.06],[-0.58,-1.45],[0.87,0],[-0.05,2.26],[1.29,3.93],[3.41,3.65],[4.36,1.95],[4.94,0.31],[7.56,-0.56],[9.43,-0.36],[11.92,-1.56]]] },
    { id: 'deserto', name: 'Deserto Dourado', theme: 'desert', lanes: [[[-11.32,2.31],[-8.08,2.4],[-5.01,3.62],[-2.89,3.68],[-0.16,2.67],[2.96,3.01],[5.47,3.29],[7.06,2.73],[9.38,1.78],[11.58,1.87]],[[-11.68,-1.87],[-8.47,-2.09],[-6.32,-2.98],[-3.73,-3.68],[-0.89,-2.98],[2.02,-1.89],[4.58,-2.42],[7.04,-3.48],[9.46,-2.06],[11.92,-1.81]]] },
    { id: 'geleira', name: 'Geleira Eterna', theme: 'ice', lanes: [[[-10.09,6.69],[-8.75,4.46],[-6.77,3.48],[-4.15,2.4],[-1.95,1.09],[-0.24,-0.14],[2.04,-1.37],[4.55,-2.84],[7.48,-4.04],[9.15,-4.79],[10.27,-6.63]],[[-10.9,-6.69],[-8.78,-5.43],[-7.66,-3.62],[-5.07,-2.65],[-1.19,-0.92],[4.16,1.73],[7.12,3.12],[7.48,4.51],[10.05,5.88]]] },
    { id: 'costa', name: 'Costa Tropical', theme: 'beach', lanes: [[[-11.96,5.96],[-9.5,4.37],[-8.81,3.48],[-6.63,3.01],[-6.02,1.62],[-5.93,0],[-4.82,-1.23],[-3.26,-2.54],[-1.83,-3.71],[0.48,-3.12],[2.1,-1.37],[4.08,-0.56],[5.53,-1.42],[8.18,-1.53],[9.18,0.45],[10.33,1.81],[11.83,1.87]]] },
    { id: 'cristal', name: 'Caverna de Cristal', theme: 'crystal', lanes: [[[-11.51,6.55],[-10.42,5.15],[-9.17,4.35],[-7.27,4.29],[-5.74,4.6],[-4.71,4.23],[-4.23,3.48],[-3.4,2.84],[-1.69,2.79],[0.01,3.09],[1.37,3.73],[2.63,3.73],[3.94,2.95],[4.83,1.62],[5.5,0.61],[6.98,0.11]],[[-11.65,-0.31],[-9.73,-0.31],[-7.13,-0.08],[-4.76,-0.03],[-2.78,-0.14],[-0.22,0.17],[1.85,0.17],[4.25,-0.22],[7.04,0.08]],[[-11.87,-5.71],[-9.28,-4.68],[-6.71,-4.9],[-4.76,-3.87],[-2.67,-3.06],[-0.49,-2.95],[1.54,-3.93],[3.49,-3.84],[4.94,-2.12],[5.33,-0.89],[6.98,0.14]]] },
    { id: 'pantano', name: 'Pântano Sombrio', theme: 'swamp', lanes: [[[-11.87,2.34],[-9.53,2.09],[-7.36,3.09],[-5.68,3.73],[-4.57,3.29],[-3.56,1.89],[-2.5,1.11],[-0.08,0.86],[2.04,1.7],[3.52,3.54],[5,3.45],[6.14,2.51],[7.98,1.45],[10.02,1.34],[11.89,1.73]],[[-11.93,-3.45],[-10.31,-3.54],[-8.33,-2.84],[-7.02,-1.84],[-5.54,-0.95],[-4.37,-0.98],[-3.09,-1.95],[-1.78,-3.48],[0.31,-4.32],[2.46,-3.87],[3.3,-2.9],[5.22,-1.95],[6.7,-2.17],[8.12,-2.93],[9.85,-3.54],[11.86,-3.4]]] },
    { id: 'ceu', name: 'Ilhas Flutuantes', theme: 'sky', lanes: [[[-11.87,-2.56],[-10.17,-2.28],[-9.48,-2.84],[-8.3,-3.32],[-7.55,-3.62],[-5.79,-3.45],[-5.26,-2.76],[-5.35,-1.7],[-5.21,-0.61],[-4.18,-0.2],[-3.31,0.58],[-3.14,1.48],[-3.23,2.53],[-2.67,3.18],[-1.53,3.34],[-0.49,3.01],[0.06,2.12],[-0.16,1.28],[0.01,0.58],[1.23,0.06],[2.04,-0.56],[1.79,-1.62],[1.82,-2.59],[2.66,-3.26],[3.69,-3.48],[5.64,-3.51],[6.87,-2.62],[7.82,-1.67],[8.46,-0.78],[7.96,-0.22],[6.95,0.42],[6.76,1.23],[7.31,2.12],[7.31,3.29]]] },
  ];
  // Os 3 originais (jardim/bosque/ruinas) vêm de MT.DATA (gamedata.js); sobrescreve os
  // caminhos com o traçado do editor sem editar o arquivo gerado.
  const ORIG = {
    jardim: [[[-11.93,6.49],[-9.87,4.9],[-8.58,2.79],[-8.55,1.2],[-6.99,0.86],[-4.85,-0.2],[-4.48,-1.53],[-3.28,-3.37],[-1.14,-4.65],[1.51,-4.32],[3.19,-2.87],[4.02,-1.37],[5.33,0.53],[6.9,1.14],[8.49,2.06],[9.13,4.12],[9.99,5.32],[11.75,6.24]]],
    bosque: [[[-11.85,0.33],[-9.39,0.28],[-6.85,0.31],[-5.63,0.58],[-5.29,1.81],[-5.35,3.15],[-4.01,3.59],[-2.7,3.59],[-1.92,2.95],[-1.08,1.53],[-0.08,1.5],[1.74,1.7],[2.13,2.56],[2.52,3.31],[3.52,3.48],[4.66,3.4],[4.94,2.53],[5.11,1.48],[5.47,1],[6.87,0.89],[7.82,0.22],[8.71,-0.28],[10.21,-0.28],[11.78,-0.17]],[[-11.87,0.33],[-9.45,0.25],[-6.91,0.25],[-5.65,-0.31],[-5.46,-1.53],[-5.46,-2.56],[-4.65,-2.84],[-3.17,-3.06],[-1.86,-2.7],[-1.39,-2.03],[-0.91,-0.86],[-0.13,-0.39],[0.96,-0.39],[2.02,-0.64],[2.38,-1.56],[3.35,-2.84],[4.72,-2.84],[6.28,-2.73],[7.34,-2.15],[7.56,-1.28],[7.87,-0.25],[9.27,-0.03],[10.27,-0.17],[11.69,-0.22]]],
    ruinas: [[[-10.31,4.07],[-6.13,3.9],[-4.65,3.09],[-3.34,3.23],[-1.67,2.37],[1.68,2.45],[5.11,0.25],[7.26,-0.14]],[[-10.93,-0.28],[-0.55,-0.31],[0.62,0.06],[1.51,0.08],[2.68,-0.72],[4.66,-0.64],[7.29,-0.17]],[[-10.84,-4.54],[-5.07,-4.32],[-4.12,-3.87],[-3.03,-3.87],[-1.55,-2.98],[2.91,-2.98],[5.45,-1.2],[7.23,-0.17]]],
  };

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
  // sobrescreve os caminhos dos 3 mapas originais com o traçado do editor
  for (const id in ORIG) {
    const m = MT.DATA.maps.find(x => x.id === id);
    if (m) { m.paths = ORIG[id].map(l => smooth(l)); m.lanes = ORIG[id].length; }
  }

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
