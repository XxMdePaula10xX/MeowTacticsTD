// Bootstrap: canvas, câmera, input, UI e game loop.
(function (MT) {
  'use strict';
  // resolução de assets (local: caminho direto; bundle: sobrescreve com data URIs)
  if (!MT.assetURL) MT.assetURL = (p) => p;

  let canvas, ctx, dpr = 1;
  function resize() {
    const wrap = canvas.parentElement, r = wrap.getBoundingClientRect();
    dpr = Math.min(window.devicePixelRatio || 1, 2);
    canvas.width = Math.round(r.width * dpr);
    canvas.height = Math.round(r.height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    MT.cam.fit(r.width, r.height);
  }

  let last = 0;
  function frame(ts) {
    const dtReal = Math.min(0.05, (ts - last) / 1000) || 0; last = ts;
    const g = MT.game;
    let scale = g.phase === 'wave' ? g.speed : 1;
    if (g.slowmoT > 0) { g.slowmoT -= dtReal; scale *= 0.4; } // slow-mo na entrada do chefe
    // pausa: congela a simulação (mantém render/UI vivos)
    const dt = g.paused ? 0 : dtReal * scale;
    MT.api.update(dt, g.paused ? 0 : dtReal);
    MT.render.draw(ts / 1000);
    MT.ui.frameUI();
    requestAnimationFrame(frame);
  }

  function boot() {
    canvas = document.getElementById('game');
    ctx = canvas.getContext('2d');
    MT.render.setCtx(ctx);
    resize();
    window.addEventListener('resize', resize);
    MT.input.attach(canvas);
    MT.ui.build();
    MT.preload();
    MT.ui.showMenu();
    requestAnimationFrame(frame);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})(window.MT);
