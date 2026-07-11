// Utilidades gerais + câmera com zoom/pan (transformação mundo<->tela).
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
    shuffle: (arr) => { const a = arr.slice(); for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; },
    fmt: (n) => (n | 0).toLocaleString('pt-BR'),
  };

  // A arte de mapa (16:9) cobre o mundo [-hw,hw] x [-hh,hh]. Os caminhos foram
  // autorados nesse mesmo espaço no Unity, então batem com a arte.
  const ART = { hw: 11.95, hh: 6.72 };

  // zoom padrão >1 aproxima o framing (unidades maiores, "câmera na batalha").
  const HOME_ZOOM = 1.22;
  const cam = {
    U: 60, baseU: 60, w: 0, h: 0, zoom: HOME_ZOOM, panX: 0, panY: 0, minZoom: 1, maxZoom: 4.5, ART, HOME_ZOOM,
    fit(pxW, pxH) {
      this.w = pxW; this.h = pxH;
      // baseU = COVER (max): no zoom padrão a arte preenche o tabuleiro — um
      // único mapa, sem colunas nem duplicata, caminhos alinhados.
      const cover = Math.max(pxW / (ART.hw * 2), pxH / (ART.hh * 2));
      const contain = Math.min(pxW / (ART.hw * 2), pxH / (ART.hh * 2));
      this.baseU = cover;
      // permite dar zoom OUT até o mapa caber INTEIRO na tela (+ pequena folga),
      // mas não mais que isso ("nao muito"). minZoom < 1 = abaixo do cover.
      this.minZoom = (contain / cover) * 0.9;
      this.zoom = U.clamp(this.zoom, this.minZoom, this.maxZoom);
      this.apply();
    },
    apply() { this.U = this.baseU * this.zoom; this.clampPan(); },
    setZoom(z, ox, oy) {
      ox = ox == null ? this.w / 2 : ox; oy = oy == null ? this.h / 2 : oy;
      const wx = this.wx(ox), wy = this.wy(oy);
      this.zoom = U.clamp(z, this.minZoom, this.maxZoom); this.U = this.baseU * this.zoom;
      this.panX = ox - wx * this.U - this.w / 2;
      this.panY = oy + wy * this.U - this.h / 2;
      this.clampPan();
    },
    zoomBy(factor, ox, oy) { this.setZoom(this.zoom * factor, ox, oy); },
    panBy(dx, dy) { this.panX += dx; this.panY += dy; this.clampPan(); },
    clampPan() {
      // Limite frouxo: dá pra arrastar até QUALQUER canto/borda da arte cruzar o
      // centro da tela (com uma folga extra), pra alcançar e dar zoom em gatos nos
      // cantos — mesmo que apareça área escura além da arte do mapa.
      const halfW = ART.hw * this.U, halfH = ART.hh * this.U;
      const maxX = halfW + this.w * 0.15, maxY = halfH + this.h * 0.15;
      this.panX = U.clamp(this.panX, -maxX, maxX); this.panY = U.clamp(this.panY, -maxY, maxY);
    },
    reset() { this.zoom = HOME_ZOOM; this.panX = 0; this.panY = 0; this.apply(); },
    sx(wx) { return this.w / 2 + this.panX + wx * this.U; },
    sy(wy) { return this.h / 2 + this.panY - wy * this.U; },
    wx(sx) { return (sx - (this.w / 2 + this.panX)) / this.U; },
    wy(sy) { return -(sy - (this.h / 2 + this.panY)) / this.U; },
  };

  MT.util = U;
  MT.cam = cam;
})(window.MT);
