"""
Script d'analyse du fichier enrichi final
Affiche des statistiques détaillées et des exemples
"""

import json
from collections import Counter

def analyze_final():
    # Charger le fichier enrichi
    with open('data/seed/initial-actions.json', 'r', encoding='utf-8') as f:
        data = json.load(f)

    actions = data.get('actions', [])

    print("=" * 80)
    print("ANALYSE DU FICHIER INITIAL-ACTIONS.JSON ENRICHI")
    print("=" * 80)
    print()

    # Statistiques globales
    total_examples = sum(len(a.get('examples', [])) for a in actions)
    avg_examples = total_examples / len(actions) if actions else 0

    print("📊 STATISTIQUES GLOBALES")
    print("-" * 80)
    print(f"  Total d'actions : {len(actions)}")
    print(f"  Total d'exemples : {total_examples}")
    print(f"  Moyenne d'exemples par action : {avg_examples:.2f}")
    print()

    # Distribution
    distribution = Counter(len(a.get('examples', [])) for a in actions)
    print("📈 DISTRIBUTION DES EXEMPLES")
    print("-" * 80)
    for num in sorted(distribution.keys()):
        count = distribution[num]
        bar = '█' * int(count / 5) if count >= 5 else '▌' * count
        print(f"  {num} exemple(s) : {count:3d} actions {bar}")
    print()

    # Par catégorie
    by_category = {}
    for action in actions:
        cat = action.get('category', 'Unknown')
        if cat not in by_category:
            by_category[cat] = {'actions': 0, 'examples': 0}
        by_category[cat]['actions'] += 1
        by_category[cat]['examples'] += len(action.get('examples', []))

    print("📂 TOP 10 CATÉGORIES PAR NOMBRE D'EXEMPLES")
    print("-" * 80)
    sorted_cats = sorted(by_category.items(), key=lambda x: x[1]['examples'], reverse=True)
    for cat, stats in sorted_cats[:10]:
        avg = stats['examples'] / stats['actions'] if stats['actions'] > 0 else 0
        print(f"  {cat}")
        print(f"    → {stats['actions']} actions, {stats['examples']} exemples (moy: {avg:.1f})")
    print()

    # Longueur des descriptions
    all_desc_lengths = []
    for action in actions:
        for example in action.get('examples', []):
            desc = example.get('description', '')
            all_desc_lengths.append(len(desc))

    if all_desc_lengths:
        avg_desc_len = sum(all_desc_lengths) / len(all_desc_lengths)
        min_desc_len = min(all_desc_lengths)
        max_desc_len = max(all_desc_lengths)

        print("📝 QUALITÉ DES DESCRIPTIONS")
        print("-" * 80)
        print(f"  Longueur moyenne : {avg_desc_len:.0f} caractères")
        print(f"  Longueur minimale : {min_desc_len} caractères")
        print(f"  Longueur maximale : {max_desc_len} caractères")

        # Catégoriser
        short = sum(1 for l in all_desc_lengths if l < 100)
        medium = sum(1 for l in all_desc_lengths if 100 <= l < 300)
        long_desc = sum(1 for l in all_desc_lengths if l >= 300)

        print(f"  Descriptions courtes (<100 car) : {short} ({short/len(all_desc_lengths)*100:.1f}%)")
        print(f"  Descriptions moyennes (100-300) : {medium} ({medium/len(all_desc_lengths)*100:.1f}%)")
        print(f"  Descriptions longues (>300) : {long_desc} ({long_desc/len(all_desc_lengths)*100:.1f}%)")
    print()

    # Exemples concrets
    print("=" * 80)
    print("EXEMPLES D'ACTIONS ENRICHIES")
    print("=" * 80)
    print()

    # Chercher quelques exemples intéressants
    examples_to_show = [
        ('ad-list-users', '🏢 Active Directory'),
        ('test-connection', '🌐 Network'),
        ('get-eventlog', '📊 Monitoring'),
    ]

    for action_id, category in examples_to_show:
        action = next((a for a in actions if a['id'] == action_id), None)
        if action:
            print(f"{'=' * 80}")
            print(f"📌 {action['title']}")
            print(f"   Catégorie: {category}")
            print(f"   ID: {action_id}")
            print(f"   Nombre d'exemples: {len(action.get('examples', []))}")
            print("-" * 80)

            for i, example in enumerate(action.get('examples', [])[:3], 1):
                cmd = example.get('command', '')
                desc = example.get('description', '')

                print(f"\n  Exemple {i}:")
                print(f"  Command: {cmd[:70]}{'...' if len(cmd) > 70 else ''}")
                print(f"  Description: {desc[:150]}{'...' if len(desc) > 150 else ''}")

            if len(action.get('examples', [])) > 3:
                print(f"\n  ... et {len(action.get('examples', [])) - 3} autres exemples")
            print()

    print("=" * 80)
    print("✅ ANALYSE TERMINÉE")
    print("=" * 80)
    print()
    print("📄 Consultez RAPPORT_ENRICHISSEMENT.md pour le rapport détaillé complet")


if __name__ == '__main__':
    analyze_final()
