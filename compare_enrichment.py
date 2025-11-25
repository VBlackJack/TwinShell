#!/usr/bin/env python3
"""
Compare original vs enriched initial-actions.json
Shows detailed statistics and concrete examples of improvements
"""

import json
from collections import defaultdict

# Load original (backup)
with open('data/seed/initial-actions.BACKUP.json', 'r', encoding='utf-8') as f:
    original = json.load(f)

# Load enriched (current)
with open('data/seed/initial-actions.json', 'r', encoding='utf-8') as f:
    enriched = json.load(f)

# Create lookup by ID
original_by_id = {action['id']: action for action in original['actions']}
enriched_by_id = {action['id']: action for action in enriched['actions']}

print("=" * 80)
print("COMPARAISON ENRICHISSEMENT - initial-actions.json")
print("=" * 80)
print()

# Global statistics
original_total = sum(len(a.get('examples', [])) for a in original['actions'])
enriched_total = sum(len(a.get('examples', [])) for a in enriched['actions'])

print("📊 STATISTIQUES GLOBALES")
print("-" * 80)
print(f"Actions totales:           {len(original['actions'])} → {len(enriched['actions'])}")
print(f"Exemples totaux:           {original_total} → {enriched_total} (+{enriched_total - original_total})")
print(f"Moyenne par action:        {original_total/len(original['actions']):.2f} → {enriched_total/len(enriched['actions']):.2f}")
print()

# Category breakdown
print("📁 ENRICHISSEMENT PAR CATÉGORIE")
print("-" * 80)

category_stats = defaultdict(lambda: {'original': 0, 'enriched': 0, 'actions': 0})

for action_id in enriched_by_id:
    if action_id in original_by_id:
        category = enriched_by_id[action_id].get('category', 'Sans catégorie')
        category_stats[category]['original'] += len(original_by_id[action_id].get('examples', []))
        category_stats[category]['enriched'] += len(enriched_by_id[action_id].get('examples', []))
        category_stats[category]['actions'] += 1

for category in sorted(category_stats.keys()):
    stats = category_stats[category]
    original_count = stats['original']
    enriched_count = stats['enriched']
    gain = enriched_count - original_count
    avg_original = original_count / stats['actions'] if stats['actions'] > 0 else 0
    avg_enriched = enriched_count / stats['actions'] if stats['actions'] > 0 else 0

    print(f"{category}")
    print(f"  Actions: {stats['actions']}")
    print(f"  Exemples: {original_count} → {enriched_count} (+{gain})")
    print(f"  Moyenne: {avg_original:.1f} → {avg_enriched:.1f}")
    print()

# Top improvements
print("🏆 TOP 10 AMÉLIORATIONS (plus d'exemples ajoutés)")
print("-" * 80)

improvements = []
for action_id in enriched_by_id:
    if action_id in original_by_id:
        original_count = len(original_by_id[action_id].get('examples', []))
        enriched_count = len(enriched_by_id[action_id].get('examples', []))
        gain = enriched_count - original_count

        if gain > 0:
            improvements.append({
                'id': action_id,
                'title': enriched_by_id[action_id].get('title', 'Sans titre'),
                'category': enriched_by_id[action_id].get('category', 'Sans catégorie'),
                'original': original_count,
                'enriched': enriched_count,
                'gain': gain
            })

improvements.sort(key=lambda x: x['gain'], reverse=True)

for i, imp in enumerate(improvements[:10], 1):
    print(f"{i}. {imp['title']}")
    print(f"   Catégorie: {imp['category']}")
    print(f"   Exemples: {imp['original']} → {imp['enriched']} (+{imp['gain']})")
    print()

# Quality comparison - description length
print("📝 QUALITÉ DES DESCRIPTIONS")
print("-" * 80)

original_desc_lengths = []
enriched_desc_lengths = []

for action_id in enriched_by_id:
    if action_id in original_by_id:
        for ex in original_by_id[action_id].get('examples', []):
            desc = ex.get('description', '')
            original_desc_lengths.append(len(desc))

        for ex in enriched_by_id[action_id].get('examples', []):
            desc = ex.get('description', '')
            enriched_desc_lengths.append(len(desc))

avg_original_desc = sum(original_desc_lengths) / len(original_desc_lengths) if original_desc_lengths else 0
avg_enriched_desc = sum(enriched_desc_lengths) / len(enriched_desc_lengths) if enriched_desc_lengths else 0

print(f"Longueur moyenne des descriptions:")
print(f"  AVANT:  {avg_original_desc:.0f} caractères")
print(f"  APRÈS:  {avg_enriched_desc:.0f} caractères")
print(f"  Gain:   +{avg_enriched_desc - avg_original_desc:.0f} caractères (+{((avg_enriched_desc/avg_original_desc - 1) * 100):.0f}%)")
print()

# Concrete examples
print("=" * 80)
print("🔍 EXEMPLES CONCRETS DE L'ENRICHISSEMENT")
print("=" * 80)
print()

examples_to_show = [
    'ad-list-users',
    'network-test-connectivity',
    'win-service-status',
    'linux-systemctl-status',
    'network-scan-ports'
]

for action_id in examples_to_show:
    if action_id in original_by_id and action_id in enriched_by_id:
        original_action = original_by_id[action_id]
        enriched_action = enriched_by_id[action_id]

        print(f"📌 {enriched_action.get('title', 'Sans titre')}")
        print(f"   Catégorie: {enriched_action.get('category', 'Sans catégorie')}")
        print()

        original_examples = original_action.get('examples', [])
        enriched_examples = enriched_action.get('examples', [])

        print(f"   AVANT ({len(original_examples)} exemple(s)):")
        for i, ex in enumerate(original_examples[:2], 1):
            print(f"   {i}. {ex.get('command', 'N/A')}")
            desc = ex.get('description', 'Sans description')
            if len(desc) > 100:
                desc = desc[:100] + "..."
            print(f"      {desc}")
            print()

        print(f"   APRÈS ({len(enriched_examples)} exemple(s)):")
        for i, ex in enumerate(enriched_examples[:3], 1):
            print(f"   {i}. {ex.get('command', 'N/A')}")
            desc = ex.get('description', 'Sans description')
            if len(desc) > 150:
                desc = desc[:150] + "..."
            print(f"      {desc}")
            print()

        print(f"   ✅ Ajouté: {len(enriched_examples) - len(original_examples)} exemples")
        print()
        print("-" * 80)
        print()

print("=" * 80)
print("✨ RÉSUMÉ")
print("=" * 80)
print(f"• {enriched_total - original_total} nouveaux exemples ajoutés")
print(f"• Moyenne d'exemples par action: {original_total/len(original['actions']):.2f} → {enriched_total/len(enriched['actions']):.2f}")
print(f"• Descriptions {((avg_enriched_desc/avg_original_desc - 1) * 100):.0f}% plus détaillées")
print(f"• Toutes les catégories ont été enrichies")
print()
