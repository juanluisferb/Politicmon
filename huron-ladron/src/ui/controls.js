// Entrada unificada: teclado, mando y controles táctiles (que escribe la escena del HUD en `touch`).
import Phaser from 'phaser';

export const touch = {
  mx: 0, my: 0, run: false, sneak: false,
  a: false, b: false, pause: false, // pulsaciones pendientes (se consumen)
  aHeld: false,
  active: false,
};

/** Pulsaciones por evento (no se pierden aunque la tecla se suelte en el mismo fotograma). */
class KeyPresses {
  constructor(scene) {
    this.set = new Set();
    const on = (e) => { if (!e.repeat) this.set.add(e.keyCode); };
    scene.input.keyboard.on('keydown', on);
    scene.events.once('shutdown', () => scene.input.keyboard.off('keydown', on));
  }

  any(...codes) { return codes.some((c) => this.set.has(c)); }
  clear() { this.set.clear(); }
}

export function isTouchDevice() {
  if (typeof window === 'undefined') return false;
  return 'ontouchstart' in window || navigator.maxTouchPoints > 0 || /Android/i.test(navigator.userAgent);
}

export class Controls {
  constructor(scene) {
    this.scene = scene;
    const K = Phaser.Input.Keyboard.KeyCodes;
    this.keys = scene.input.keyboard.addKeys({
      up: K.UP, down: K.DOWN, left: K.LEFT, right: K.RIGHT,
      w: K.W, a2: K.A, s: K.S, d: K.D,
      run: K.SHIFT, sneak: K.C, ctrl: K.CTRL,
      act: K.Z, space: K.SPACE, enter: K.ENTER, use: K.X, e: K.E, q: K.Q,
      pause: K.ESC, p: K.P,
    });
    this.prevPad = {};
    this.presses = new KeyPresses(scene);
  }

  pad() {
    const gp = this.scene.input.gamepad;
    if (!gp || !gp.total) return null;
    return gp.getPad(0);
  }

  /** Estado de juego por fotograma. */
  read() {
    const k = this.keys;
    const K = Phaser.Input.Keyboard.KeyCodes;
    const P = this.presses;
    let mx = (k.right.isDown || k.d.isDown ? 1 : 0) - (k.left.isDown || k.a2.isDown ? 1 : 0);
    let my = (k.down.isDown || k.s.isDown ? 1 : 0) - (k.up.isDown || k.w.isDown ? 1 : 0);
    let run = k.run.isDown;
    let sneak = k.sneak.isDown || k.ctrl.isDown;
    let a = P.any(K.Z, K.SPACE, K.ENTER, K.E);
    let b = P.any(K.X, K.Q);
    let pause = P.any(K.ESC, K.P);
    P.clear();
    let aHeld = k.act.isDown || k.space.isDown || k.enter.isDown;

    const p = this.pad();
    if (p) {
      const ax = p.axes.length ? p.axes[0].getValue() : 0;
      const ay = p.axes.length > 1 ? p.axes[1].getValue() : 0;
      if (Math.hypot(ax, ay) > 0.25) { mx = ax; my = ay; }
      if (p.left) mx = -1;
      if (p.right) mx = 1;
      if (p.up) my = -1;
      if (p.down) my = 1;
      const btn = (i) => !!(p.buttons[i] && p.buttons[i].pressed);
      const edge = (i) => btn(i) && !this.prevPad[i];
      run = run || btn(7) || btn(5);
      sneak = sneak || btn(6) || btn(4);
      a = a || edge(0);
      b = b || edge(1) || edge(2);
      pause = pause || edge(9);
      aHeld = aHeld || btn(0);
      for (let i = 0; i < 12; i++) this.prevPad[i] = btn(i);
    }

    if (touch.active) {
      if (Math.hypot(touch.mx, touch.my) > 0.2) { mx = touch.mx; my = touch.my; }
      run = run || touch.run;
      sneak = sneak || touch.sneak;
      a = a || touch.a;
      b = b || touch.b;
      pause = pause || touch.pause;
      aHeld = aHeld || touch.aHeld;
      touch.a = false;
      touch.b = false;
      touch.pause = false;
    }
    return { mx, my, run, sneak: sneak && !run, a, b, pause, aHeld };
  }
}

/**
 * Menú navegable con teclado/mando y táctil. items: [{x, y, w, h, onSelect, onFocus?, onLeft?, onRight?}]
 * layout 'v' (arriba/abajo) u 'h' (izquierda/derecha).
 */
export class Menu {
  constructor(scene, items, opts = {}) {
    this.scene = scene;
    this.items = items;
    this.index = opts.start ?? 0;
    this.layout = opts.layout ?? 'v';
    this.onBack = opts.onBack;
    this.onChange = opts.onChange;
    this.sound = opts.sound;
    this.enabled = true;
    this.tapToFocus = opts.tapToFocus ?? false;
    this.prevPad = {};
    this.repeatT = 0;
    this.presses = new KeyPresses(scene);
    for (const it of items) {
      if (!it.zone) {
        it.zone = scene.add.zone(it.x, it.y, it.w, it.h).setOrigin(0).setInteractive();
        it.zone.setDepth(1000);
      }
      it.zone.on('pointerdown', () => {
        if (!this.enabled) return;
        const i = this.items.indexOf(it);
        if (this.index !== i) {
          this.index = i;
          this.onChange?.(i);
          // En listas de tarjetas, el primer toque solo enfoca.
          if (this.tapToFocus) { this.sound?.('move'); return; }
        }
        this.sound?.('select');
        it.onSelect?.();
      });
    }
    scene.events.on('update', this.update, this);
    scene.events.once('shutdown', () => scene.events.off('update', this.update, this));
    this.onChange?.(this.index);
  }

  move(d) {
    const n = this.items.length;
    this.index = (this.index + d + n) % n;
    this.sound?.('move');
    this.onChange?.(this.index);
  }

  update() {
    if (!this.enabled) { this.presses.clear(); return; }
    const K = Phaser.Input.Keyboard.KeyCodes;
    const P = this.presses;
    const L = P.any(K.LEFT, K.A);
    const R = P.any(K.RIGHT, K.D);
    const prev = this.layout === 'v' ? P.any(K.UP, K.W) : L;
    const next = this.layout === 'v' ? P.any(K.DOWN, K.S) : R;
    const side = this.layout === 'v' ? (L ? -1 : R ? 1 : 0) : 0;
    let ok = P.any(K.ENTER, K.Z, K.SPACE);
    let back = P.any(K.ESC, K.X, K.BACKSPACE);
    P.clear();
    let dPrev = prev;
    let dNext = next;
    let dSide = side;
    const gp = this.scene.input.gamepad;
    const p = gp && gp.total ? gp.getPad(0) : null;
    if (p) {
      const ay = p.axes.length > 1 ? p.axes[1].getValue() : 0;
      const ax = p.axes.length ? p.axes[0].getValue() : 0;
      const st = {
        up: p.up || ay < -0.5, down: p.down || ay > 0.5, left: p.left || ax < -0.5, right: p.right || ax > 0.5,
        ok: !!p.buttons[0]?.pressed, back: !!p.buttons[1]?.pressed,
      };
      const e = (n) => st[n] && !this.prevPad[n];
      if (this.layout === 'v') { dPrev ||= e('up'); dNext ||= e('down'); if (e('left')) dSide = -1; if (e('right')) dSide = 1; } else { dPrev ||= e('left'); dNext ||= e('right'); }
      ok ||= e('ok');
      back ||= e('back');
      this.prevPad = st;
    }
    if (dPrev) this.move(-1);
    if (dNext) this.move(1);
    const cur = this.items[this.index];
    if (dSide && cur) {
      if (dSide < 0) cur.onLeft?.(); else cur.onRight?.();
    }
    if (ok && cur) {
      this.sound?.('select');
      cur.onSelect?.();
    }
    if (back && this.onBack) {
      this.sound?.('back');
      this.onBack();
    }
  }
}
