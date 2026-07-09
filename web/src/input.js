// Entrada: toque/mouse com ZOOM (pinça + roda) e PAN (arraste), além de
// posicionar/selecionar por toque. Distingue toque (tap) de arraste.
(function (MT) {
  'use strict';
  const input = { hover: null };
  const TAP_MOVE = 9; // px: abaixo disso um pointerup conta como toque

  function attach(canvas) {
    const pointers = new Map();
    let single = null;   // { id, sx, sy, lastX, lastY, moved }
    let pinch = null;    // { startDist, startZoom, prevMidX, prevMidY }

    function rectXY(e) { const r = canvas.getBoundingClientRect(); return { x: e.clientX - r.left, y: e.clientY - r.top }; }
    function toWorld(sx, sy) { return { wx: MT.cam.wx(sx), wy: MT.cam.wy(sy), sx, sy }; }

    canvas.addEventListener('wheel', (e) => {
      e.preventDefault();
      const p = rectXY(e);
      MT.cam.zoomBy(e.deltaY < 0 ? 1.12 : 0.89, p.x, p.y);
    }, { passive: false });

    canvas.addEventListener('pointerdown', (e) => {
      canvas.setPointerCapture && canvas.setPointerCapture(e.pointerId);
      const p = rectXY(e);
      pointers.set(e.pointerId, p);
      if (pointers.size === 2) {
        const pts = [...pointers.values()];
        pinch = { startDist: dist(pts[0], pts[1]), startZoom: MT.cam.zoom, prevMidX: (pts[0].x + pts[1].x) / 2, prevMidY: (pts[0].y + pts[1].y) / 2 };
        single = null; input.hover = null;
      } else if (pointers.size === 1) {
        single = { id: e.pointerId, sx: p.x, sy: p.y, lastX: p.x, lastY: p.y, moved: false };
        const g = MT.game;
        if (g.selected && g.selected.kind === 'bench') input.hover = toWorld(p.x, p.y);
      }
    });

    canvas.addEventListener('pointermove', (e) => {
      const p = rectXY(e);
      if (pointers.has(e.pointerId)) pointers.set(e.pointerId, p);

      if (pinch && pointers.size >= 2) {
        const pts = [...pointers.values()];
        const d = dist(pts[0], pts[1]);
        const midX = (pts[0].x + pts[1].x) / 2, midY = (pts[0].y + pts[1].y) / 2;
        MT.cam.setZoom(pinch.startZoom * (d / pinch.startDist), midX, midY);
        MT.cam.panBy(midX - pinch.prevMidX, midY - pinch.prevMidY);
        pinch.prevMidX = midX; pinch.prevMidY = midY;
        return;
      }
      if (single && e.pointerId === single.id) {
        const dx = p.x - single.lastX, dy = p.y - single.lastY;
        if (!single.moved && MT.util.dist(p.x, p.y, single.sx, single.sy) > TAP_MOVE) single.moved = true;
        if (single.moved) MT.cam.panBy(dx, dy);
        single.lastX = p.x; single.lastY = p.y;
        const g = MT.game;
        if (g.selected && g.selected.kind === 'bench') input.hover = toWorld(p.x, p.y);
        return;
      }
      // hover do mouse (sem botão): preview de posicionamento
      if (e.buttons === 0) { const g = MT.game; if (g.selected && g.selected.kind === 'bench') input.hover = toWorld(p.x, p.y); }
    });

    function up(e) {
      const wasSingle = single && e.pointerId === single.id;
      pointers.delete(e.pointerId);
      if (pointers.size < 2) pinch = null;
      if (wasSingle) {
        if (!single.moved) tap(single.sx, single.sy);
        single = null;
        if (e.pointerType !== 'mouse') input.hover = null;
      }
    }
    canvas.addEventListener('pointerup', up);
    canvas.addEventListener('pointercancel', up);

    function tap(sx, sy) {
      const g = MT.game; if (g.phase !== 'prep') return;
      const p = toWorld(sx, sy);
      if (g.selected && g.selected.kind === 'bench') { input.hover = p; MT.api.placeAt(p.wx, p.wy); input.hover = null; return; }
      const hit = pickCat(p.wx, p.wy);
      if (g.selected && g.selected.kind === 'board') {
        if (hit && hit !== g.selected.cat) { MT.api.selectBoard(hit); return; }
        if (MT.api.placeValid(p.wx, p.wy)) { MT.api.placeAt(p.wx, p.wy); return; }
        MT.api.deselect(); return;
      }
      if (hit) MT.api.selectBoard(hit);
    }
    function pickCat(wx, wy) { let best = null, bd = 0.6; for (const c of MT.game.board) { const d = MT.util.dist(c.x, c.y, wx, wy); if (d < bd) { bd = d; best = c; } } return best; }
  }
  function dist(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }

  MT.input = input; MT.input.attach = attach;
})(window.MT);
