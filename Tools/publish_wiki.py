"""Validate and copy .docs/wiki into a GitHub wiki checkout; commit/push separately."""
from html import unescape
from html.parser import HTMLParser
from functools import cache
from hashlib import sha256
from pathlib import Path
from urllib.parse import unquote, urlsplit
import argparse
import json
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / '.docs/wiki'
RAW = 'https://raw.githubusercontent.com/wiki/Starbugstone/Rivet-Reach/'
MANIFEST = '.rivet-wiki-manifest.json'


def anchors(text):
    seen = {}
    result = set()
    for heading in re.findall(r'^#{1,6}\s+(.+)$', text, re.M):
        slug = re.sub(r'[^\w\- ]', '', heading.lower()).replace(' ', '-')
        count = seen.get(slug, 0)
        seen[slug] = count + 1
        result.add(slug + (f'-{count}' if count else ''))
    return result


class Links(HTMLParser):
    def __init__(self, name, icon_targets):
        super().__init__()
        self.name = name
        self.icon_targets = icon_targets
        self.targets = []
        self.link = None

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == 'a':
            self.link = attrs.get('href')
        for key in ('href', 'src'):
            if key in attrs:
                self.targets.append(attrs[key])
        if tag == 'img' and attrs.get('src', '').startswith('icons/'):
            expected = self.icon_targets.get(attrs['src'])
            if not expected or not self.link or self.link.split('#')[0] != expected:
                raise ValueError(f'{self.name}: item icon must link to its item page: {attrs}')
            if not attrs.get('alt'):
                raise ValueError(f'{self.name}: icon needs alternative text')

    def handle_endtag(self, tag):
        if tag == 'a':
            self.link = None


def validate():
    # Reject stale game exports and hand-edited generated pages before copying anything.
    subprocess.run([sys.executable, str(ROOT / 'Tools/generate_wiki_items.py'), '--check'], check=True)
    catalog = json.loads((ROOT / '.docs/wiki-data/catalog.json').read_text())
    icon_targets = {'icons/' + str(i['definition']['runtimeId']) + '.png':
                    'Item-' + i['definition']['stableId'].split(':')[1].replace('_', '-') + '.md'
                    for i in catalog['items']}
    for path in SOURCE.rglob('*.png'):
        with path.open('rb') as stream:
            if stream.read(8) != b'\x89PNG\r\n\x1a\n':
                raise ValueError(f'Expected PNG bytes (not an LFS pointer): {path}')
    pages = {p.name: p.read_text(encoding='utf-8') for p in SOURCE.glob('*.md')}
    page_anchors = {name: anchors(text) for name, text in pages.items()}
    count = 0
    checked = set()
    for name, text in pages.items():
        parser = Links(name, icon_targets)
        parser.feed(text)
        targets = parser.targets + re.findall(r'\]\(([^\s)]+)\)', text)
        for target in targets:
            url = urlsplit(unescape(target))
            if url.scheme or url.netloc:
                continue
            path = unquote(url.path)
            count += 1
            key = (path or name, unquote(url.fragment))
            if key in checked:
                continue
            resolved = (SOURCE / path).resolve() if path else SOURCE / name
            if not resolved.is_relative_to(SOURCE.resolve()) or not resolved.is_file():
                raise ValueError(f'{name}: missing/escaping local target {target}')
            if url.fragment and resolved.suffix == '.md' and unquote(url.fragment) not in page_anchors[resolved.name]:
                raise ValueError(f'{name}: missing section {target}')
            checked.add(key)
    print(f'Validated {len(pages)} pages and {count} local links/images; all item icons link to their item page.')


@cache
def image_url(value):
    # Raw GitHub URLs can otherwise keep serving the old art after a wiki push.
    version = sha256((SOURCE / value).read_bytes()).hexdigest()[:12]
    return RAW + value + '?v=' + version


def render(text):
    def target(value):
        if re.fullmatch(r'[A-Za-z0-9_-]+\.md(?:#[^\s]*)?', value):
            return value.replace('.md', '', 1)
        if value.startswith(('icons/', 'images/')):
            return image_url(value)
        return value
    text = re.sub(r'\]\(([^\s)]+)\)', lambda m: '](' + target(m[1]) + ')', text)
    return re.sub(r'\b(href|src)="([^"]+)"', lambda m: m[1] + '="' + target(m[2]) + '"', text)


def publish(checkout):
    files = sorted([p for p in SOURCE.rglob('*') if p.is_file() and p.suffix in ('.md', '.png', '.jpg', '.webp')])
    relative = [p.relative_to(SOURCE).as_posix() for p in files]
    previous = json.loads((checkout / MANIFEST).read_text()) if (checkout / MANIFEST).exists() else []
    # Migrate import metadata copied by the older industry-only asset workflow.
    for path in (checkout / 'icons').glob('*.png.meta'):
        if (SOURCE / 'icons' / path.stem).is_file() and path.read_text().startswith('fileFormatVersion:'):
            path.unlink()
    # Only remove files recorded by a prior publish; preserve untracked remote edits.
    for name in set(previous) - set(relative):
        path = (checkout / name).resolve()
        if not path.is_relative_to(checkout.resolve()) or '.git' in Path(name).parts:
            raise ValueError('Unsafe prior manifest entry: ' + name)
        if path.is_file():
            path.unlink()
    for path, name in zip(files, relative):
        dest = checkout / name
        dest.parent.mkdir(parents=True, exist_ok=True)
        if path.suffix == '.md':
            dest.write_text(render(path.read_text(encoding='utf-8')), encoding='utf-8')
        else:
            shutil.copyfile(path, dest)
    (checkout / MANIFEST).write_text(json.dumps(relative, indent=2) + '\n')
    print(f'Copied {len(files)} pages/assets to {checkout}. Review, commit and push the wiki checkout separately.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('checkout', type=Path, nargs='?')
    parser.add_argument('--check', action='store_true', help='Validate maintained sources without copying')
    args = parser.parse_args()
    if not args.check and (args.checkout is None or not (args.checkout / '.git').exists()):
        parser.error('Expected an initialized wiki Git checkout, or use --check')
    validate()
    if not args.check:
        publish(args.checkout)


if __name__ == '__main__':
    main()
