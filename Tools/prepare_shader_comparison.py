#!/usr/bin/env python3
"""Stage repository-owned historical shaders in an isolated verification project."""
import argparse
import hashlib
import json
import re
import subprocess
import uuid
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('project', type=Path)
parser.add_argument('--reference', default='3034df3')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
project = args.project.resolve()
if project == root or not (project / 'ProjectSettings/ProjectVersion.txt').is_file():
    parser.error('Use an existing isolated Unity project, never the working project.')
destination = project / 'Assets/RivetReach/Resources/Verification'
destination.mkdir(parents=True, exist_ok=True)
commit = subprocess.check_output(['git', 'rev-parse', args.reference + '^{commit}'], cwd=root, text=True).strip()
manifest = {'reference_commit': commit, 'files': {}}
for name in ['WorldLit.shader', 'HeldBlock.shader', 'HeldTool.shader', 'ExplorerSkin.shader', 'WorldLighting.hlsl', 'VoxelLight.hlsl']:
    path = 'Assets/RivetReach/Resources/Materials/' + name
    original = subprocess.check_output(['git', 'show', commit + ':' + path], cwd=root)
    text = original.decode().replace('Shader "RivetReach/', 'Shader "Hidden/RivetReach/Reference/')
    (destination / name).write_text(text)
    meta = destination / (name + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid5(uuid.NAMESPACE_URL, 'rivet-reach-shader-reference/' + name).hex + '\n')
    manifest['files'][path] = hashlib.sha256(original).hexdigest()
# Preserve the actual material keyword variants in the player build. Merely
# including a reference Shader in Resources can strip its shader_feature sets.
def guid(path):
    return re.search(r'^guid: (\w+)', path.read_text(), re.M).group(1)
old_guid = guid(project / 'Assets/RivetReach/Resources/Materials/WorldLit.shader.meta')
new_guid = guid(destination / 'WorldLit.shader.meta')
variants = destination / 'Materials'
variants.mkdir(exist_ok=True)
manifest['material_variants'] = {}
for path in sorted((project / 'Assets/RivetReach/Resources').rglob('*.mat')):
    if destination in path.parents:
        continue
    original = path.read_text()
    if 'guid: ' + old_guid not in original:
        continue
    identity = hashlib.sha256(str(path.relative_to(project)).encode()).hexdigest()[:16]
    (variants / (identity + '.mat')).write_text(original.replace('guid: ' + old_guid, 'guid: ' + new_guid))
    manifest['material_variants'][str(path.relative_to(project))] = hashlib.sha256(original.encode()).hexdigest()
(project / 'Logs/shader-reference.json').write_text(json.dumps(manifest, indent=2) + '\n')
print('Staged six reference files and', len(manifest['material_variants']), 'material variants from', commit, 'in', destination)
