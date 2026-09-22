#!/usr/bin/env python3
"""Compare bracketed frozen-factory controls; these are not gameplay speedups."""
import argparse
import csv
import json
from datetime import datetime, timedelta
from pathlib import Path


def compare(analysis, performance, telemetry, offset_hours=2):
    stages = {row['workload']: row for row in performance['workloads']}
    gpu_rows = []
    for row in telemetry:
        values = list(row.values())
        gpu_rows.append((datetime.strptime(values[0].strip(), '%Y/%m/%d %H:%M:%S.%f'),
                         int(values[1]), int(values[2].strip().split()[0]), values[-1].strip() == 'Active'))

    def thermal(name):
        start = datetime.fromisoformat(stages[name]['startedUtc'].replace('Z', '+00:00')).replace(tzinfo=None)
        start += timedelta(hours=offset_hours)
        end = start + timedelta(seconds=analysis['workloads'][name]['sampled_seconds'])
        rows = [row for row in gpu_rows if start <= row[0] <= end]
        if not rows:
            return None
        clocks = sorted(row[2] for row in rows)
        return {'samples': len(rows), 'thermal_active_samples': sum(row[3] for row in rows),
                'temperature_min_c': min(row[1] for row in rows), 'temperature_max_c': max(row[1] for row in rows),
                'clock_min_mhz': clocks[0], 'clock_median_mhz': clocks[len(clocks)//2], 'clock_max_mhz': clocks[-1]}

    result = {'conditions': 'Frozen scene, diagnostic quality/visibility controls, not gameplay speedups. Each control is bracketed by restored baselines; round 2 reverses order. Baseline reference is mean of the two GPU medians. Thermal telemetry is sparse and does not remove hardware variation.', 'comparisons': []}
    for name in stages:
        if not name.startswith('gpu-') or not name.endswith('-control'):
            continue
        prefix = name.removesuffix('-control')
        triple = [prefix + suffix for suffix in ('-before', '-control', '-after')]
        for stage in triple:
            if stage not in analysis['workloads']:
                raise ValueError(f'Missing bracket {stage}')
        medians = [analysis['workloads'][stage]['thread_samples']['gpu_ms']['median'] for stage in triple]
        baseline = (medians[0] + medians[2]) / 2
        result['comparisons'].append({'control': prefix, 'gpu_median_ms_before_control_after': medians,
            'gpu_median_reduction_percent': 100 * (baseline - medians[1]) / baseline,
            'baseline_drift_percent': 100 * abs(medians[2] - medians[0]) / baseline,
            'frame_p95_ms_before_control_after': [analysis['workloads'][stage]['frames']['p95'] for stage in triple],
            'srp_draw_mean_before_control_after': [stages[stage]['meanSrpDrawCalls'] for stage in triple],
            'thermal_before_control_after': [thermal(stage) for stage in triple]})
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path, help='Contains analysis.json, performance.json and gpu-telemetry.csv')
    parser.add_argument('--offset-hours', type=float, default=2, help='Telemetry timezone UTC offset from build identity')
    args = parser.parse_args()
    with (args.directory / 'gpu-telemetry.csv').open(encoding='utf-8-sig') as source:
        telemetry = list(csv.DictReader(source))
    result = compare(json.loads((args.directory / 'analysis.json').read_text()),
                     json.loads((args.directory / 'performance.json').read_text()), telemetry, args.offset_hours)
    (args.directory / 'graphics-comparison.json').write_text(json.dumps(result, indent=2) + '\n')
    for row in result['comparisons']:
        print(row['control'], [round(v, 2) for v in row['gpu_median_ms_before_control_after']],
              f"reduction {row['gpu_median_reduction_percent']:.1f}%, baseline drift {row['baseline_drift_percent']:.1f}%")
