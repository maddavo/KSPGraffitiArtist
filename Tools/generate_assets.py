"""Generate original spray-can geometry and PNGs using only Python's standard library.

Writes Unity coordinates directly into KSP's .mu v5 binary format. Format reference:
https://github.com/taniwha/io_object_mu (reference only; no code or art vendored).
"""
import math
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[1] / "GameData" / "KSPGraffitiArtist"


def png(path, width, height, pixel):
    def chunk(name, data):
        return struct.pack(">I", len(data)) + name + data + struct.pack(">I", zlib.crc32(name + data))
    rows = b"".join(b"\0" + bytes(c for x in range(width) for c in pixel(x, y)) for y in range(height))
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
                     + chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b""))


class Writer:
    def __init__(self): self.data = bytearray()
    def ints(self, *values): self.data.extend(struct.pack("<" + "i" * len(values), *values))
    def floats(self, *values): self.data.extend(struct.pack("<" + "f" * len(values), *values))
    def string(self, value):
        value = value.encode("utf8")
        count = len(value)
        while count > 127:
            self.data.append((count & 127) | 128)
            count >>= 7
        self.data.append(count)
        self.data.extend(value)
    def transform(self, name):
        self.string(name)
        self.floats(0, 0, 0, 0, 0, 0, 1, 1, 1, 1)
        self.ints(24)
        self.string("Untagged")
        self.ints(0)


def cylinder(writer, name, radius, bottom, top, material):
    count = 32
    positions, normals, uv, triangles = [], [], [], []
    for i in range(count + 1):
        angle = i * math.tau / count
        x, z = math.cos(angle), math.sin(angle)
        for y, v in [(bottom, 0), (top, 1)]:
            positions.append((x * radius, y, z * radius))
            normals.append((x, 0, z))
            uv.append((i / count, v))
    for i in range(count):
        k = i * 2
        triangles.extend((k, k+1, k+3, k, k+3, k+2))
    for y, normal_y in [(bottom, -1), (top, 1)]:
        start = len(positions)
        positions.append((0, y, 0)); normals.append((0, normal_y, 0)); uv.append((.5, .5))
        for i in range(count + 1):
            angle = i * math.tau / count
            x, z = math.cos(angle), math.sin(angle)
            positions.append((x * radius, y, z * radius)); normals.append((0, normal_y, 0)); uv.append(((x+1)/2, (z+1)/2))
        for i in range(count):
            a, b = start+i+1, start+i+2
            triangles.extend((start, a, b) if normal_y < 0 else (start, b, a))
    writer.ints(0); writer.transform(name)
    writer.ints(7, 13, len(positions), 1, 14)
    for p in positions: writer.floats(*p)
    writer.ints(15)
    for p in uv: writer.floats(*p)
    writer.ints(17)
    for p in normals: writer.floats(*p)
    writer.ints(19, len(triangles), *triangles, 22, 8)
    writer.data.extend(b"\x01\x01")  # cast and receive shadows
    writer.ints(1, material, 1)


def main():
    parts = ROOT / "Parts" / "SprayCan"
    images = ROOT / "Images"
    parts.mkdir(parents=True, exist_ok=True); images.mkdir(parents=True, exist_ok=True)
    w = Writer(); w.ints(76543, 5); w.string("GraffitiArtistSprayCan"); w.transform("sprayCan")
    w.ints(28); w.data.append(0); w.floats(.072, .2, .072, 0, .1, 0)
    # Original 7 cm diameter, 20 cm tall can with a silver shoulder and small black nozzle.
    cylinder(w, "body", .035, 0, .155, 0)
    cylinder(w, "label", .0352, .04, .12, 3)
    cylinder(w, "base", .036, 0, .008, 1)
    cylinder(w, "shoulder", .03, .155, .17, 1)
    cylinder(w, "valve", .011, .17, .182, 1)
    cylinder(w, "nozzle", .014, .182, .2, 2)
    w.ints(10, 4)
    colors = [(35, 105, 200, 255), (195, 200, 205, 255), (25, 25, 28, 255), (245, 220, 70, 255)]
    for i, color in enumerate(colors):
        w.string("canMaterial" + str(i)); w.string("KSP/Diffuse"); w.ints(1)
        w.string("_MainTex"); w.ints(4, i); w.floats(1, 1, 0, 0)
        png(parts / ("canColor" + str(i) + ".png"), 2, 2, lambda x, y, c=color: c)
    w.ints(12, 4)
    for i in range(4): w.string("canColor" + str(i)); w.ints(0)
    (parts / "sprayCan.mu").write_bytes(w.data)
    def arrow(x, y):
        # Arrow pointing right, transparent around it.
        shaft = 24 <= x <= 145 and 47 <= y <= 81
        head = 120 <= x <= 230 and abs(y-64) <= (230-x)*.5
        return (255, 220, 30, 255) if shaft or head else (0, 0, 0, 0)
    png(images / "direction-arrow.png", 256, 128, arrow)
    print("Generated original sprayCan.mu and five PNG assets")


if __name__ == "__main__": main()
