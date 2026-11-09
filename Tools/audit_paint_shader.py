"""Read-only installed shader audit; needs separately installed UnityPy, never ships stock data.

Usage: python Tools/audit_paint_shader.py <UnityPy-parent> <KSP-root> <report.json>
Unity 2019 shader program offsets are relative to their recorded segment, not always segment 0.
"""
import hashlib
import json
from pathlib import Path
import struct
import sys

sys.path.insert(0, sys.argv[1])
import UnityPy
from UnityPy.export.ShaderConverter import ShaderSubProgram
from UnityPy.helpers import CompressionHelper
from UnityPy.streams import EndianBinaryReader

asset = Path(sys.argv[2]) / 'KSP_x64_Data/sharedassets0.assets'
environment = UnityPy.load(str(asset))
shaders = {}
for obj in environment.objects:
    if obj.type.name == 'Shader':
        shader = obj.read()
        if shader.m_ParsedForm.m_Name in ('Standard', 'Standard (Specular setup)'):
            shaders[shader.m_ParsedForm.m_Name] = shader

feature_names = {
    '_ALPHABLEND_ON', '_ALPHATEST_ON', '_ALPHAPREMULTIPLY_ON', '_NORMALMAP',
    '_EMISSION', '_SPECGLOSSMAP', '_METALLICGLOSSMAP', '_PARALLAXMAP', '_DETAIL_MULX2',
    '_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A', '_SPECULARHIGHLIGHTS_OFF', '_GLOSSYREFLECTIONS_OFF'
}


def programs(shader):
    result = {}
    for index, platform in enumerate(shader.platforms):
        segments = [CompressionHelper.decompress_lz4(bytes(shader.compressedBlob)[offset:offset+size], length)
                    for offset, size, length in zip(shader.offsets[index], shader.compressedLengths[index], shader.decompressedLengths[index])]
        keywords = []
        for program_index in range(struct.unpack_from('<i', segments[0])[0]):
            offset, size, segment = struct.unpack_from('<iii', segments[0], 4+12*program_index)
            reader = EndianBinaryReader(segments[segment], endian='<')
            reader.Position = offset
            subprogram = ShaderSubProgram(reader)
            keywords.append(set(subprogram.m_Keywords + (subprogram.m_LocalKeywords or [])))
        result[int(platform)] = keywords
    return result


old = programs(shaders['Standard'])
replacement = shaders['Standard (Specular setup)']
new = programs(replacement)
properties = {prop.m_Name for prop in replacement.m_ParsedForm.m_PropInfo.m_Props}
assert {'_MainTex', '_SpecColor', '_Glossiness', '_Mode', '_SrcBlend', '_DstBlend', '_ZWrite'} <= properties
checks = 1
rows = []
for platform, keys in new.items():
    old_keys = old[platform]
    fade = sum('_ALPHABLEND_ON' in key for key in old_keys)
    spec_off = sum('_SPECULARHIGHLIGHTS_OFF' in key for key in old_keys)
    glossy_off = sum('_GLOSSYREFLECTIONS_OFF' in key for key in old_keys)
    matches = [key for key in keys if key & feature_names == {'_ALPHAPREMULTIPLY_ON'}]
    shadow = sum('SHADOWS_SCREEN' in key for key in matches)
    assert fade == 0, 'Update diagnosis: installed Fade variants now exist'
    assert spec_off == 0 and glossy_off == 0, 'Update diagnosis: stripped highlight keywords changed'
    assert matches, 'Required replacement material profile absent'
    assert shadow, 'Replacement shadow-receiving variants absent'
    checks += 4
    rows.append(dict(platform=platform, old_fade_programs=fade, old_specular_off_programs=spec_off,
                     old_glossy_off_programs=glossy_off, replacement_programs=len(matches), replacement_shadow_programs=shadow))

report = dict(asset=str(asset), asset_sha256=hashlib.sha256(asset.read_bytes()).hexdigest().upper(),
              shader='Standard (Specular setup)', material_features=['_ALPHAPREMULTIPLY_ON'],
              platforms=rows, checks=checks, limitation='Compiled availability only; no GPU rendering or captured font texture.')
Path(sys.argv[3]).write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(f'PASS: {checks} installed compiled-shader checks across {len(rows)} platforms; native appearance pending.')
