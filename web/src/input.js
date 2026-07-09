// Entrada por toque/mouse no tabuleiro: posicionar, selecionar, hover.
(function (MT) {
  'use strict';
  const input = { hover: null };

  function attach(canvas) {
    function toWorld(e) {
      const r = canvas.getBoundingClientRect();
      const sx = (e.clientX - r.left), sy = (e.clientY - r.top);
      return { wx: MT.cam.wx(sx), wy: MT.cam.wy(sy), sx, sy };
    }
    canvas.addEventListener('pointermove', (e) => {
      const g = MT.game;
      if (g.selected && g.selected.kind === 'bench') input.hover = toWorld(e);
    });
    canvas.addEventListener('pointerleave', () => { input.hover = null; });

    canvas.addEventListener('pointerdown', (e) => {
      const g = MT.game;
      if (g.phase !== 'prep') return;
      const p = toWorld(e);
      if (g.selected && g.selected.kind === 'bench') { input.hover = p; MT.api.placeAt(p.wx, p.wy); input.hover = null; return; }
      if (g.selected && g.selected.kind === 'board') {
        // toque em vazio move o gato selecionado; toque em outro gato seleciona-o
        const hit = pickBoardCat(p.wx, p.wy);
        if (hit && hit !== g.selected.cat) { MT.api.selectBoard(hit); return; }
        if (MT.api.placeValid(p.wx, p.wy)) { MT.api.placeAt(p.wx, p.wy); return; }
        MT.api.deselect(); return;
      }
      // nada selecionado: tocar num gato do tabuleiro seleciona
      const hit = pickBoardCat(p.wx, p.wy);
      if (hit) MT.api.selectBoard(hit);
    });
  }
  function pickBoardCat(wx, wy) {
    let best = null, bd = 0.6;
    for (const c of MT.game.board) { const d = MT.util.dist(c.x, c.y, wx, wy); if (d < bd) { bd = d; best = c; } }
    return best;
  }

  MT.input = input; MT.input.attach = attach;
})(window.MT);
