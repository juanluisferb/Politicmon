"""Chiptune synthesizer: square waves (duty 12.5/25/50%) + LFSR noise -> 22 kHz 16-bit WAV.

Score notation (per channel, space separated):
  C4  E5  F#3  Bb4   note (default length: 2 sixteenths)
  C4/4               note with explicit length in sixteenths
  r  r/8             rest
  |                  bar separator (ignored)
Drum channel tokens: k (kick), s (snare), h (hat), o (open hat), r (rest); same /len syntax.
"""
import math
import os
import random
import struct
import wave
import pixlib

RATE = 22050
NOTE_INDEX = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6,
              "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def freq(name):
    n, octv = name[:-1], int(name[-1])
    semis = NOTE_INDEX[n] + (octv + 1) * 12
    return 440.0 * 2 ** ((semis - 69) / 12.0)


def parse(score, default=2):
    out = []
    for tok in score.split():
        if tok == "|":
            continue
        if "/" in tok:
            name, ln = tok.split("/")
            ln = int(ln)
        else:
            name, ln = tok, default
        out.append((name, ln))
    return out


def square(f, n, duty, vol, decay=0.0, sustain=1.0, vibrato=0.0):
    buf = [0.0] * n
    if f <= 0:
        return buf
    phase = 0.0
    attack = min(n, int(RATE * 0.004))
    release = min(n, int(RATE * 0.012))
    for i in range(n):
        ff = f * (1.0 + vibrato * math.sin(2 * math.pi * 5.5 * i / RATE)) if vibrato else f
        phase = (phase + ff / RATE) % 1.0
        env = 1.0
        if decay:
            env = max(sustain, math.exp(-decay * i / RATE))
        if i < attack:
            env *= i / attack
        if i > n - release:
            env *= max(0.0, (n - i) / release)
        buf[i] = (vol if phase < duty else -vol) * env
    return buf


def noise(n, vol, decay, short=False, seed=1):
    lfsr = 0x7FFF ^ seed
    buf = [0.0] * n
    period = 2 if short else 6
    val = 1.0
    for i in range(n):
        if i % period == 0:
            bit = (lfsr ^ (lfsr >> 1)) & 1
            lfsr = (lfsr >> 1) | (bit << 14)
            if short:
                lfsr = (lfsr & ~(1 << 6)) | (bit << 6)
            val = 1.0 if lfsr & 1 else -1.0
        buf[i] = val * vol * math.exp(-decay * i / RATE)
    return buf


def kick(n, vol):
    buf = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / RATE
        f = 150 * math.exp(-t * 30) + 45
        phase = (phase + f / RATE) % 1.0
        buf[i] = (vol if phase < 0.5 else -vol) * math.exp(-t * 18)
    return buf


def render_channel(score, bpm, kind="pulse", duty=0.5, vol=0.25, decay=0.0, sustain=1.0, vibrato=0.0, legato=0.92):
    step = 60.0 / bpm / 4.0
    out = []
    pos = 0
    for name, ln in parse(score):
        n = int(round((pos + ln) * step * RATE)) - int(round(pos * step * RATE))
        pos += ln
        if kind == "drums":
            if name == "k":
                seg = kick(min(n, int(RATE * 0.12)), vol * 1.2)
            elif name == "s":
                seg = noise(min(n, int(RATE * 0.16)), vol * 0.8, 18)
            elif name == "h":
                seg = noise(min(n, int(RATE * 0.04)), vol * 0.45, 60, short=False)
            elif name == "o":
                seg = noise(min(n, int(RATE * 0.12)), vol * 0.4, 22)
            else:
                seg = []
            seg = seg + [0.0] * (n - len(seg))
        elif name == "r":
            seg = [0.0] * n
        else:
            on = int(n * legato)
            seg = square(freq(name), on, duty, vol, decay, sustain, vibrato) + [0.0] * (n - on)
        out.extend(seg)
    return out


def mix(chans):
    n = max(len(c) for c in chans)
    out = [0.0] * n
    for c in chans:
        for i, v in enumerate(c):
            out[i] += v
    return out


def write(name, samples, kind="music"):
    peak = max(1e-6, max(abs(s) for s in samples))
    gain = min(1.0, 0.9 / peak)
    path = os.path.join(pixlib.ROOT, "assets", "audio", "%s_%s.wav" % (kind, name))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * gain)) * 32000)) for s in samples))
    return path


def song(name, bpm, lead, bass, drums=None, harmony=None, lead_duty=0.25, bass_duty=0.5, lead_vibrato=0.004, harmony_duty=0.125):
    chans = [render_channel(lead, bpm, duty=lead_duty, vol=0.22, decay=1.5, sustain=0.6, vibrato=lead_vibrato),
             render_channel(bass, bpm, duty=bass_duty, vol=0.16, decay=3.0, sustain=0.5)]
    if harmony:
        chans.append(render_channel(harmony, bpm, duty=harmony_duty, vol=0.10, decay=2.0, sustain=0.5))
    if drums:
        chans.append(render_channel(drums, bpm, kind="drums", vol=0.2))
    lengths = {len(c) for c in chans}
    if len(lengths) > 1:
        print("  warning: channel lengths differ in %s: %s" % (name, sorted(lengths)))
    n = min(len(c) for c in chans)
    write(name, mix([c[:n] for c in chans]))


def rep(s, n):
    return " ".join([s] * n)


DR4 = "k h s h k h s h | k h s h k k s h"
DR_FAST = "k h s h k h s h"
DR_MARCH = "k r s r k k s r"

SONGS = {
    "title": dict(bpm=124,
        lead="C5/4 G4/2 C5/2 E5/4 D5/2 C5/2 | D5/4 G4/2 B4/2 D5/8 | E5/4 C5/2 E5/2 G5/4 F5/2 E5/2 | D5/2 E5/2 F5/2 D5/2 C5/8 | "
             "A4/2 C5/2 F5/2 A5/2 G5/4 E5/4 | F5/2 E5/2 D5/2 C5/2 D5/8 | E5/2 G5/2 C6/4 B5/2 A5/2 G5/4 | F5/2 D5/2 B4/2 D5/2 C5/8",
        bass="C3 C3 G2 C3 C3 C3 G2 C3 | G2 G2 D3 G2 G2 G2 D3 G2 | C3 C3 G2 C3 E3 E3 C3 E3 | G2 G2 B2 G2 C3 G2 C3 r | "
             "F2 F2 C3 F2 F2 F2 C3 F2 | G2 G2 D3 G2 G2 G2 D3 G2 | C3 C3 E3 C3 A2 A2 E3 A2 | G2 G2 B2 G2 C3 G2 C3 r",
        drums=rep(DR4, 4)),
    "madrid": dict(bpm=132,
        lead="G4 B4 D5 B4 C5 B4 A4 r | G4 B4 D5 G5 F#5 E5 D5 r | E5 D5 C5 B4 A4 B4 C5 A4 | B4 A4 G4 A4 B4/4 G4/4 | "
             "G4 B4 D5 B4 C5 B4 A4 r | G4 B4 D5 G5 A5 G5 F#5 r | E5 G5 F#5 E5 D5 C5 B4 A4 | G4/4 D5/4 G4/8",
        bass="G2 G3 D3 G3 C3 G3 E3 G3 | G2 G3 D3 G3 D3 A3 F#3 A3 | C3 G3 E3 G3 D3 A3 F#3 A3 | G2 D3 G3 D3 G2 D3 G3 D3 | "
             "G2 G3 D3 G3 C3 G3 E3 G3 | G2 G3 D3 G3 D3 A3 F#3 A3 | C3 G3 E3 G3 D3 A3 F#3 A3 | G2 D3 G3 D3 G2/4 r/4",
        harmony="r/16 | r/16 | r/16 | r/16 | B4/8 C5/8 | B4/8 D5/8 | C5/8 A4/8 | B4/16",
        drums=rep(DR4, 4)),
    "andalucia": dict(bpm=138,
        lead="E5 F5 E5 D5 C5/3 B4/3 A4/2 | B4 C5 B4 A4 G#4/4 r/4 | A4 B4 C5 D5 E5/3 F5/3 E5/2 | D5 C5 B4 C5 B4/8 | "
             "E5 F5 G#5 F5 E5/3 D5/3 C5/2 | D5 E5 D5 C5 B4/4 r/4 | A4 C5 B4 A4 G#4/3 A4/3 B4/2 | C5 B4 A4 G#4 E4/8",
        bass="A2/3 A2/3 E3/2 A2/3 A2/3 E3/2 | E2/3 E2/3 B2/2 E2/3 E2/3 B2/2 | A2/3 A2/3 E3/2 D3/3 D3/3 A3/2 | E2/3 E2/3 B2/2 E2/3 E2/3 B2/2 | "
             "A2/3 A2/3 E3/2 F2/3 F2/3 C3/2 | G2/3 G2/3 D3/2 C3/3 C3/3 G3/2 | F2/3 F2/3 C3/2 E2/3 E2/3 B2/2 | E2/3 E2/3 B2/2 E2/3 E2/3 G#2/2",
        drums=rep("k/3 h/3 s/2 k/3 h/3 s/2", 8)),
    "galicia": dict(bpm=112,
        lead="D5/3 F#5/3 A5/2 G5/3 F#5/3 E5/2 | D5/3 E5/3 F#5/2 E5/3 D5/3 B4/2 | A4/3 D5/3 F#5/2 E5/3 D5/3 E5/2 | F#5/6 E5/2 D5/8 | "
             "D5/3 F#5/3 A5/2 B5/3 A5/3 F#5/2 | G5/3 F#5/3 E5/2 D5/3 E5/3 F#5/2 | E5/3 D5/3 B4/2 A4/3 B4/3 C#5/2 | D5/8 A4/8",
        bass="D3/8 A2/8 | D3/8 G2/8 | D3/8 A2/8 | D3/8 D3/8 | D3/8 B2/8 | G2/8 D3/8 | G2/8 A2/8 | D3/8 D3/8",
        harmony=rep("D4/16", 2) + " " + rep("D4/16", 2) + " " + rep("D4/16", 4),
        drums=rep("k/3 h/3 h/2 s/3 h/3 h/2", 8), lead_duty=0.125, lead_vibrato=0.008, harmony_duty=0.5),
    "battle": dict(bpm=164,
        lead="A4 A4 C5 A4 E5 A4 D5 C5 | B4 B4 D5 B4 E5 B4 D5 B4 | A4 A4 C5 A4 F5 E5 D5 C5 | B4 C5 D5 E5 G#5/4 E5/4 | "
             "A5 G5 E5 C5 A4 C5 E5 G5 | F5 E5 D5 C5 B4/4 D5/4 | C5 B4 A4 C5 E5 D5 C5 B4 | A4/4 E5/4 A5/8",
        bass=rep("A2 A3", 4) + " " + rep("G2 G3", 4) + " " + rep("F2 F3", 4) + " " + rep("E2 E3", 4) + " " +
             rep("A2 A3", 4) + " " + rep("D2 D3", 4) + " " + rep("F2 F3", 2) + " " + rep("E2 E3", 2) + " " + rep("A2 A3", 4),
        drums=rep(DR_FAST, 8), lead_duty=0.5),
    "gym": dict(bpm=150,
        lead="D5/4 F5/4 A5/4 G5 F5 | E5/4 C5/4 E5/4 G5/4 | F5/4 A5/4 D6/4 C6 A5 | Bb5/4 A5 G5 A5/8 | "
             "D5 D5 F5 D5 A5 D5 G5 F5 | E5 E5 G5 E5 C6 E5 Bb5 A5 | F5 G5 A5 Bb5 C6 Bb5 A5 G5 | A5/8 D5/8",
        bass=rep("D2 D3", 4) + " " + rep("C2 C3", 4) + " " + rep("Bb1 Bb2", 4) + " " + rep("A1 A2", 4) + " " +
             rep("D2 D3", 4) + " " + rep("C2 C3", 4) + " " + rep("Bb1 Bb2", 4) + " " + rep("A1 A2", 4),
        drums=rep(DR_MARCH, 8), lead_duty=0.5),
    "legend": dict(bpm=140,
        lead="C5/4 Eb5/4 G5/4 C6/4 | Bb5/4 Ab5/4 G5/8 | Ab5/4 G5/4 F5/4 Eb5/4 | D5/4 Eb5/4 F5/8 | "
             "G5 G5 Ab5 G5 F5 Eb5 D5 Eb5 | F5 F5 G5 F5 Eb5 D5 C5 D5 | Eb5 D5 C5 Bb4 Ab4 G4 Ab4 B4 | C5/16",
        bass=rep("C2 G2", 4) + " " + rep("Ab1 Eb2", 4) + " " + rep("F1 C2", 4) + " " + rep("G1 D2", 4) + " " +
             rep("C2 G2", 4) + " " + rep("Bb1 F2", 4) + " " + rep("Ab1 Eb2", 4) + " " + rep("G1 D2", 4),
        drums=rep("k h k h s h k h", 8), lead_duty=0.5),
    "victory": dict(bpm=140,
        lead="C5 E5 G5 C6/10 | B5 G5 A5 B5/10 | C6 B5 A5 G5 F5 E5 D5 E5 | C5/8 r/8",
        bass="C3/8 G2/8 | G2/8 D3/8 | F2/8 G2/8 | C3/8 r/8",
        drums="k h s h k h s h | k h s h k h s h | k h s h k h s h | k/8 r/8"),
    "encounter": dict(bpm=150,
        lead="E5 E5 r E5 r C5 E5 r | G5/4 r/4 G4/4 r/4 | E5 D5 C5 D5 E5 F5 G5 A5 | G5/4 E5/4 C5/8",
        bass=rep("C3 C3 G2 C3", 2) + " " + rep("C3 C3 G2 C3", 2) + " " + rep("F2 F2 C3 F2", 2) + " " + rep("G2 G2 D3 G2", 2),
        drums=rep(DR_FAST, 4), lead_duty=0.5),
    "badge": dict(bpm=120,
        lead="G4 C5 E5 G5/6 E5/2 G5/2 | A5 G5 F5 E5 D5/4 G5/4 | C6/16",
        bass="C3/8 C3/8 | F2/8 G2/8 | C3/16",
        drums="k h s h k h s h | k h s h k h s h | k/16"),
    "evolution": dict(bpm=110,
        lead="C5 E5 G5 E5 C5 E5 G5 E5 | D5 F5 A5 F5 D5 F5 A5 F5 | E5 G5 B5 G5 E5 G5 B5 G5 | F5 A5 C6 A5 G5/8",
        bass="C3/16 | D3/16 | E3/16 | F3/8 G2/8"),
    "sede": dict(bpm=104,
        lead="E5 G5 C6 G5 E5/4 D5/4 | C5 E5 G5 E5 D5/8 | F5 A5 C6 A5 G5/4 E5/4 | D5 E5 F5 D5 C5/8",
        bass="C3/4 G2/4 C3/4 G2/4 | C3/4 G2/4 G2/4 D3/4 | F2/4 C3/4 C3/4 G2/4 | G2/4 B2/4 C3/8",
        drums=rep("k h h h s h h h", 4), lead_duty=0.125),
    "garaje": dict(bpm=96,
        lead="C5/4 E5/4 G5/4 E5/4 | F5/4 A5/4 G5/8 | E5/4 C5/4 D5/4 B4/4 | C5/16",
        bass="C3/8 A2/8 | F2/8 G2/8 | A2/8 G2/8 | C3/16", lead_duty=0.125),
    "ending": dict(bpm=120,
        lead="E5/4 D5 C5 D5/4 E5/4 | G5/4 E5 C5 D5/8 | E5/4 G5 A5 G5/4 E5/4 | D5 E5 D5 C5 C5/8 | "
             "A5/4 G5 E5 G5/4 A5/4 | C6/4 B5 A5 G5/8 | E5/4 G5 A5 G5/4 E5/4 | D5/4 E5/4 C5/8",
        bass="C3/8 G2/8 | E2/8 G2/8 | A2/8 F2/8 | G2/8 C3/8 | F2/8 C3/8 | A2/8 G2/8 | C3/8 A2/8 | G2/8 C3/8",
        drums=rep(DR4, 4), lead_duty=0.25),
}

SFX = {
    "select": [("pulse", "C6/1 G6/2", 900, 0.5, 0.2, 0, 1.0)],
    "cursor": [("pulse", "A5/1", 1000, 0.5, 0.15, 0, 1.0)],
    "error": [("pulse", "C3/3 r/1 C3/3", 600, 0.5, 0.2, 0, 1.0)],
    "bump": [("pulse", "G2/2", 600, 0.5, 0.25, 6, 0.2)],
    "menu": [("pulse", "E5/1 A5/1 E6/2", 900, 0.25, 0.18, 0, 1.0)],
    "door": [("noise", "o/2 r/2", 400, 0, 0.35, 0, 1.0), ("pulse", "C4/1 G3/2", 500, 0.5, 0.15, 0, 1.0)],
    "hit": [("noise", "s/3", 600, 0, 0.6, 0, 1.0), ("pulse", "G2/1 C2/2", 800, 0.5, 0.2, 0, 1.0)],
    "faint": [("pulse", "G4/2 E4/2 C4/2 G3/2 E3/2 C3/4", 700, 0.5, 0.2, 2, 0.5)],
    "cry": [("pulse", "C5/1 E5/1 G5/1 E5/1 C6/2", 1100, 0.125, 0.18, 0, 1.0)],
    "throw": [("pulse", "C5/1 D5/1 E5/1 F5/1 G5/1 A5/1 B5/1 C6/1", 1500, 0.5, 0.15, 0, 1.0)],
    "shake": [("noise", "h/2", 600, 0, 0.5, 0, 1.0), ("pulse", "E4/1", 600, 0.5, 0.12, 0, 1.0)],
    "capture": [("pulse", "C5/2 E5/2 G5/2 C6/2 G5/2 C6/6", 700, 0.25, 0.22, 1, 0.6),
                ("pulse", "C4/2 C4/2 E4/2 E4/2 G4/2 C5/6", 700, 0.5, 0.12, 1, 0.6)],
    "levelup": [("pulse", "G5/2 G5/1 G5/1 C6/6", 700, 0.25, 0.22, 1, 0.6),
                ("pulse", "E5/2 E5/1 E5/1 G5/6", 700, 0.5, 0.1, 1, 0.6)],
    "heal": [("pulse", "C5/2 E5/2 G5/2 E5/2 C6/2 G5/2 C6/6", 640, 0.125, 0.22, 1, 0.6)],
    "item": [("pulse", "G5/2 B5/2 D6/2 G6/6", 700, 0.25, 0.2, 1, 0.6)],
    "save": [("pulse", "C6/2 E6/2 G6/4", 700, 0.25, 0.2, 1, 0.6)],
    "buy": [("pulse", "B5/1 E6/3", 900, 0.5, 0.18, 0, 1.0)],
    "exclaim": [("pulse", "E6/2 r/1 E6/3", 1000, 0.5, 0.2, 0, 1.0)],
    "status": [("pulse", "C5/1 F#5/1 C5/1 F#5/1 C5/1 F#5/1", 1200, 0.125, 0.15, 0, 1.0)],
    "run": [("noise", "h/1 h/1 h/1 h/1", 900, 0, 0.3, 0, 1.0)],
}


def make_sfx(name, layers):
    chans = []
    for (kind, score, bpm, duty, vol, decay, sustain) in layers:
        if kind == "noise":
            chans.append(render_channel(score, bpm, kind="drums", vol=vol))
        else:
            chans.append(render_channel(score, bpm, duty=duty, vol=vol, decay=decay, sustain=sustain, legato=0.98))
    write(name, mix(chans), kind="sfx")


def main():
    for name, s in SONGS.items():
        song(name, **s)
    for name, layers in SFX.items():
        make_sfx(name, layers)
    print("audio: %d songs, %d sfx" % (len(SONGS), len(SFX)))


if __name__ == "__main__":
    main()
