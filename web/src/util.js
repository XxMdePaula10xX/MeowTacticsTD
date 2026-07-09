// Utilidades gerais + câmera (transformação mundo<->tela).
window.MT = window.MT || {};
(function (MT) {
  'use strict';

  const U = {
    clamp: (v, a, b) => Math.max(a, Math.min(b, v)),
    lerp: (a, b, t) => a + (b - a) * t,
    dist: (ax, ay, bx, by) => Math.hypot(ax - bx, ay - by),
    rand: () => Math.random(),
    randInt: (n) => Math.floor(Math.random() * n),
    pick: (arr) => arr[Math.floor(Math.random() * arr.length)],
    uid: (() => { let n = 1; return () => n++; })(),
    // embaralha (Fisher-Yates) uma cópia
    shuffle: (arr) => {
      const a = arr.slice();
      for (let i = a.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [a[i], a[j]] = [a[j], a[i]];
      }
      return a;
    },
    fmt: (n) => (n | 0).toLocaleString('pt-BR'),
  };

  // Vista de mundo fixa (unidades Unity), com "contain" na área do tabuleiro.
  // x:[-12,12] (24) por y:[-6.5,6.5] (13) cobre todos os mapas.
  const WORLD_W = 24, WORLD_H = 13;
  const cam = {
    U: 60, cx: 0, cy: 0, w: 0, h: 0,
    fit(pxW, pxH) {
      this.w = pxW; this.h = pxH;
      this.U = Math.min(pxW / WORLD_W, pxH / WORLD_H);
      this.cx = pxW / 2; this.cy = pxH / 2;
    },
    // mundo -> tela (y invertido: +y do Unity é para cima)
    sx(wx) { return this.cx + wx * this.U; },
    sy(wy) { return this.cy - wy * this.U; },
    // tela -> mundo
    wx(sx) { return (sx - this.cx) / this.U; },
    wy(sy) { return -(sy - this.cy) / this.U; },
  };

  MT.util = U;
  MT.cam = cam;
  MT.WORLD = { W: WORLD_W, H: WORLD_H };
})(window.MT);
