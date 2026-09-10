"""Copy maintained player guides into an existing GitHub wiki checkout; commit/push separately."""
from pathlib import Path
import argparse
import re

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('checkout',type=Path)
args=parser.parse_args()
if not (args.checkout/'.git').exists():
    parser.error('Expected an initialized wiki Git checkout')
source=Path(__file__).resolve().parents[1]/'.docs/wiki'
for page in sorted(source.glob('*.md')):
    text=page.read_text(encoding='utf-8')
    # Local Markdown links stay checkable in the main repository; wiki URLs omit the extension.
    def link(match):
        target=match.group(1)
        if not (source/(target+'.md')).is_file():
            raise ValueError(f'{page.name}: broken local page link {target}')
        return ']('+target+')'
    text=re.sub(r'\]\(([A-Za-z0-9_-]+)\.md\)',link,text)
    (args.checkout/page.name).write_text(text,encoding='utf-8')
    print(page.name)
