#!/usr/bin/env python3
"""
Script to add missing Linux/Windows examples for cross-platform actions.
Analyzes actions where examples only exist for one platform and generates the missing ones.
"""

import json

def analyze_and_fix_missing_examples(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    actions_needing_examples = []
    examples_added = 0

    for action in data.get('actions', []):
        # Only process cross-platform actions (platform: 2)
        if action.get('platform') != 2:
            continue

        # Collect all examples from all sources
        all_examples = []
        if action.get('examples'):
            all_examples.extend(action['examples'])
        if action.get('windowsExamples'):
            all_examples.extend(action['windowsExamples'])
        if action.get('linuxExamples'):
            all_examples.extend(action['linuxExamples'])

        if not all_examples:
            continue

        # Count examples per platform
        windows_examples = [e for e in all_examples if e.get('platform') == 0]
        linux_examples = [e for e in all_examples if e.get('platform') == 1]
        both_examples = [e for e in all_examples if e.get('platform') == 2]

        # Check if we have Windows template and Linux template
        has_windows_template = action.get('windowsCommandTemplate') is not None
        has_linux_template = action.get('linuxCommandTemplate') is not None

        # If action has both templates but examples only for one platform
        if has_windows_template and has_linux_template:
            windows_template = action.get('windowsCommandTemplate', {})
            linux_template = action.get('linuxCommandTemplate', {})

            # If no Linux examples but we have Windows examples, generate Linux examples
            if len(linux_examples) == 0 and len(windows_examples) > 0 and linux_template:
                linux_cmd_name = linux_template.get('name', '')
                linux_pattern = linux_template.get('commandPattern', '')

                # Add a basic example based on the Linux template
                if linux_cmd_name and linux_pattern:
                    # Create a simple example using the template pattern
                    new_example = {
                        "command": linux_pattern.replace('{', '<').replace('}', '>'),
                        "description": f"Exemple Linux utilisant {linux_cmd_name}. Remplacez les paramètres entre <> par vos valeurs.",
                        "platform": 1
                    }

                    # Add to examples list
                    if 'examples' not in action:
                        action['examples'] = []
                    action['examples'].append(new_example)
                    examples_added += 1
                    actions_needing_examples.append({
                        'id': action['id'],
                        'title': action['title'],
                        'missing': 'linux',
                        'linux_cmd': linux_cmd_name
                    })

            # If no Windows examples but we have Linux examples, generate Windows examples
            if len(windows_examples) == 0 and len(linux_examples) > 0 and windows_template:
                windows_cmd_name = windows_template.get('name', '')
                windows_pattern = windows_template.get('commandPattern', '')

                if windows_cmd_name and windows_pattern:
                    new_example = {
                        "command": windows_pattern.replace('{', '<').replace('}', '>'),
                        "description": f"Exemple Windows utilisant {windows_cmd_name}. Remplacez les paramètres entre <> par vos valeurs.",
                        "platform": 0
                    }

                    if 'examples' not in action:
                        action['examples'] = []
                    action['examples'].append(new_example)
                    examples_added += 1
                    actions_needing_examples.append({
                        'id': action['id'],
                        'title': action['title'],
                        'missing': 'windows',
                        'windows_cmd': windows_cmd_name
                    })

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Added {examples_added} examples to {len(actions_needing_examples)} actions")

    if actions_needing_examples:
        print("\nActions that received new examples:")
        for action_info in actions_needing_examples:
            print(f"  - {action_info['title']} (missing {action_info['missing']})")

    return examples_added, actions_needing_examples

if __name__ == '__main__':
    input_file = r'G:\_dev\TwinShell\TwinShell\data\seed\initial-actions.json'
    output_file = input_file  # Overwrite

    analyze_and_fix_missing_examples(input_file, output_file)
    print("\nDone!")
