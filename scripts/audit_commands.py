#!/usr/bin/env python3
"""
Audit script for TwinShell commands database.
Analyzes initial-actions.json for quality issues.
"""

import json
import re
from collections import defaultdict
from pathlib import Path

def load_actions(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    return data.get('actions', [])

def analyze_actions(actions):
    issues = defaultdict(list)
    stats = {
        'total': len(actions),
        'by_platform': defaultdict(int),
        'by_level': defaultdict(int),
        'by_category': defaultdict(int),
        'with_examples': 0,
        'without_examples': 0,
        'with_template': 0,
        'without_template': 0,
        'with_description': 0,
        'empty_description': 0,
    }

    platform_map = {0: 'Windows', 1: 'Linux', 2: 'Both'}
    level_map = {0: 'Info', 1: 'Run', 2: 'Dangerous'}

    deprecated_commands = {
        'wmic': 'Deprecated - use Get-CimInstance instead',
        'net user': 'Consider using Get-LocalUser for better output',
        'netsh interface ip show config': 'Consider using Get-NetIPConfiguration',
    }

    dangerous_patterns = [
        (r'rm\s+-rf\s+/', 'Dangerous recursive delete from root'),
        (r'Remove-Item.*-Recurse.*-Force.*\$env:', 'Dangerous recursive delete of system paths'),
        (r'Format-Volume', 'Disk formatting command'),
        (r'Clear-Disk', 'Disk clearing command'),
        (r'Stop-Computer\s+-Force', 'Force shutdown without confirmation'),
        (r'Restart-Computer\s+-Force', 'Force restart without confirmation'),
    ]

    for action in actions:
        action_id = action.get('id', 'unknown')
        title = action.get('title', 'No title')
        category = action.get('category', 'Unknown')
        platform = action.get('platform', 0)
        level = action.get('level', 0)
        description = action.get('description', '')
        examples = action.get('examples', [])
        windows_template = action.get('windowsCommandTemplate')
        linux_template = action.get('linuxCommandTemplate')

        # Stats
        stats['by_platform'][platform_map.get(platform, 'Unknown')] += 1
        stats['by_level'][level_map.get(level, 'Unknown')] += 1
        stats['by_category'][category] += 1

        if examples:
            stats['with_examples'] += 1
        else:
            stats['without_examples'] += 1

        if windows_template or linux_template:
            stats['with_template'] += 1
        else:
            stats['without_template'] += 1

        if description and len(description.strip()) > 10:
            stats['with_description'] += 1
        else:
            stats['empty_description'] += 1
            issues['empty_description'].append(f"{title} ({action_id[:8]})")

        # Check for deprecated commands
        all_commands = []
        if windows_template:
            all_commands.append(windows_template.get('commandPattern', ''))
        if linux_template:
            all_commands.append(linux_template.get('commandPattern', ''))
        for ex in examples:
            all_commands.append(ex.get('command', ''))

        for cmd in all_commands:
            cmd_lower = cmd.lower()
            for deprecated, reason in deprecated_commands.items():
                if deprecated.lower() in cmd_lower:
                    issues['deprecated'].append(f"{title}: {reason}")
                    break

        # Check for dangerous commands not marked as dangerous
        if level != 2:  # Not marked as Dangerous
            for cmd in all_commands:
                for pattern, desc in dangerous_patterns:
                    if re.search(pattern, cmd, re.IGNORECASE):
                        issues['unmarked_dangerous'].append(f"{title}: {desc}")
                        break

        # Check for empty examples
        if not examples:
            issues['no_examples'].append(f"{title} ({action_id[:8]})")

        # Check for placeholder examples that weren't replaced
        for ex in examples:
            cmd = ex.get('command', '')
            if '<' in cmd and '>' in cmd and not cmd.startswith('#'):
                # Check if it's a genuine placeholder vs comparison operators
                if re.search(r'<[a-zA-Z]+>', cmd):
                    issues['placeholder_examples'].append(f"{title}: {cmd[:50]}...")
                    break

    return stats, dict(issues)

def print_report(stats, issues):
    print("=" * 70)
    print("AUDIT DES COMMANDES TWINSHELL")
    print("=" * 70)
    print()

    print("## STATISTIQUES GÉNÉRALES")
    print(f"Total des commandes: {stats['total']}")
    print()

    print("### Par plateforme:")
    for platform, count in sorted(stats['by_platform'].items(), key=lambda x: -x[1]):
        print(f"  - {platform}: {count}")
    print()

    print("### Par niveau de criticité:")
    for level, count in sorted(stats['by_level'].items(), key=lambda x: -x[1]):
        print(f"  - {level}: {count}")
    print()

    print("### Par catégorie:")
    for category, count in sorted(stats['by_category'].items(), key=lambda x: -x[1]):
        print(f"  - {category}: {count}")
    print()

    print("### Qualité des données:")
    print(f"  - Avec exemples: {stats['with_examples']}")
    print(f"  - Sans exemples: {stats['without_examples']}")
    print(f"  - Avec template: {stats['with_template']}")
    print(f"  - Sans template: {stats['without_template']}")
    print(f"  - Description complète: {stats['with_description']}")
    print(f"  - Description vide/courte: {stats['empty_description']}")
    print()

    print("=" * 70)
    print("## PROBLÈMES IDENTIFIÉS")
    print("=" * 70)
    print()

    if issues.get('deprecated'):
        print(f"### Commandes dépréciées ({len(issues['deprecated'])})")
        for item in issues['deprecated'][:20]:
            print(f"  - {item}")
        if len(issues['deprecated']) > 20:
            print(f"  ... et {len(issues['deprecated']) - 20} autres")
        print()

    if issues.get('unmarked_dangerous'):
        print(f"### Commandes dangereuses non marquées ({len(issues['unmarked_dangerous'])})")
        for item in issues['unmarked_dangerous'][:10]:
            print(f"  - {item}")
        if len(issues['unmarked_dangerous']) > 10:
            print(f"  ... et {len(issues['unmarked_dangerous']) - 10} autres")
        print()

    if issues.get('no_examples'):
        print(f"### Commandes sans exemples ({len(issues['no_examples'])})")
        for item in issues['no_examples'][:15]:
            print(f"  - {item}")
        if len(issues['no_examples']) > 15:
            print(f"  ... et {len(issues['no_examples']) - 15} autres")
        print()

    if issues.get('placeholder_examples'):
        print(f"### Exemples avec placeholders non remplacés ({len(issues['placeholder_examples'])})")
        for item in issues['placeholder_examples'][:10]:
            print(f"  - {item}")
        if len(issues['placeholder_examples']) > 10:
            print(f"  ... et {len(issues['placeholder_examples']) - 10} autres")
        print()

    if issues.get('empty_description'):
        print(f"### Descriptions vides ou trop courtes ({len(issues['empty_description'])})")
        for item in issues['empty_description'][:10]:
            print(f"  - {item}")
        if len(issues['empty_description']) > 10:
            print(f"  ... et {len(issues['empty_description']) - 10} autres")
        print()

    # Summary
    total_issues = sum(len(v) for v in issues.values())
    print("=" * 70)
    print("## RÉSUMÉ")
    print("=" * 70)
    print(f"Total des problèmes identifiés: {total_issues}")
    print()

    # Score calculation
    quality_score = 100
    quality_score -= len(issues.get('deprecated', [])) * 0.5
    quality_score -= len(issues.get('unmarked_dangerous', [])) * 2
    quality_score -= len(issues.get('no_examples', [])) * 0.2
    quality_score -= len(issues.get('placeholder_examples', [])) * 1
    quality_score -= len(issues.get('empty_description', [])) * 0.1
    quality_score = max(0, quality_score)

    print(f"Score de qualité: {quality_score:.1f}/100")

    if quality_score >= 90:
        print("Verdict: EXCELLENT")
    elif quality_score >= 75:
        print("Verdict: BON")
    elif quality_score >= 50:
        print("Verdict: ACCEPTABLE")
    else:
        print("Verdict: À AMÉLIORER")

if __name__ == '__main__':
    file_path = Path(__file__).parent.parent / 'data' / 'seed' / 'initial-actions.json'
    actions = load_actions(file_path)
    stats, issues = analyze_actions(actions)
    print_report(stats, issues)
