"""Package a verified Windows alpha folder with source identity and payload hashes."""
import argparse
import datetime
import hashlib
import json
import re
from pathlib import Path
import subprocess
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('player', type=Path)
parser.add_argument('output', type=Path)
parser.add_argument('--source-commit', default='HEAD', help='Commit used for the build; only documentation may have changed since.')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]

def git(*arguments):
    return subprocess.check_output(['git', '-C', str(root), *arguments], text=True).strip()

if git('status', '--porcelain', '--untracked-files=no'):
    parser.error('Commit tracked changes before identifying a release payload.')
if not (args.player / 'RivetReach.exe').is_file() or not (args.player / 'RivetReach_Data').is_dir():
    parser.error('Expected the complete built player folder.')
source_commit = git('rev-parse', args.source_commit + '^{commit}')
if git('diff', '--name-only', source_commit, 'HEAD', '--', 'Assets', 'Packages', 'ProjectSettings', 'Tools/Build-Release.ps1'):
    parser.error('Build inputs differ from the supplied source commit; rebuild before packaging.')
settings = (root / 'ProjectSettings/ProjectSettings.asset').read_text()
version_match = re.search(r'^  bundleVersion: (\d+\.\d+\.\d+)\s*$', settings, re.M)
schema_match = re.search(r'public const int Format=(\d+);', (root / 'Assets/RivetReach/Code/Persistence/SaveIO.cs').read_text())
if not version_match or not schema_match:
    parser.error('Cannot determine the player version and save schema from the build inputs.')
version = version_match.group(1)
player_name = f'RivetReach-{version}-alpha-windows-x64'
if args.player.name != player_name:
    parser.error(f'Expected player directory named {player_name}.')
for required in ('UnityPlayer.dll', 'MonoBleedingEdge', 'RivetReach_Data/Managed/Assembly-CSharp.dll', 'README.md', 'LICENSE.md', 'THIRD_PARTY_NOTICES.md', 'licenses'):
    if not (args.player / required).exists():
        parser.error(f'Missing required player payload: {required}')
archive = args.output / (player_name + '.zip')
if archive.exists():
    parser.error('Use a new packaging output directory; existing archives are preserved.')
args.output.mkdir(parents=True, exist_ok=True)
files = sorted(p for p in args.player.rglob('*') if p.is_file()
               and not any('DoNotShip' in part for part in p.parts)
               and p.suffix.lower() not in {'.pdb', '.mdb'}
               and p.name != 'build-manifest.json')
manifest = {
    'product': 'Rivet Reach', 'version': version, 'channel': 'alpha',
    'platform': 'Windows x64', 'graphics_api': 'Direct3D11', 'unity': '6000.4.4f1', 'save_schema': int(schema_match.group(1)),
    'source_commit': source_commit, 'source_tree': git('rev-parse', source_commit + '^{tree}'),
    'packaging_commit': git('rev-parse', 'HEAD'),
    'packaged_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'files': [{'path': p.relative_to(args.player).as_posix(), 'bytes': p.stat().st_size,
               'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in files]
}
manifest_path = args.output / 'build-manifest.json'
manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as package:
    for path in files:
        package.write(path, (Path(args.player.name) / path.relative_to(args.player)).as_posix())
    package.write(manifest_path, args.player.name + '/build-manifest.json')
with zipfile.ZipFile(archive) as package:
    if package.testzip() is not None:
        raise RuntimeError('ZIP integrity check failed.')
checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
(args.output / 'SHA256SUMS.txt').write_text(f'{checksum}  {archive.name}\n', encoding='ascii')
print(f'{archive}: {archive.stat().st_size} bytes, {len(files)} payload files; SHA-256 {checksum}')
