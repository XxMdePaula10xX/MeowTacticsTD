// Entrada: ZOOM (pinça+roda), PAN (arraste em área vazia), DRAG & DROP de gatos
// (do banco pro tabuleiro e reposicionar), TOQUE-E-APONTA (fallback) e ciclar
// prioridade tocando no gato durante a onda.
(function (MT) {
  'use strict';
  const input = { hover: null, dragging: null };
  const TAP_MOVE = 9;
  let canvasEl = null;

  function rectXY(cx, cy) { const r = canvasEl.getBoundingClientRect(); return { x: cx - r.left, y: cy - r.top }; }
  function toWorld(sx, sy) { return { wx: MT.cam.wx(sx), wy: MT.cam.wy(sy), sx, sy }; }
  function pickCat(wx, wy) { let best = null, bd = 0.6; for (const c of MT.game.board) { const d = MT.util.dist(c.x, c.y, wx, wy); if (d < bd) { bd = d; best = c; } } return best; }
  function overCanvas(cx, cy) { const r = canvasEl.getBoundingClientRect(); return cx >= r.left && cx <= r.right && cy >= r.top && cy <= r.bottom; }
  function dist(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }

  function attach(canvas) {
    canvasEl = canvas;
    const pointers = new Map();
    let single = null, pinch = null;

    canvas.addEventListener('wheel', (e) => { e.preventDefault(); const p = rectXY(e.clientX, e.clientY); MT.cam.zoomBy(e.deltaY < 0 ? 1.12 : 0.89, p.x, p.y); }, { passive: false });

    canvas.addEventListener('pointerdown', (e) => {
      canvas.setPointerCapture && canvas.setPointerCapture(e.pointerId);
      const p = rectXY(e.clientX, e.clientY);
      pointers.set(e.pointerId, p);
      if (pointers.size === 2) {
        const pts = [...pointers.values()];
        pinch = { startDist: dist(pts[0], pts[1]), startZoom: MT.cam.zoom, prevMidX: (pts[0].x + pts[1].x) / 2, prevMidY: (pts[0].y + pts[1].y) / 2 };
        if (single && single.dragCat && single.moved) { single.dragCat.x = single.origX; single.dragCat.y = single.origY; } // cancela arraste ao pinçar
        input.dragging = null; single = null; input.hover = null;
        return;
      }
      if (pointers.size === 1) {
        single = { id: e.pointerId, sx: p.x, sy: p.y, lastX: p.x, lastY: p.y, moved: false, dragCat: null };
        const g = MT.game;
        if (g.phase === 'prep') {
          const w = toWorld(p.x, p.y);
          if (g.selected && g.selected.kind === 'bench') input.hover = w; // carregando gato do banco (modo toque)
          else { const hit = pickCat(w.wx, w.wy); if (hit) { single.dragCat = hit; single.origX = hit.x; single.origY = hit.y; } }
        }
      }
    });

    canvas.addEventListener('pointermove', (e) => {
      const p = rectXY(e.clientX, e.clientY);
      if (pointers.has(e.pointerId)) pointers.set(e.pointerId, p);
      if (pinch && pointers.size >= 2) {
        const pts = [...pointers.values()], d = dist(pts[0], pts[1]);
        const midX = (pts[0].x + pts[1].x) / 2, midY = (pts[0].y + pts[1].y) / 2;
        MT.cam.setZoom(pinch.startZoom * (d / pinch.startDist), midX, midY);
        MT.cam.panBy(midX - pinch.prevMidX, midY - pinch.prevMidY);
        pinch.prevMidX = midX; pinch.prevMidY = midY; return;
      }
      if (single && e.pointerId === single.id) {
        const dx = p.x - single.lastX, dy = p.y - single.lastY;
        if (!single.moved && MT.util.dist(p.x, p.y, single.sx, single.sy) > TAP_MOVE) single.moved = true;
        const g = MT.game;
        if (single.dragCat) {
          if (single.moved) { const w = toWorld(p.x, p.y); single.dragCat.x = w.wx; single.dragCat.y = w.wy; input.dragging = single.dragCat; }
        } else if (g.selected && g.selected.kind === 'bench') {
          input.hover = toWorld(p.x, p.y); // preview segue o dedo
        } else if (single.moved) {
          MT.cam.panBy(dx, dy); // pan em área vazia
        }
        single.lastX = p.x; single.lastY = p.y; return;
      }
      if (e.buttons === 0) { const g = MT.game; if (g.selected && g.selected.kind === 'bench') input.hover = toWorld(p.x, p.y); }
    });

    function up(e) {
      const wasSingle = single && e.pointerId === single.id;
      pointers.delete(e.pointerId);
      if (pointers.size < 2) pinch = null;
      if (wasSingle) {
        if (single.dragCat) {
          const cat = single.dragCat;
          if (single.moved) {
            if (MT.api.placeValid(cat.x, cat.y, cat)) MT.api.repositionCat(cat, cat.x, cat.y);
            else { cat.x = single.origX; cat.y = single.origY; MT.api.recompute(); }
          } else {
            MT.api.selectBoard(cat); // toque simples abre o painel
          }
          input.dragging = null;
        } else {
          if (!single.moved) tap(single.sx, single.sy);
          if (e.pointerType !== 'mouse') input.hover = null;
        }
        single = null;
      }
      if (!single && pointers.size === 1) { const id = [...pointers.keys()][0], q = pointers.get(id); single = { id, sx: q.x, sy: q.y, lastX: q.x, lastY: q.y, moved: true, dragCat: null }; }
    }
    canvas.addEventListener('pointerup', up);
    canvas.addEventListener('pointercancel', up);

    function tap(sx, sy) {
      const g = MT.game, p = toWorld(sx, sy);
      if (g.phase === 'wave') { const hit = pickCat(p.wx, p.wy); if (hit) { const mode = MT.api.cyclePriority(hit); MT.ui && MT.ui.priorityToast && MT.ui.priorityToast(hit, mode); } return; }
      if (g.phase !== 'prep') return;
      if (g.selected && g.selected.kind === 'bench') { input.hover = p; MT.api.placeAt(p.wx, p.wy); input.hover = null; return; }
      const hit = pickCat(p.wx, p.wy);
      if (hit) MT.api.selectBoard(hit); else MT.api.deselect();
    }
  }

  // Arraste começando num slot do banco (DOM) — rastreado no documento até soltar.
  function beginBenchDrag(cat, e) {
    if (MT.game.phase !== 'prep' || !canvasEl) return;
    const wasSel = MT.game.selected && MT.game.selected.cat === cat;
    MT.api.pick(cat, 'bench');
    const start = { x: e.clientX, y: e.clientY }; let moved = false;
    const move = (ev) => {
      if (!moved && Math.hypot(ev.clientX - start.x, ev.clientY - start.y) > TAP_MOVE) moved = true;
      if (moved && overCanvas(ev.clientX, ev.clientY)) { const p = rectXY(ev.clientX, ev.clientY); input.hover = toWorld(p.x, p.y); }
    };
    const cleanup = () => {
      document.removeEventListener('pointermove', move);
      document.removeEventListener('pointerup', up);
      document.removeEventListener('pointercancel', cancel);
    };
    const up = (ev) => {
      cleanup();
      if (moved) {
        if (overCanvas(ev.clientX, ev.clientY)) {
          const p = rectXY(ev.clientX, ev.clientY), w = toWorld(p.x, p.y); input.hover = w;
          if (MT.api.placeValid(w.wx, w.wy)) MT.api.placeAt(w.wx, w.wy); else MT.api.deselect();
        } else MT.api.deselect();
        input.hover = null;
      } else if (wasSel) MT.api.deselect(); // toque repetido no mesmo gato desmarca
      // toque simples num gato não selecionado: mantém selecionado (toque-e-aponta),
      // então basta tocar no mapa pra posicionar.
    };
    // iOS pode cancelar o ponteiro (ex.: gesto virou rolagem): mantém o gato
    // selecionado pra que o toque-e-aponta ainda funcione.
    const cancel = () => { cleanup(); input.hover = null; };
    document.addEventListener('pointermove', move);
    document.addEventListener('pointerup', up);
    document.addEventListener('pointercancel', cancel);
  }

  MT.input = input; MT.input.attach = attach; MT.input.beginBenchDrag = beginBenchDrag;
})(window.MT);
