import json
from collections import Counter

# Charger le fichier JSON
with open('data/seed/initial-actions.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Analyser les actions
actions = data.get('actions', [])
print(f"Nombre total d'actions: {len(actions)}")
print()

# Compter les catégories
categories = Counter(action.get('category', 'N/A') for action in actions)
print("Catégories (nombre d'actions):")
for cat, count in sorted(categories.items(), key=lambda x: x[1], reverse=True):
    print(f"  {cat}: {count}")
print()

# Analyser les exemples
examples_stats = []
for action in actions:
    num_examples = len(action.get('examples', []))
    examples_stats.append(num_examples)

print(f"Statistiques des exemples:")
print(f"  Minimum: {min(examples_stats)}")
print(f"  Maximum: {max(examples_stats)}")
print(f"  Moyenne: {sum(examples_stats) / len(examples_stats):.2f}")
print()

# Compter les actions par nombre d'exemples
examples_distribution = Counter(examples_stats)
print("Distribution du nombre d'exemples:")
for num, count in sorted(examples_distribution.items()):
    print(f"  {num} exemple(s): {count} actions")
