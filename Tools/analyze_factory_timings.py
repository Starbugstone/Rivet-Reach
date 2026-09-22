#!/usr/bin/env python3
"""Summarize retail factory captures without adding nested/parallel scopes."""
import argparse
import csv
import gzip
import json
from collections import defaultdict
from pathlib import Path


def distribution(values):
    values = sorted(values)
    if not values:
        return None
    return {"count": len(values), "mean": sum(values) / len(values),
            "median": values[len(values) // 2],
            "p95": values[int((len(values) - 1) * .95)],
            "p99": values[int((len(values) - 1) * .99)], "max": values[-1]}


def analyze(path):
    opener = gzip.open if path.suffix == ".gz" else open
    with opener(path, "rt", newline="", encoding="utf-8-sig") as source:
        rows = list(csv.DictReader(source))
    if not rows:
        raise ValueError("Empty capture")
    scopes = [key.removesuffix("_wall_ms") for key in rows[0] if key.endswith("_wall_ms")]
    if len(scopes) != 27:
        raise ValueError(f"Expected 27 retail scopes, found {len(scopes)}")
    groups = defaultdict(list)
    for row in rows:
        groups[row["workload"]].append(row)
    report = {"source": str(path), "conditions": (
        "Scope milliseconds are inclusive wall-time totals between coroutine observations. "
        "Per-call values below are total divided by calls, not call-duration percentiles. "
        "Processing has two calls per factory tick. Parent/child scopes overlap; worker "
        "latency is parallel completed work, not main-thread time. Frame intervals precede "
        "observations; spike contexts retain neighbours. GPU/CPU samples are delayed and "
        "deduplicated by timing_timestamp. Zero/negative thread times are unavailable."),
        "workloads": {}}
    for name, group in groups.items():
        frames = [float(r["frame_ms"]) for r in group]
        seconds = sum(frames) / 1000
        for previous, current in zip(group, group[1:]):
            if int(current["unity_frame"]) != int(previous["unity_frame"]) + 1:
                raise ValueError(f"Nonconsecutive Unity frames in {name}")
        result = {"frames": distribution(frames), "sampled_seconds": seconds,
                  "over_60_budget": sum(v > 1000 / 60 for v in frames),
                  "over_45_floor": sum(v > 1000 / 45 for v in frames), "scopes": {}}
        unique = {r["timing_timestamp"]: r for r in group if r["timing_timestamp"] != "0"}
        result["thread_samples"] = {key: distribution([float(r[key]) for r in unique.values() if float(r[key]) > 0])
                                    for key in ("gpu_ms", "cpu_main_ms", "cpu_render_ms", "present_wait_ms")}
        result["queue_peaks"] = {key: max(int(r[key]) for r in group)
                                 for key in ("pending_light", "pending_terrain", "pending_fluid")}
        for scope in scopes:
            values = [float(r[scope + "_wall_ms"]) for r in group]
            calls = [int(r[scope + "_calls"]) for r in group]
            if min(values) < 0 or min(calls) < 0:
                raise ValueError(f"Counter reset in {name}/{scope}")
            count, total = sum(calls), sum(values)
            result["scopes"][scope] = {
                "calls": count, "total_ms": total, "calls_per_second": count / seconds,
                "mean_ms_per_call": total / count if count else None,
                "ms_per_second": total / seconds,
                "all_observations_ms": distribution(values),
                "active_observations_ms": distribution([v for v, c in zip(values, calls) if c]),
            }
        factory_calls = result["scopes"]["RR.IndustryTick"]["calls"]
        if result["scopes"]["RR.IndustryMachines"]["calls"] != 2 * factory_calls:
            raise ValueError(f"Incomplete processing scope pairs in {name}")
        for scope in ("RR.Topology", "RR.IndustryPower", "RR.IndustryItems", "RR.IndustryFluids"):
            if result["scopes"][scope]["calls"] != factory_calls:
                raise ValueError(f"Incomplete factory scope calls in {name}/{scope}")
        result["factory_phases_ms_per_tick"] = {
            scope: result["scopes"][scope]["total_ms"] / factory_calls if factory_calls else None
            for scope in ("RR.IndustryTick", "RR.Topology", "RR.IndustryMachines", "RR.IndustryPower", "RR.IndustryItems", "RR.IndustryFluids")}
        largest = sorted(range(len(group)), key=lambda i: frames[i], reverse=True)[:5]
        keys = ["frame", "unity_frame", "frame_ms", "sample_time_s", "timing_timestamp",
                "gpu_ms", "cpu_main_ms", "cpu_render_ms", "present_wait_ms",
                "pending_light", "pending_terrain", "pending_fluid", "machine_views_created", "crate_views_created"]
        result["worst_frame_contexts"] = [
            [{**{k: group[j][k] for k in keys},
              "scopes": {scope: {"ms": float(group[j][scope + "_wall_ms"]),
                                 "calls": int(group[j][scope + "_calls"])} for scope in scopes}}
             for j in range(max(0, i - 4), min(len(group), i + 7))] for i in largest]
        report["workloads"][name] = result
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    result = analyze(args.capture)
    contexts = {name: data.pop("worst_frame_contexts") for name, data in result["workloads"].items()}
    context_path = args.output.with_suffix(".spikes.json.gz")
    with gzip.open(context_path, "wt", encoding="utf-8") as target:
        json.dump(contexts, target, indent=2)
    result["spike_contexts"] = str(context_path)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    for name, data in result["workloads"].items():
        frame = data["frames"]
        print(f"{name}: median {frame['median']:.2f}, p95 {frame['p95']:.2f}, max {frame['max']:.2f} ms; "
              f"{data['over_45_floor']}/{frame['count']} below 45 FPS")
