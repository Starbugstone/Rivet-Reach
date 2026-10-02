#!/usr/bin/env python3
"""Read-only recipe reachability and exact batch accounting; not a timed playtest."""
import argparse
from collections import Counter
from pathlib import Path
import hashlib
import json
import math


def review(catalog):
    items = {i['definition']['stableId']: i for i in catalog['items']}
    recipes = catalog['recipes']
    grid = [r for r in recipes if r['grid']]
    roots = {f'rivet:{x}' for x in (
        'log dirt leaves sand sandstone red_clay snow potato wheat_seed grain flax_seed '
        'flax_fibre carrot_seed carrot berry_seed berries mushroom apple sapling '
        'raw_chicken feather egg floater_rock').split()}
    available = set(roots)
    mined = [('wood_pickaxe', 'cobblestone coal'), ('stone_pickaxe', 'raw_copper raw_iron'),
             ('copper_pickaxe', 'azure_ore'), ('iron_pickaxe', 'diamond raw_gold'),
             ('diamond_pickaxe', 'lava_rock')]
    reached = set()
    waves = []
    while True:
        before = set(available)
        for tool, resources in mined:
            if f'rivet:{tool}' in available:
                available.update(f'rivet:{x}' for x in resources.split())
        if 'rivet:fishing_rod' in available:
            available.add('rivet:raw_fish')
        if 'rivet:bucket' in available:
            available.update(['rivet:water_bucket', 'rivet:lava_bucket'])
        for r in recipes:
            if r['station'] and r['station'] not in available:
                continue
            if r['fuels'] and not available.intersection(r['fuels']):
                continue
            if r['watts'] and not (
                {'rivet:battery_block', 'rivet:hand_crank'} <= available or
                {'rivet:boiler_engine', 'rivet:alternator', 'rivet:pump'} <= available):
                continue
            if all(not i['item'] or i['item'] in available for i in r['ingredients']):
                available.add(r['output']['item'])
                reached.add(r['id'])
        if available == before:
            break
        waves.append(sorted(available - before))

    # Reproduce authored batches and retain leftovers. This deliberately uses
    # ordinary raw-ore smelting before the first crusher, not free ore doubling.
    base = {f'rivet:{x}' for x in 'log cobblestone iron_ingot copper_ingot gold_ingot diamond coal sand azure_crystal floater_rock'.split()}
    preferred = {r['output']['item']: r for r in reversed(grid)}
    startup = dict(workbench=1, wood_pickaxe=1, stone_pickaxe=1, iron_pickaxe=1,
                   stone_axe=1, furnace=1, torch=8)
    manual = dict(startup, wrench=1, chest=2, item_pipe=8, machinist_bench=1)
    powered = dict(manual, boiler_engine=1, alternator=1, pump=1, crusher=1,
                   bucket=1, fluid_pipe=4, power_cable=4)
    extraction = dict(powered, drill=1, item_pipe=12, power_cable=8)

    def bill(targets):
        supply, materials, crafts = Counter(), Counter(), Counter()

        def take(item, count, visiting=()):
            held = min(supply[item], count)
            supply[item] -= held
            count -= held
            if not count:
                return
            if item in base:
                materials[item] += count
                return
            if item in visiting or item not in preferred:
                raise ValueError(f'No bootstrap expansion: {item}, {visiting}')
            recipe = preferred[item]
            batches = math.ceil(count / recipe['output']['count'])
            for ingredient in recipe['ingredients']:
                if ingredient['item']:
                    take(ingredient['item'], ingredient['count'] * batches, visiting + (item,))
            crafts[recipe['id']] += batches
            supply[item] += batches * recipe['output']['count'] - count

        for item, count in targets.items():
            take('rivet:' + item, count)
        smelts = sum(materials[f'rivet:{metal}_ingot'] for metal in ('iron', 'copper', 'gold'))
        return dict(targets=targets, materials=dict(sorted(materials.items())),
                    leftovers={k: v for k, v in sorted(supply.items()) if v},
                    crafts=dict(sorted(crafts.items())), bootstrap_smelts=smelts,
                    single_furnace_processing_seconds=smelts * 10,
                    minimum_extra_coal_for_those_smelts=math.ceil(smelts / 8))

    advanced = {}
    for material in ['gold_ingot', 'diamond', 'floater_rock', 'azure_crystal', 'feather', 'lava_rock']:
        advanced[material] = [r['output'] for r in grid if any(i['item'] == 'rivet:' + material for i in r['ingredients'])]
    return dict(items=len(items), grid_recipes=len(grid), all_recipes=len(recipes),
                natural_roots=sorted(roots), reachability_waves=waves,
                unreachable_grid_recipes=[r['id'] for r in grid if r['id'] not in reached],
                bills={name: bill(target) for name, target in [('startup', startup), ('piped_furnace', manual),
                    ('powered_ore_processing', powered), ('with_drill', extraction)]},
                direct_material_uses=advanced,
                limitations=['Reachability assumes eventual access to natural resources, combat drops, water, fuel, and exploration; it is not a seed or time guarantee.',
                    'Bills are one explicitly listed compact layout; include authored batch rounding and separate persistent starter workshop.',
                    'Bills exclude shelter, food, armor, replacement tools, exploration tunnels, operating fuel/water and terrain-specific extra pipes.',
                    'Smelt time is furnace work, overlaps player actions, and excludes fuel burn wasted between loads. Coal is additional to any recipe coal.',
                    'Tagged food recipes use catalog example ingredients; compost quantity is not modeled by bills.'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog', type=Path, default=Path('.docs/wiki-data/catalog.json'))
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    raw = args.catalog.read_bytes()
    result = review(json.loads(raw))
    result['catalog_sha256'] = hashlib.sha256(raw).hexdigest()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: result[k] for k in ['items', 'grid_recipes', 'all_recipes', 'unreachable_grid_recipes']}, indent=2))
    for name, bill in result['bills'].items():
        print(name, bill['materials'], 'smelt seconds:', bill['single_furnace_processing_seconds'])
