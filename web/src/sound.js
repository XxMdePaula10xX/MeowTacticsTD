// Efeitos sonoros simples via WebAudio (sem arquivos). Ligável/desligável.
(function (MT) {
  'use strict';
  let ctx = null;
  function ac() { if (!ctx) { try { ctx = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) {} } return ctx; }
  const enabled = {
    get() { try { return localStorage.getItem('mt_sound') !== '0'; } catch (e) { return true; } },
    set(v) { try { localStorage.setItem('mt_sound', v ? '1' : '0'); } catch (e) {} },
  };
  function tone(freq, dur, type, gain) {
    const a = ac(); if (!a || !enabled.get()) return;
    if (a.state === 'suspended') { try { a.resume(); } catch (e) {} }
    const o = a.createOscillator(), g = a.createGain();
    o.type = type || 'sine'; o.frequency.value = freq;
    const t = a.currentTime; g.gain.setValueAtTime(gain || 0.05, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g); g.connect(a.destination); o.start(t); o.stop(t + dur);
  }
  const S = {
    place: () => tone(660, 0.10, 'sine', 0.06),
    coin: () => { tone(880, 0.09, 'sine', 0.05); setTimeout(() => tone(1180, 0.08, 'sine', 0.04), 60); },
    wave: () => tone(300, 0.16, 'sawtooth', 0.05),
    boss: () => { tone(120, 0.4, 'sawtooth', 0.08); },
    lose: () => { tone(200, 0.5, 'sawtooth', 0.08); setTimeout(() => tone(120, 0.6, 'sawtooth', 0.08), 120); },
    win: () => { [523, 659, 784, 1046].forEach((f, i) => setTimeout(() => tone(f, 0.18, 'sine', 0.06), i * 110)); },
    error: () => tone(150, 0.14, 'square', 0.05),
    ach: () => { [659, 988].forEach((f, i) => setTimeout(() => tone(f, 0.16, 'triangle', 0.06), i * 120)); },
  };
  MT.sfx = { play(t) { if (S[t]) S[t](); }, enabled, resume() { const a = ac(); if (a && a.state === 'suspended') { try { a.resume(); } catch (e) {} } } };
})(window.MT);
