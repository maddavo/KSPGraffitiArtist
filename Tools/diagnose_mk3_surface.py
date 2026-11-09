"""Read-only sample-vessel clipping reproduction; writes only a local report and optional test fixture.

Requires a separately obtained io_object_mu mu.py reader (not bundled). Usage:
  py -3 Tools/diagnose_mk3_surface.py <reader-directory> <KSP-root> <quicksave.sfs> <output-directory> <vessel-name> [half-height]
Stock mesh geometry in the generated fixture is local test evidence, not a redistributable asset.
"""
import base64
import hashlib
import json
import math
from pathlib import Path
import struct
import sys

sys.path.insert(0, sys.argv[1])
try:
    from mu import Mu
except ImportError:
    from mu_reference import Mu

game, save, out = Path(sys.argv[2]), Path(sys.argv[3]), Path(sys.argv[4])
if len(sys.argv) < 6:
    raise SystemExit('Supply reader directory, KSP root, save, output directory and vessel name.')
wanted_vessel = sys.argv[5]
rows = (-.08, 0, .08)
if len(sys.argv) > 6:
    half_height = float(sys.argv[6])
    rows = (-half_height, -half_height/2, 0, half_height/2, half_height)
out.mkdir(parents=True, exist_ok=True)
source = game / 'GameData/Squad/Parts/FuelTank/mk3Fuselage/CREW.mu'
model = Mu().read(str(source))


def add(a, b): return tuple(x+y for x, y in zip(a, b))
def sub(a, b): return tuple(x-y for x, y in zip(a, b))
def mul(a, s): return tuple(x*s for x in a)
def dot(a, b): return sum(x*y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a): return mul(a, 1/math.sqrt(dot(a, a)))
def unity(v): return v[0], v[2], v[1]


def transform(obj, v):
    t = obj.transform
    v = tuple(a*b for a, b in zip(v, unity(t.localScale)))
    q = t.localRotation
    xyz = -q[1], -q[3], -q[2]
    return add(add(v, add(mul(cross(xyz, v), 2*q[0]), mul(cross(xyz, cross(xyz, v)), 2))), unity(t.localPosition))


def mesh_triangles(obj):
    # MODEL placement replaces the exported top-level position; retain child transforms.
    mesh = obj.shared_mesh
    vertices = [transform(obj, unity(v)) for v in mesh.verts]
    return [(vertices[a], vertices[c], vertices[b]) for sm in mesh.submeshes for a, b, c in sm]


def ray(origin, direction, tri):
    a, b, c = tri
    e1, e2 = sub(b, a), sub(c, a)
    p = cross(direction, e2)
    det = dot(e1, p)
    if abs(det) < 1e-9: return None
    offset = sub(origin, a)
    u = dot(offset, p)/det
    if not 0 <= u <= 1: return None
    q = cross(offset, e1)
    v = dot(direction, q)/det
    if v < 0 or u+v > 1: return None
    distance = dot(e2, q)/det
    return distance if distance >= 0 else None


stack, pending, markings = [], '', []
for line in save.open(encoding='utf-8-sig'):
    s = line.strip()
    if s == '{': stack.append((pending, {})); pending = ''
    elif s == '}':
        if stack and stack[-1][0] == 'MARKING':
            vessel = next((v for n, v in reversed(stack) if n == 'VESSEL'), {})
            part = next((v for n, v in reversed(stack) if n == 'PART'), {})
            if vessel.get('name') == wanted_vessel and part.get('name') == 'mk3CrewCabin':
                markings.append(dict(stack[-1][1], part_uid=part.get('uid')))
        if stack: stack.pop()
    elif '=' in s and stack:
        k, v = s.split('=', 1); stack[-1][1][k.strip()] = v.strip()
    elif s: pending = s

if len(markings) != 1: raise RuntimeError('Expected one marked Mk3 part on ' + wanted_vessel)
m = markings[0]
visible = [tri for obj in model.obj.children if hasattr(obj, 'shared_mesh') and hasattr(obj, 'renderer') for tri in mesh_triangles(obj)]
collider_obj = next(o for o in model.obj.children if o.transform.name == 'Mk3COL')
collision = mesh_triangles(collider_obj)
centre = tuple(float(m[k]) for k in ('x', 'y', 'z'))
normal = unit(tuple(float(m[k]) for k in ('nx', 'ny', 'nz')))
up = unit(tuple(float(m[k]) for k in ('ux', 'uy', 'uz')))
angle = math.radians(-float(m['rotation']))
up = add(mul(up, math.cos(angle)), add(mul(cross(normal, up), math.sin(angle)), mul(normal, dot(normal, up)*(1-math.cos(angle)))))
right = cross(normal, up)
samples, gaps = [], []
for row in rows:
    for i in range(21):
        plane = add(centre, add(mul(right, (i/20-.5)*float(m['width'])), mul(up, row)))
        origin, direction = add(plane, mul(normal, 1.1)), mul(normal, -1)
        hits = sorted((d, tri) for tri in visible if (d := ray(origin, direction, tri)) is not None)
        old = min((d for tri in collision if (d := ray(origin, direction, tri)) is not None), default=None)
        if not hits or old is None: continue
        distance, tri = hits[0]
        face = unit(cross(sub(tri[1], tri[0]), sub(tri[2], tri[0])))
        if dot(face, normal) < 0: face = mul(face, -1)
        expected = 1.1-distance+dot(face, normal)*.0025
        samples.append((plane, normal, expected)); gaps.append((old-distance)*1000)
with (out / 'mk3-surface-fixture.bin').open('wb') as f:
    f.write(struct.pack('<i', len(visible)))
    for tri in visible:
        face = unit(cross(sub(tri[1], tri[0]), sub(tri[2], tri[0])))
        for v in (*tri, face): f.write(struct.pack('<3f', *v))
    f.write(struct.pack('<i', len(samples)))
    for plane, n, expected in samples: f.write(struct.pack('<7f', *plane, *n, expected))
report = dict(save=str(save), save_sha256=hashlib.sha256(save.read_bytes()).hexdigest(),
              vessel=wanted_vessel,
              model=str(source), model_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
              label=base64.b64decode(m['text64']).decode('utf8'), marking=m,
              matched_samples=len(samples), visible_minus_collider_mm=dict(min=min(gaps), max=max(gaps), mean=sum(gaps)/len(gaps)),
              old_lift_mm=2.5, status='Read-only offline geometry reproduction; native corrected appearance still pending')
(out / 'mk3-surface-report.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf8')
print(json.dumps(report, indent=2))
