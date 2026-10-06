"""Gera os sprites e sons do jogo "Tanque Cheio" proceduralmente.

Uso:  python Tools/generate_assets.py
Saída: UnityTutorial/Assets/Sprites e UnityTutorial/Assets/Audio

O layout da cidade precisa ser igual ao de CityMap.cs.
"""
import math
import os
import random
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITES = os.path.join(ROOT, "UnityTutorial", "Assets", "Sprites")
AUDIO = os.path.join(ROOT, "UnityTutorial", "Assets", "Audio")
os.makedirs(SPRITES, exist_ok=True)
os.makedirs(AUDIO, exist_ok=True)

random.seed(7)
np.random.seed(7)

# ---------------------------------------------------------------- cidade
# Igual a CityMap.Rows ('#' prédio, 'P' parque, 'G' posto, '.' rua). Linha 0 = topo.
ROWS = [
    "......................",
    "......................",
    "..###..###..GGG..###..",
    "..###..###..GGG..###..",
    "..###..###..GGG..###..",
    "......................",
    "......................",
    "..###..PPP..###..###..",
    "..###..PPP..###..###..",
    "..###..PPP..###..###..",
    "......................",
    "......................",
]
W, H = len(ROWS[0]), len(ROWS)
ASPHALT = (58, 61, 68)
SIDEWALK = (150, 150, 142)
CURB = (110, 110, 104)


def blocks():
    """Agrupa as células não-rua em blocos retangulares (x, y, w, h, tipo) em células, y a partir do topo."""
    seen = set()
    result = []
    for y in range(H):
        for x in range(W):
            c = ROWS[y][x]
            if c == "." or (x, y) in seen:
                continue
            w = 1
            while x + w < W and ROWS[y][x + w] == c:
                w += 1
            h = 1
            while y + h < H and all(ROWS[y + h][x + i] == c for i in range(w)):
                h += 1
            for yy in range(y, y + h):
                for xx in range(x, x + w):
                    seen.add((xx, yy))
            result.append((x, y, w, h, c))
    return result


def draw_city():
    S = 128  # px por célula no desenho (reduz para 64 no final)
    img = Image.new("RGB", (W * S, H * S), ASPHALT)
    px = np.array(img).astype(np.int16)
    noise = np.random.randint(-6, 7, size=(H * S, W * S, 1))
    px = np.clip(px + noise, 0, 255).astype(np.uint8)
    img = Image.fromarray(px)
    d = ImageDraw.Draw(img)

    # faixas tracejadas no centro das ruas (ruas têm 2 células)
    road_cols = [1, 6, 11, 16, 21]
    road_rows = [1, 6, 11]
    dash, gap, lw = 0.35 * S, 0.3 * S, int(0.06 * S)

    def in_intersection_v(yc):
        return any(abs(yc - r) < 1.2 for r in road_rows)

    def in_intersection_h(xc):
        return any(abs(xc - c) < 1.2 for c in road_cols)

    for cx in road_cols:
        y = 0.0
        while y < H * S:
            yc = (y + dash / 2) / S
            if not in_intersection_v(yc):
                d.rectangle([cx * S - lw / 2, y, cx * S + lw / 2, y + dash], fill=(230, 210, 90))
            y += dash + gap
    for cy in road_rows:
        x = 0.0
        while x < W * S:
            xc = (x + dash / 2) / S
            if not in_intersection_h(xc):
                d.rectangle([x, cy * S - lw / 2, x + dash, cy * S + lw / 2], fill=(230, 210, 90))
            x += dash + gap

    # faixas de pedestre na borda das esquinas
    def zebra(x0, y0, horizontal):
        for i in range(5):
            if horizontal:
                d.rectangle([x0 + i * 0.4 * S + 0.05 * S, y0, x0 + i * 0.4 * S + 0.25 * S, y0 + 0.35 * S], fill=(215, 215, 210))
            else:
                d.rectangle([x0, y0 + i * 0.4 * S + 0.05 * S, x0 + 0.35 * S, y0 + i * 0.4 * S + 0.25 * S], fill=(215, 215, 210))

    for cx in road_cols:
        for cy in road_rows:
            for side in (-1, 1):
                yz = cy * S + side * 1.0 * S + (0 if side > 0 else -0.35 * S)
                if 0 <= yz < H * S - 0.3 * S:
                    zebra(cx * S - S, yz, True)
                xz = cx * S + side * 1.0 * S + (0 if side > 0 else -0.35 * S)
                if 0 <= xz < W * S - 0.3 * S:
                    zebra(xz, cy * S - S, False)

    roof_colors = [(120, 92, 80), (86, 98, 120), (140, 128, 104), (96, 110, 92), (118, 84, 104)]
    for i, (bx, by, bw, bh, kind) in enumerate(blocks()):
        x0, y0, x1, y1 = bx * S, by * S, (bx + bw) * S, (by + bh) * S
        d.rectangle([x0, y0, x1, y1], fill=CURB)
        d.rectangle([x0 + 6, y0 + 6, x1 - 6, y1 - 6], fill=SIDEWALK)
        # juntas da calçada
        for k in range(1, bw * 3):
            d.line([x0 + k * S / 3, y0 + 6, x0 + k * S / 3, y0 + 0.2 * S], fill=(135, 135, 128), width=3)
            d.line([x0 + k * S / 3, y1 - 0.2 * S, x0 + k * S / 3, y1 - 6], fill=(135, 135, 128), width=3)
        inset = int(0.22 * S)
        ix0, iy0, ix1, iy1 = x0 + inset, y0 + inset, x1 - inset, y1 - inset
        if kind == "P":
            d.rectangle([ix0, iy0, ix1, iy1], fill=(70, 140, 70))
            d.line([ix0, (iy0 + iy1) / 2, ix1, (iy0 + iy1) / 2], fill=(190, 170, 120), width=int(0.25 * S))
            d.line([(ix0 + ix1) / 2, iy0, (ix0 + ix1) / 2, iy1], fill=(190, 170, 120), width=int(0.25 * S))
            d.ellipse([(ix0 + ix1) / 2 - 0.4 * S, (iy0 + iy1) / 2 - 0.4 * S, (ix0 + ix1) / 2 + 0.4 * S, (iy0 + iy1) / 2 + 0.4 * S], fill=(90, 150, 200), outline=(170, 170, 160), width=8)
            for _ in range(14):
                tx, ty = random.uniform(ix0 + 30, ix1 - 30), random.uniform(iy0 + 30, iy1 - 30)
                if abs(tx - (ix0 + ix1) / 2) < 0.45 * S or abs(ty - (iy0 + iy1) / 2) < 0.45 * S:
                    continue
                r = random.uniform(0.18, 0.3) * S
                d.ellipse([tx - r + 8, ty - r + 8, tx + r + 8, ty + r + 8], fill=(40, 80, 40))
                d.ellipse([tx - r, ty - r, tx + r, ty + r], fill=(50, 115, 50))
                d.ellipse([tx - r * 0.5, ty - r * 0.6, tx + r * 0.2, ty - r * 0.1], fill=(80, 150, 70))
        elif kind == "G":
            # posto de gasolina: cobertura listrada e bombas
            d.rectangle([ix0, iy0, ix1, iy1], fill=(200, 200, 195))
            d.rectangle([ix0 + 20, iy0 + 20, ix1 - 20, iy0 + 0.9 * S], fill=(220, 40, 40))
            for k in range(8):
                xx = ix0 + 20 + k * (ix1 - ix0 - 40) / 8
                d.rectangle([xx, iy0 + 20, xx + (ix1 - ix0 - 40) / 16, iy0 + 0.9 * S], fill=(250, 250, 250))
            for k in range(3):
                cx = ix0 + (k + 0.75) * (ix1 - ix0) / 3.5
                d.rounded_rectangle([cx - 0.15 * S, iy0 + 1.3 * S, cx + 0.15 * S, iy0 + 1.75 * S], radius=10, fill=(240, 190, 30), outline=(60, 60, 60), width=6)
            d.rectangle([ix0 + 20, iy1 - 0.55 * S, ix1 - 20, iy1 - 20], fill=(90, 90, 95))
        else:
            color = roof_colors[i % len(roof_colors)]
            d.rectangle([ix0 + 10, iy0 + 10, ix1 + 10, iy1 + 10], fill=(40, 40, 44))  # sombra
            d.rectangle([ix0, iy0, ix1, iy1], fill=color)
            dark = tuple(max(0, c - 25) for c in color)
            light = tuple(min(255, c + 25) for c in color)
            d.rectangle([ix0, iy0, ix1, iy1], outline=dark, width=14)
            d.line([ix0 + 14, iy0 + 14, ix1 - 14, iy0 + 14], fill=light, width=6)
            # detalhes do telhado: ar-condicionado, caixa d'água, claraboias
            for _ in range(random.randint(3, 5)):
                ax, ay = random.uniform(ix0 + 40, ix1 - 90), random.uniform(iy0 + 40, iy1 - 90)
                sz = random.uniform(0.25, 0.45) * S
                d.rectangle([ax + 6, ay + 6, ax + sz + 6, ay + sz + 6], fill=dark)
                d.rectangle([ax, ay, ax + sz, ay + sz], fill=(170, 172, 176), outline=(110, 110, 115), width=4)
                d.ellipse([ax + sz * 0.2, ay + sz * 0.2, ax + sz * 0.8, ay + sz * 0.8], outline=(110, 110, 115), width=4)
    img = img.resize((W * 64, H * 64), Image.LANCZOS)
    img.save(os.path.join(SPRITES, "City.png"))
    return img


# ---------------------------------------------------------------- carros
def draw_car(body, police=False):
    k = 4
    w, h = 72 * k, 128 * k
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m = 6 * k
    # sombra
    shadow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle([m + 3 * k, m + 4 * k, w - m + 3 * k, h - m + 4 * k], radius=16 * k, fill=(0, 0, 0, 110))
    img = Image.alpha_composite(img, shadow.filter(ImageFilter.GaussianBlur(3 * k)))
    d = ImageDraw.Draw(img)
    # rodas
    for wy in (0.22, 0.72):
        for wx in (m - 2 * k, w - m - 8 * k):
            d.rounded_rectangle([wx, wy * h, wx + 10 * k, wy * h + 22 * k], radius=3 * k, fill=(25, 25, 25))
    outline = tuple(int(c * 0.55) for c in body)
    d.rounded_rectangle([m, m, w - m, h - m], radius=16 * k, fill=body, outline=outline, width=3 * k)
    if police:
        d.rounded_rectangle([m, m, w - m, 0.3 * h], radius=16 * k, fill=(30, 30, 35))
        d.rounded_rectangle([m, 0.78 * h, w - m, h - m], radius=16 * k, fill=(30, 30, 35))
        d.rectangle([m, 0.26 * h, w - m, 0.3 * h], fill=(30, 30, 35))
    else:
        # faixas de corrida
        d.rectangle([w / 2 - 9 * k, m, w / 2 - 4 * k, h - m], fill=(245, 245, 245))
        d.rectangle([w / 2 + 4 * k, m, w / 2 + 9 * k, h - m], fill=(245, 245, 245))
    glass = (60, 80, 105)
    # para-brisa (frente = topo da imagem)
    d.polygon([(m + 7 * k, 0.33 * h), (w - m - 7 * k, 0.33 * h), (w - m - 11 * k, 0.43 * h), (m + 11 * k, 0.43 * h)], fill=glass)
    d.polygon([(m + 11 * k, 0.70 * h), (w - m - 11 * k, 0.70 * h), (w - m - 8 * k, 0.77 * h), (m + 8 * k, 0.77 * h)], fill=glass)
    roof = tuple(min(255, int(c * 1.08)) for c in body)
    d.rounded_rectangle([m + 11 * k, 0.43 * h, w - m - 11 * k, 0.70 * h], radius=6 * k, fill=roof)
    if police:
        d.rounded_rectangle([m + 9 * k, 0.5 * h, w / 2, 0.57 * h], radius=2 * k, fill=(230, 40, 40))
        d.rounded_rectangle([w / 2, 0.5 * h, w - m - 9 * k, 0.57 * h], radius=2 * k, fill=(40, 90, 240))
    # faróis e lanternas
    d.ellipse([m + 4 * k, m + 2 * k, m + 16 * k, m + 10 * k], fill=(255, 245, 170))
    d.ellipse([w - m - 16 * k, m + 2 * k, w - m - 4 * k, m + 10 * k], fill=(255, 245, 170))
    d.rectangle([m + 5 * k, h - m - 7 * k, m + 16 * k, h - m - 3 * k], fill=(150, 10, 10))
    d.rectangle([w - m - 16 * k, h - m - 7 * k, w - m - 5 * k, h - m - 3 * k], fill=(150, 10, 10))
    # retrovisores
    d.ellipse([m - 4 * k, 0.36 * h, m + 3 * k, 0.4 * h], fill=outline)
    d.ellipse([w - m - 3 * k, 0.36 * h, w - m + 4 * k, 0.4 * h], fill=outline)
    return img.resize((w // k, h // k), Image.LANCZOS)


def draw_gas_can():
    k = 4
    s = 64 * k
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    body = (245, 180, 20)
    out = (120, 70, 0)
    d.rounded_rectangle([12 * k, 16 * k, 52 * k, 60 * k], radius=6 * k, fill=body, outline=out, width=3 * k)
    # alça
    d.rounded_rectangle([22 * k, 6 * k, 46 * k, 18 * k], radius=4 * k, fill=body, outline=out, width=3 * k)
    d.rounded_rectangle([27 * k, 10 * k, 41 * k, 15 * k], radius=2 * k, fill=(0, 0, 0, 0))
    # bico
    d.polygon([(12 * k, 20 * k), (4 * k, 8 * k), (9 * k, 5 * k), (17 * k, 17 * k)], fill=(70, 70, 70), outline=(30, 30, 30))
    # gota
    cx, cy = 32 * k, 40 * k
    d.polygon([(cx, cy - 13 * k), (cx - 8 * k, cy + 1 * k), (cx + 8 * k, cy + 1 * k)], fill=(40, 30, 20))
    d.ellipse([cx - 8 * k, cy - 6 * k, cx + 8 * k, cy + 10 * k], fill=(40, 30, 20))
    d.ellipse([cx - 4 * k, cy - 1 * k, cx, cy + 4 * k], fill=(255, 230, 140))
    return img.resize((64, 64), Image.LANCZOS)


def draw_oil():
    k = 4
    w, h = 128 * k, 96 * k
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for _ in range(9):
        cx, cy = random.uniform(0.3, 0.7) * w, random.uniform(0.35, 0.65) * h
        rx, ry = random.uniform(0.12, 0.25) * w, random.uniform(0.15, 0.3) * h
        d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(18, 16, 22, 235))
    img = img.filter(ImageFilter.GaussianBlur(2 * k))
    d = ImageDraw.Draw(img)
    d.arc([0.35 * w, 0.35 * h, 0.6 * w, 0.6 * h], 200, 300, fill=(120, 80, 160, 160), width=3 * k)
    d.arc([0.45 * w, 0.42 * h, 0.7 * w, 0.62 * h], 190, 280, fill=(60, 140, 160, 140), width=2 * k)
    return img.resize((128, 96), Image.LANCZOS)


def draw_glow():
    s = 128
    y, x = np.mgrid[0:s, 0:s]
    r = np.sqrt((x - s / 2 + 0.5) ** 2 + (y - s / 2 + 0.5) ** 2) / (s / 2)
    a = np.clip(1 - r, 0, 1) ** 2
    arr = np.zeros((s, s, 4), dtype=np.uint8)
    arr[..., :3] = 255
    arr[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def draw_heart():
    k = 4
    s = 64 * k
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = []
    for i in range(200):
        t = i / 200 * 2 * math.pi
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((s / 2 + x * s / 36, s / 2 - y * s / 36 + 2 * k))
    d.polygon(pts, fill=(255, 255, 255))
    return img.resize((64, 64), Image.LANCZOS)


def draw_fuel_icon():
    k = 4
    s = 64 * k
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    white = (255, 255, 255)
    d.rounded_rectangle([12 * k, 8 * k, 40 * k, 58 * k], radius=4 * k, fill=white)
    d.rectangle([17 * k, 14 * k, 35 * k, 28 * k], fill=(0, 0, 0, 0))
    d.rectangle([8 * k, 54 * k, 44 * k, 60 * k], fill=white)
    d.line([(40 * k, 22 * k), (50 * k, 30 * k), (50 * k, 46 * k), (55 * k, 46 * k), (55 * k, 18 * k), (48 * k, 12 * k)], fill=white, width=4 * k, joint="curve")
    return img.resize((64, 64), Image.LANCZOS)


def save(img, name):
    img.save(os.path.join(SPRITES, name))


# ---------------------------------------------------------------- áudio
RATE = 22050


def write_wav(name, samples, peak=0.85):
    samples = np.asarray(samples, dtype=np.float64)
    m = np.max(np.abs(samples)) or 1.0
    samples = samples / m * peak
    data = (samples * 32767).astype("<i2").tobytes()
    with wave.open(os.path.join(AUDIO, name), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)


def t_axis(dur):
    return np.arange(int(RATE * dur)) / RATE


def square(f, t, duty=0.5):
    return np.where((f * t) % 1 < duty, 1.0, -1.0)


def tri(f, t):
    return 2 * np.abs(2 * ((f * t) % 1) - 1) - 1


def saw(f, t):
    return 2 * ((f * t) % 1) - 1


def env(n, a=0.005, r=0.05, total=None):
    total = total or n / RATE
    t = np.arange(n) / RATE
    e = np.ones(n)
    e = np.minimum(e, t / a if a > 0 else 1)
    e = np.minimum(e, np.clip((total - t) / r, 0, 1))
    return e


def lowpass(x, alpha):
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += alpha * (v - acc)
        y[i] = acc
    return y


def note_freq(name):
    names = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}
    n, o = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[n] + 12 * (o + 1) - 69) / 12)


def music(bpm, chords, bars_per_chord, lead=True, drums=True, pad=False):
    beat = 60 / bpm
    bar = beat * 4
    total = bar * bars_per_chord * len(chords)
    n = int(total * RATE)
    out = np.zeros(n)

    def add(start, sig):
        i = int(start * RATE)
        end = min(n, i + len(sig))
        out[i:end] += sig[: end - i]

    for ci, chord in enumerate(chords):
        root = note_freq(chord[0])
        tones = [note_freq(x) for x in chord]
        c0 = ci * bars_per_chord * bar
        for b in range(bars_per_chord * 8):  # colcheias
            st = c0 + b * beat / 2
            t = t_axis(beat / 2 * 0.9)
            f = root / 2 if b % 2 == 0 else root
            add(st, 0.35 * square(f / 2, t, 0.25) * env(len(t), 0.003, 0.03))
        if lead:
            for b in range(bars_per_chord * 16):  # semicolcheias
                st = c0 + b * beat / 4
                t = t_axis(beat / 4 * 0.8)
                f = tones[b % len(tones)] * (2 if (b // 4) % 2 else 1)
                add(st, 0.13 * square(f, t, 0.125) * env(len(t), 0.002, 0.04))
        if pad:
            t = t_axis(bars_per_chord * bar)
            sig = sum(tri(f, t) + 0.5 * tri(f * 1.004, t) for f in tones)
            add(c0, 0.09 * sig * env(len(t), 0.3, 0.4))
    if drums:
        bars = int(round(total / bar))
        for b in range(bars):
            for q in range(4):
                st = b * bar + q * beat
                if q in (0, 2):
                    t = t_axis(0.18)
                    f = 120 * np.exp(-t * 25) + 45
                    add(st, 0.9 * np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t * 14))
                else:
                    t = t_axis(0.15)
                    add(st, 0.35 * np.random.uniform(-1, 1, len(t)) * np.exp(-t * 22))
                for e8 in (0, 0.5):
                    t = t_axis(0.04)
                    hat = np.random.uniform(-1, 1, len(t))
                    hat = hat - lowpass(hat, 0.5)
                    add(st + e8 * beat, 0.25 * hat * np.exp(-t * 90))
    return out


def gen_audio():
    # música do jogo: 150 BPM, Lá menor
    game = music(150, [("A3", "C4", "E4"), ("F3", "A3", "C4"), ("C3", "E3", "G3"), ("G3", "B3", "D4")], 2)
    write_wav("music_game.wav", game, 0.6)
    # música do menu: mais calma
    menu = music(100, [("A3", "C4", "E4"), ("F3", "A3", "C4"), ("C3", "E3", "G3"), ("E3", "G#3", "B3")], 2, lead=False, drums=False, pad=True)
    write_wav("music_menu.wav", menu, 0.55)

    # sirene: "uiuuu" (wail) em loop de 2s, com número inteiro de ciclos
    t = t_axis(2.0)
    f = 800 + 350 * np.sin(2 * np.pi * 0.5 * t - np.pi / 2)
    phase = np.cumsum(f) / RATE
    phase *= round(phase[-1]) / phase[-1]
    siren = 0.7 * np.sin(2 * np.pi * phase) + 0.3 * np.sign(np.sin(2 * np.pi * phase))
    write_wav("siren.wav", lowpass(siren, 0.35), 0.5)

    # motor: loop de 1s
    t = t_axis(1.0)
    eng = saw(50, t) * 0.6 + saw(100, t) * 0.3 + square(25, t) * 0.2
    eng *= 0.75 + 0.25 * np.sin(2 * np.pi * 200 * t)
    write_wav("engine.wav", lowpass(eng, 0.08), 0.5)

    # reabastecer: "glub glub" + "plim"
    parts = []
    for i in range(3):
        t = t_axis(0.07)
        f = 300 + 900 * (t / 0.07) ** 2 + i * 80
        parts.append(np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t * 20))
        parts.append(np.zeros(int(RATE * 0.015)))
    t = t_axis(0.35)
    ding = (np.sin(2 * np.pi * 1568 * t) + 0.5 * np.sin(2 * np.pi * 2093 * t)) * np.exp(-t * 9)
    write_wav("pickup.wav", np.concatenate(parts + [ding]), 0.7)

    # batida
    t = t_axis(0.7)
    noise = lowpass(np.random.uniform(-1, 1, len(t)), 0.25) * np.exp(-t * 7)
    thump = np.sin(2 * np.pi * np.cumsum(90 * np.exp(-t * 6) + 30) / RATE) * np.exp(-t * 8)
    clank = sum(np.sin(2 * np.pi * fr * t) for fr in (523, 789, 1311, 1873)) * np.exp(-t * 12) * 0.25
    write_wav("crash.wav", noise + thump + clank, 0.9)

    # game over e vitória
    seq = []
    for fr in (440, 392, 349, 262):
        t = t_axis(0.22)
        seq.append(square(fr, t, 0.5) * env(len(t), 0.005, 0.05) * 0.5)
    t = t_axis(0.6)
    seq.append(square(196, t, 0.5) * np.exp(-t * 3) * 0.5)
    write_wav("gameover.wav", np.concatenate(seq), 0.6)

    seq = []
    for fr in (523, 659, 784, 1047):
        t = t_axis(0.12)
        seq.append(square(fr, t, 0.25) * env(len(t), 0.003, 0.03))
    t = t_axis(0.7)
    seq.append((square(1047, t, 0.25) + square(1319, t, 0.25) * 0.6) * np.exp(-t * 3))
    write_wav("victory.wav", np.concatenate(seq), 0.6)

    # clique, alerta de gasolina baixa
    t = t_axis(0.06)
    write_wav("click.wav", square(880, t, 0.25) * np.exp(-t * 50), 0.5)
    t = t_axis(0.09)
    beep = square(1200, t, 0.5) * env(len(t), 0.002, 0.01)
    write_wav("lowfuel.wav", np.concatenate([beep, np.zeros(int(RATE * 0.06)), beep]), 0.45)


if __name__ == "__main__":
    draw_city()
    save(draw_car((214, 40, 40)), "CarRed.png")
    save(draw_car((240, 240, 240), police=True), "CarPolice.png")
    save(draw_gas_can(), "GasCan.png")
    save(draw_oil(), "Oil.png")
    save(draw_glow(), "Glow.png")
    save(draw_heart(), "Heart.png")
    save(draw_fuel_icon(), "FuelIcon.png")
    save(Image.new("RGBA", (8, 8), (255, 255, 255, 255)), "White.png")
    gen_audio()
    print("ok")
