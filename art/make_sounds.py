#!/usr/bin/env python3
"""Synthesizes candidate charge-up sounds for the Railgunner.

Everything here is generated from scratch, so there are no licensing
concerns. Run from the repo root:

    python3 art/make_sounds.py [output_dir]

Writes 16-bit mono 44.1 kHz WAV files.
"""
import math
import random
import struct
import sys
import wave
from pathlib import Path

RATE = 44100


def write(path, samples):
    peak = max(1e-9, max(abs(s) for s in samples))
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(32000 * s / peak)) for s in samples))


def sweep(seconds, f0, f1, curve=2.0):
    """Frequency at time t rising from f0 to f1, faster near the end."""
    def freq(t):
        x = min(1.0, t / seconds)
        return f0 + (f1 - f0) * (x ** curve)
    return freq


def render(seconds, voices, envelope, tail=0.0):
    """voices: list of (freq_fn, amplitude, waveform)."""
    n = int(RATE * (seconds + tail))
    out = [0.0] * n
    for freq_fn, amp, shape in voices:
        phase = 0.0
        for i in range(n):
            t = i / RATE
            phase += 2 * math.pi * freq_fn(min(t, seconds)) / RATE
            if shape == "sine":
                v = math.sin(phase)
            elif shape == "saw":
                v = 2 * ((phase / (2 * math.pi)) % 1.0) - 1
            else:  # soft square
                v = math.tanh(3 * math.sin(phase))
            out[i] += amp * v
    return [s * envelope(i / RATE, seconds, tail) for i, s in enumerate(out)]


def charge_env(t, seconds, tail):
    attack = min(1.0, t / 0.05)
    swell = 0.35 + 0.65 * min(1.0, t / seconds)
    release = 1.0 if t <= seconds else max(0.0, 1 - (t - seconds) / max(tail, 1e-6))
    return attack * swell * release


def capacitor_whine():
    """Classic sci-fi capacitor charge: a pure tone climbing high."""
    s = 1.0
    f = sweep(s, 300, 3200, 1.6)
    return render(s, [(f, 1.0, "sine"), (lambda t: f(t) * 2, 0.25, "sine")], charge_env, tail=0.08)


def heavy_railgun():
    """Low hum and high whine rising together, with a beating pulse."""
    s = 1.0
    hi = sweep(s, 400, 2600, 2.0)
    lo = sweep(s, 55, 140, 1.0)
    samples = render(s, [(hi, 0.8, "sine"), (lambda t: hi(t) * 1.01, 0.6, "sine"),
                         (lo, 0.7, "square")], charge_env, tail=0.1)
    pulse = [0.75 + 0.25 * math.sin(2 * math.pi * (6 + 30 * min(1, i / RATE)) * i / RATE) for i in range(len(samples))]
    return [a * b for a, b in zip(samples, pulse)]


def energy_buildup():
    """Saw-ish rising buzz with a hiss of noise, more aggressive."""
    s = 1.0
    f = sweep(s, 120, 1500, 2.2)
    random.seed(7)
    samples = render(s, [(f, 0.6, "saw"), (lambda t: f(t) * 1.5, 0.3, "sine")], charge_env, tail=0.08)
    noise_gain = [0.15 * min(1, i / (RATE * s)) for i in range(len(samples))]
    return [v + g * (random.random() * 2 - 1) for v, g in zip(samples, noise_gain)]


def ready_tone():
    """Seamless 1 s loop: a steady sine with a gentle pitch wobble, played
    while the Railgunner holds a full charge. Integer carrier and wobble
    frequencies over exactly 1 s make the loop click-free."""
    n = RATE
    out = []
    phase = 0.0
    for i in range(n):
        t = i / RATE
        f = 660 + 14 * math.sin(2 * math.pi * 5 * t)
        phase += 2 * math.pi * f / RATE
        out.append(math.sin(phase) + 0.18 * math.sin(2 * phase) + 0.08 * math.sin(3 * phase))
    return [0.55 * v for v in out]


CANDIDATES = {
    "synth_capacitor_whine.wav": capacitor_whine,
    "synth_heavy_railgun.wav": heavy_railgun,
    "synth_energy_buildup.wav": energy_buildup,
}


RESOURCES = {
    "src/SoRCharacters/Resources/ready_tone.wav": ready_tone,
}


def write_raw(path, samples):
    """Writes without normalizing, so quiet sounds stay quiet."""
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(32000 * max(-1.0, min(1.0, s)))) for s in samples))


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "--resources":
        for path, make in RESOURCES.items():
            write_raw(path, make())
            print("wrote", path)
        return
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "art/sounds")
    out.mkdir(parents=True, exist_ok=True)
    for name, make in CANDIDATES.items():
        write(out / name, make())
        print("wrote", out / name)


if __name__ == "__main__":
    main()
