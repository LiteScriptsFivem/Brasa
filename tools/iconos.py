"""Genera el logo de Brasa (.ico multi-tamaño, .png) y los iconos de estado de la bandeja."""
from PIL import Image, ImageDraw
import os, sys

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

TOP = (255, 146, 43)
BOT = (226, 52, 28)


NAR = (238, 116, 38)
AZU = (104, 168, 218)
F1 = (46, 51, 58)
F2 = (28, 31, 36)


def logo(size, punto=None):
    """Brasa x LiteScripts: servidor naranja entre llaves { } azules, sobre fondo oscuro."""
    S = 4
    W = size * S
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))

    grad = Image.new("RGBA", (W, W))
    gd = ImageDraw.Draw(grad)
    for y in range(W):
        k = y / (W - 1)
        gd.line([(0, y), (W, y)], fill=tuple(int(F1[i] + (F2[i] - F1[i]) * k) for i in range(3)) + (255,))
    mask = Image.new("L", (W, W), 0)
    m = int(W * 0.03)
    rad = int(W * 0.23)
    ImageDraw.Draw(mask).rounded_rectangle([m, m, W - m - 1, W - m - 1], radius=rad, fill=255)
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    # borde naranja fino
    d.rounded_rectangle([m, m, W - m - 1, W - m - 1], radius=rad, outline=NAR + (255,), width=max(S, int(W * 0.035)))

    pequeno = size <= 24
    # llaves { } azules
    grosor = max(S * 1.2, W * (0.07 if pequeno else 0.055))
    def llave(x, sentido):
        y0, y1 = W * 0.24, W * 0.76
        ym = (y0 + y1) / 2
        a = W * 0.06 * sentido
        pts = [(x + a, y0), (x, y0 + W * 0.05), (x, ym - W * 0.05), (x - a, ym),
               (x, ym + W * 0.05), (x, y1 - W * 0.05), (x + a, y1)]
        d.line(pts, fill=AZU + (255,), width=int(grosor), joint="curve")
    llave(W * 0.20, 1)
    llave(W * 0.80, -1)

    # servidor naranja
    barras = 2 if pequeno else 3
    x0, x1 = W * 0.31, W * 0.69
    alto = W * (0.17 if pequeno else 0.125)
    hueco = W * (0.08 if pequeno else 0.055)
    total = barras * alto + (barras - 1) * hueco
    y = (W - total) / 2
    for i in range(barras):
        d.rounded_rectangle([x0, y, x1, y + alto], radius=alto * 0.3, fill=NAR + (255,))
        r = alto * 0.19
        cx, cy = x0 + alto * 0.5, y + alto / 2
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=AZU + (255,) if i == 0 else F2 + (255,))
        if not pequeno:
            for k in range(2):
                lx = x1 - alto * (0.5 + k * 0.36)
                d.line([lx, y + alto * 0.3, lx, y + alto * 0.7], fill=F2 + (255,), width=max(1, int(W * 0.012)))
        y += alto + hueco

    if punto:
        r = W * 0.22
        cx, cy = W - r - W * 0.01, W - r - W * 0.01
        d.ellipse([cx - r - W * 0.05, cy - r - W * 0.05, cx + r + W * 0.05, cy + r + W * 0.05], fill=F2 + (255,))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=punto + (255,))

    return img.resize((size, size), Image.LANCZOS)


def frame_bmp(img):
    """Entrada ICO clasica: BITMAPINFOHEADER + BGRA de abajo arriba + mascara AND."""
    import struct
    w, h = img.size
    cab = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, 0, 0, 0, 0, 0)
    px = img.load()
    xor = bytearray()
    for y in range(h - 1, -1, -1):
        for x in range(w):
            r, g, b, a = px[x, y]
            xor += bytes((b, g, r, a))
    fila = ((w + 31) // 32) * 4
    andm = bytearray()
    for y in range(h - 1, -1, -1):
        bits = bytearray(fila)
        for x in range(w):
            if px[x, y][3] == 0:
                bits[x // 8] |= 0x80 >> (x % 8)
        andm += bits
    return cab + bytes(xor) + bytes(andm)


def guardar_ico(nombre, punto=None, tamanos=(16, 20, 24, 32, 40, 48, 64, 128, 256)):
    import struct
    datos = [frame_bmp(logo(s, punto)) for s in tamanos]
    cab = struct.pack("<HHH", 0, 1, len(datos))
    off = 6 + 16 * len(datos)
    entradas = b""
    for s, d in zip(tamanos, datos):
        entradas += struct.pack("<BBBBHHII", s % 256, s % 256, 0, 0, 1, 32, len(d), off)
        off += len(d)
    with open(os.path.join(OUT, nombre), "wb") as f:
        f.write(cab + entradas + b"".join(datos))


guardar_ico("brasa.ico")
logo(256).save(os.path.join(OUT, "logo.png"))
estados = {
    "verde": (34, 197, 94),
    "ambar": (245, 158, 11),
    "rojo": (239, 68, 68),
    "gris": (120, 125, 135),
}
for n, c in estados.items():
    guardar_ico(f"bandeja-{n}.ico", c, (16, 20, 24, 32, 40, 48))
    logo(64, c).save(os.path.join(OUT, f"prev-{n}.png"))
print("ok")
