#!/usr/bin/env python3
"""
Script to update examples in initial-actions.json with Platform field.
- Examples in windowsExamples get platform: 0 (Windows)
- Examples in linuxExamples get platform: 1 (Linux)
- Examples in examples stay with platform: 2 (Both) - default
"""

import json
import sys

def update_examples_platform(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    actions_updated = 0
    examples_updated = 0

    for action in data.get('actions', []):
        action_modified = False

        # Update windowsExamples with platform: 0 (Windows)
        if 'windowsExamples' in action and action['windowsExamples']:
            for example in action['windowsExamples']:
                if 'platform' not in example:
                    example['platform'] = 0  # Windows
                    examples_updated += 1
                    action_modified = True

        # Update linuxExamples with platform: 1 (Linux)
        if 'linuxExamples' in action and action['linuxExamples']:
            for example in action['linuxExamples']:
                if 'platform' not in example:
                    example['platform'] = 1  # Linux
                    examples_updated += 1
                    action_modified = True

        # Update regular examples with platform: 2 (Both) if not already set
        if 'examples' in action and action['examples']:
            for example in action['examples']:
                if 'platform' not in example:
                    example['platform'] = 2  # Both
                    examples_updated += 1
                    action_modified = True

        if action_modified:
            actions_updated += 1

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Updated {actions_updated} actions with {examples_updated} examples")
    return actions_updated, examples_updated

if __name__ == '__main__':
    input_file = r'G:\_dev\TwinShell\TwinShell\data\seed\initial-actions.json'
    output_file = input_file  # Overwrite

    update_examples_platform(input_file, output_file)
    print("Done!")
