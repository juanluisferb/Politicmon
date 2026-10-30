// Sonido 100 % sintetizado con Web Audio: efectos tipo chiptune y un secuenciador de música
// con tres canales (pulso, triángulo y ruido) que cambia de intensidad según el dueño.

const NOTE = { C: 0, 'C#': 1, D: 2, 'D#': 3, E: 4, F: 5, 'F#': 6, G: 7, 'G#': 8, A: 9, 'A#': 10, B: 11 };
export function freq(n) {
  const m = /^([A-G]#?)(\d)$/.exec(n);
  if (!m) return 0;
  const midi = (Number(m[2]) + 1) * 12 + NOTE[m[1]];
  return 440 * 2 ** ((midi - 69) / 12);
}

// Patrones: cada cadena son pasos de semicorchea separados por espacios ('.' = silencio, '-' = mantener).
const SONGS = {
  title: {
    bpm: 140,
    lead: 'E5 . G5 . C6 . G5 . A5 . G5 . E5 . C5 . D5 . F5 . A5 . F5 . G5 - - . . . . . '
      + 'E5 . G5 . C6 . G5 . A5 . B5 . C6 . A5 . G5 . E5 . D5 . F5 . E5 - - - C5 . . . ',
    bass: 'C3 . . C3 G2 . . . F2 . . F2 C3 . . . D3 . . D3 A2 . . . G2 . . G2 B2 . G2 . '
      + 'C3 . . C3 G2 . . . F2 . . F2 C3 . . . G2 . . G2 D3 . . . C3 . G2 . C3 . . . ',
    drums: 'k . h . s . h . k . h k s . h . ',
  },
  calm: {
    bpm: 112,
    lead: 'A4 . . C5 . . B4 . A4 . . . E4 . . . F4 . . A4 . . G4 . F4 . . . C4 . . . '
      + 'D4 . . F4 . . E4 . D4 . . . A4 . . . E4 . G#4 . B4 . D5 . C5 . B4 . G#4 - . . ',
    bass: 'A2 . . . E3 . . . A2 . . . E3 . . . F2 . . . C3 . . . F2 . . . C3 . . . '
      + 'D2 . . . A2 . . . D2 . . . A2 . . . E2 . . . B2 . . . E2 . . . G#2 . . . ',
    drums: 'k . . . h . . . k . . . h . . h ',
  },
  alert: {
    bpm: 128,
    lead: 'A4 . C5 . E5 . C5 . A4 . C5 . E5 . D5 . F4 . A4 . C5 . A4 . F4 . A4 . C5 . B4 . '
      + 'D4 . F4 . A4 . F4 . D4 . F4 . A4 . G4 . E4 . G#4 . B4 . D5 . E5 . D5 . B4 . G#4 . ',
    bass: 'A2 . A2 . E3 . A2 . A2 . A2 . E3 . G2 . F2 . F2 . C3 . F2 . F2 . F2 . C3 . E2 . '
      + 'D2 . D2 . A2 . D2 . D2 . D2 . A2 . C3 . E2 . E2 . B2 . E2 . E2 . E2 . G#2 . B2 . ',
    drums: 'k . h h s . h . k . h h s . h h ',
  },
  chase: {
    bpm: 168,
    lead: 'A5 . A5 . G5 . A5 . C6 . A5 . G5 . E5 . F5 . F5 . E5 . F5 . A5 . F5 . E5 . C5 . '
      + 'D5 . D5 . C5 . D5 . F5 . D5 . C5 . A4 . E5 . E5 . G#5 . E5 . B5 . G#5 . E5 . D5 . ',
    bass: 'A2 A3 A2 A3 A2 A3 A2 A3 A2 A3 A2 A3 A2 A3 G2 G3 F2 F3 F2 F3 F2 F3 F2 F3 F2 F3 F2 F3 F2 F3 E2 E3 '
      + 'D2 D3 D2 D3 D2 D3 D2 D3 D2 D3 D2 D3 D2 D3 C3 C4 E2 E3 E2 E3 E2 E3 E2 E3 E2 E3 E2 E3 G#2 G#3 B2 B3 ',
    drums: 'k h s h k h s h k h s h k s s s ',
  },
};

const split = (s) => s.trim().split(/\s+/);

class Sound {
  constructor() {
    this.ctx = null;
    this.musicOn = true;
    this.sfxOn = true;
    this.song = null;
    this.pending = null;
    this.step = 0;
    this.nextTime = 0;
    this.timer = null;
    try {
      const prefs = JSON.parse(localStorage.getItem('huron.audio') || '{}');
      if (prefs.music === false) this.musicOn = false;
      if (prefs.sfx === false) this.sfxOn = false;
    } catch { /* sin almacenamiento */ }
  }

  /** Se llama en el primer toque/tecla (los navegadores exigen un gesto para sonar). */
  unlock() {
    if (!this.ctx) {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (!AC) return;
      this.ctx = new AC();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.5;
      this.master.connect(this.ctx.destination);
      this.musicGain = this.ctx.createGain();
      this.musicGain.gain.value = this.musicOn ? 0.55 : 0;
      this.musicGain.connect(this.master);
      this.sfxGain = this.ctx.createGain();
      this.sfxGain.gain.value = this.sfxOn ? 0.9 : 0;
      this.sfxGain.connect(this.master);
      const len = this.ctx.sampleRate;
      this.noiseBuf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
      const d = this.noiseBuf.getChannelData(0);
      for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
    if (this.wanted && !this.timer) this.play(this.wanted);
  }

  savePrefs() {
    try { localStorage.setItem('huron.audio', JSON.stringify({ music: this.musicOn, sfx: this.sfxOn })); } catch { /* nada */ }
  }

  toggleMusic() {
    this.musicOn = !this.musicOn;
    if (this.musicGain) this.musicGain.gain.value = this.musicOn ? 0.55 : 0;
    this.savePrefs();
    return this.musicOn;
  }

  toggleSfx() {
    this.sfxOn = !this.sfxOn;
    if (this.sfxGain) this.sfxGain.gain.value = this.sfxOn ? 0.9 : 0;
    this.savePrefs();
    return this.sfxOn;
  }

  suspend() { if (this.ctx && this.ctx.state === 'running') this.ctx.suspend(); }
  resume() { if (this.ctx && this.ctx.state === 'suspended') this.ctx.resume(); }

  // ---------------------------------------------------------------- instrumentos
  tone(f, t, dur, { type = 'square', vol = 0.2, slide = 0, out = this.sfxGain, attack = 0.005 } = {}) {
    if (!this.ctx || !f) return;
    const o = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    o.type = type;
    o.frequency.setValueAtTime(f, t);
    if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(20, f + slide), t + dur);
    g.gain.setValueAtTime(0, t);
    g.gain.linearRampToValueAtTime(vol, t + attack);
    g.gain.exponentialRampToValueAtTime(0.001, t + dur);
    o.connect(g).connect(out);
    o.start(t);
    o.stop(t + dur + 0.02);
  }

  noise(t, dur, { vol = 0.2, hp = 800, lp = 12000, out = this.sfxGain } = {}) {
    if (!this.ctx) return;
    const s = this.ctx.createBufferSource();
    s.buffer = this.noiseBuf;
    const h = this.ctx.createBiquadFilter();
    h.type = 'highpass';
    h.frequency.value = hp;
    const l = this.ctx.createBiquadFilter();
    l.type = 'lowpass';
    l.frequency.value = lp;
    const g = this.ctx.createGain();
    g.gain.setValueAtTime(vol, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + dur);
    s.connect(h).connect(l).connect(g).connect(out);
    s.start(t, Math.random() * 0.5);
    s.stop(t + dur + 0.02);
  }

  // ---------------------------------------------------------------- efectos
  sfx(name) {
    if (!this.ctx || !this.sfxOn) return;
    const t = this.ctx.currentTime + 0.01;
    const seq = (notes, gap, opts) => notes.forEach((n, i) => this.tone(freq(n), t + i * gap, gap * 1.6, opts));
    switch (name) {
      case 'step': this.noise(t, 0.03, { vol: 0.05, hp: 2000, lp: 6000 }); break;
      case 'stepRun': this.noise(t, 0.04, { vol: 0.09, hp: 1200, lp: 5000 }); break;
      case 'pickup': seq(['C5', 'G5'], 0.05, { vol: 0.12 }); break;
      case 'deposit': seq(['C5', 'E5', 'G5', 'C6'], 0.05, { vol: 0.12 }); break;
      case 'pair': seq(['G5', 'C6', 'E6', 'G6', 'C7'], 0.06, { vol: 0.12 }); break;
      case 'drop': this.tone(300, t, 0.08, { type: 'triangle', vol: 0.2, slide: -150 }); break;
      case 'search': this.noise(t, 0.12, { vol: 0.08, hp: 1500, lp: 4000 }); break;
      case 'found': seq(['E5', 'A5'], 0.06, { vol: 0.12 }); break;
      case 'empty': seq(['C4', 'A3'], 0.08, { type: 'triangle', vol: 0.2 }); break;
      case 'power': this.tone(400, t, 0.25, { vol: 0.12, slide: 800 }); break;
      case 'slide': this.noise(t, 0.4, { vol: 0.1, hp: 3000 }); this.tone(600, t, 0.4, { vol: 0.06, slide: 600 }); break;
      case 'keys': for (let i = 0; i < 6; i++) this.tone(2400 + (i % 3) * 400, t + i * 0.04, 0.06, { type: 'square', vol: 0.06 }); break;
      case 'tv': this.noise(t, 0.5, { vol: 0.15, hp: 300 }); seq(['C6', 'E6'], 0.08, { type: 'sine', vol: 0.08 }); break;
      case 'treat': seq(['C5', 'E5', 'G5', 'E5', 'G5', 'C6'], 0.04, { vol: 0.1 }); break;
      case 'dook': for (let i = 0; i < 3; i++) this.tone(900 + i * 80, t + i * 0.07, 0.05, { type: 'square', vol: 0.06, slide: -300 }); break;
      case 'question': seq(['E5', 'A5'], 0.09, { type: 'triangle', vol: 0.25 }); break;
      case 'alarm': for (let i = 0; i < 4; i++) this.tone(i % 2 ? 660 : 880, t + i * 0.09, 0.09, { vol: 0.1 }); break;
      case 'caught': seq(['G4', 'E4', 'C4', 'G3'], 0.1, { vol: 0.12 }); break;
      case 'released': seq(['C4', 'G4'], 0.07, { vol: 0.1 }); break;
      case 'tier': seq(['A2', 'A#2', 'A2'], 0.15, { type: 'sawtooth', vol: 0.12 }); break;
      case 'tidy': seq(['G4', 'D4'], 0.08, { type: 'triangle', vol: 0.2 }); break;
      case 'washed': for (let i = 0; i < 5; i++) this.tone(300 + Math.random() * 400, t + i * 0.06, 0.08, { type: 'sine', vol: 0.12, slide: 300 }); break;
      case 'door': this.noise(t, 0.12, { vol: 0.15, hp: 200, lp: 1200 }); break;
      case 'tube': this.tone(200, t, 0.5, { type: 'triangle', vol: 0.2, slide: 400 }); break;
      case 'noPower': this.tone(110, t, 0.12, { vol: 0.08 }); break;
      case 'exhausted': this.tone(500, t, 0.5, { type: 'triangle', vol: 0.15, slide: -350 }); break;
      case 'move': this.tone(880, t, 0.04, { vol: 0.06 }); break;
      case 'select': seq(['C5', 'C6'], 0.05, { vol: 0.1 }); break;
      case 'back': this.tone(440, t, 0.06, { vol: 0.08, slide: -200 }); break;
      case 'win': seq(['C5', 'E5', 'G5', 'C6', 'G5', 'C6', 'E6'], 0.1, { vol: 0.12 }); break;
      case 'fail': seq(['G4', 'F#4', 'F4', 'E4'], 0.18, { vol: 0.12 }); break;
      case 'star': seq(['E6', 'G6', 'C7'], 0.05, { vol: 0.1 }); break;
      case 'tick': this.tone(1200, t, 0.03, { vol: 0.05 }); break;
      default: break;
    }
  }

  // ---------------------------------------------------------------- música
  play(name) {
    this.wanted = name;
    if (!this.ctx) return;
    if (this.song && this.songName === name) return;
    if (!this.song || !this.timer) {
      this.load(name);
      this.step = 0;
      this.nextTime = this.ctx.currentTime + 0.05;
      clearInterval(this.timer);
      this.timer = setInterval(() => this.schedule(), 25);
    } else {
      this.pending = name; // cambia al final del compás
    }
  }

  load(name) {
    const s = SONGS[name];
    this.songName = name;
    this.song = { bpm: s.bpm, lead: split(s.lead), bass: split(s.bass), drums: split(s.drums) };
  }

  stop() {
    this.wanted = null;
    clearInterval(this.timer);
    this.timer = null;
    this.song = null;
    this.songName = null;
  }

  schedule() {
    if (!this.ctx || !this.song) return;
    while (this.nextTime < this.ctx.currentTime + 0.12) {
      const s = this.song;
      const st = 60 / s.bpm / 4;
      if (this.pending && this.step % 16 === 0) {
        this.load(this.pending);
        this.pending = null;
        this.step = 0;
        continue;
      }
      const t = this.nextTime;
      const out = this.musicGain;
      const len = (arr, i) => {
        let n = 1;
        while (arr[(i + n) % arr.length] === '-') n++;
        return n;
      };
      const li = this.step % s.lead.length;
      const ln = s.lead[li];
      if (ln !== '.' && ln !== '-') this.tone(freq(ln), t, st * len(s.lead, li) * 0.95, { type: 'square', vol: 0.07, out });
      const bi = this.step % s.bass.length;
      const bn = s.bass[bi];
      if (bn !== '.' && bn !== '-') this.tone(freq(bn), t, st * len(s.bass, bi) * 0.9, { type: 'triangle', vol: 0.22, out });
      const dn = s.drums[this.step % s.drums.length];
      if (dn === 'k') this.tone(150, t, 0.12, { type: 'sine', vol: 0.4, slide: -110, out });
      if (dn === 's') this.noise(t, 0.1, { vol: 0.14, hp: 1000, lp: 6000, out });
      if (dn === 'h') this.noise(t, 0.03, { vol: 0.06, hp: 7000, out });
      this.nextTime += st;
      this.step++;
    }
  }
}

export const sound = new Sound();
