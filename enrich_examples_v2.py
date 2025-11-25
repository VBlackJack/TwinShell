"""
Script d'enrichissement massif des exemples TwinShell v2
Version améliorée avec enrichissement automatique intelligent
"""

import json
import re
from typing import List, Dict, Any
from copy import deepcopy


class AutoEnricher:
    """Classe pour enrichissement automatique basé sur les patterns de commandes"""

    @staticmethod
    def auto_enrich_powershell_command(cmd_pattern: str, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit automatiquement une commande PowerShell basée sur son type"""
        examples = []
        current_examples = action.get('examples', [])

        # Garder les exemples existants
        if current_examples:
            examples.extend(deepcopy(current_examples))

        # Déterminer le type de commande (verbe)
        verb = None
        for v in ['Get-', 'Set-', 'New-', 'Remove-', 'Start-', 'Stop-', 'Restart-', 'Enable-', 'Disable-', 'Test-', 'Add-', 'Clear-', 'Update-', 'Install-', 'Uninstall-']:
            if v in cmd_pattern:
                verb = v.rstrip('-')
                break

        base_cmd = cmd_pattern.split('|')[0].strip() if '|' in cmd_pattern else cmd_pattern.strip()

        # Exemples selon le verbe
        if verb == 'Get' and len(examples) < 5:
            # Commandes de lecture
            if '-Properties' not in base_cmd:
                examples.append({
                    "command": f"{base_cmd} | Select-Object * -First 1",
                    "description": "Affiche toutes les propriétés du premier objet retourné. Select-Object * élargit l'affichage pour montrer toutes les colonnes. -First 1 limite au premier résultat. Utilisez cette commande pour découvrir les propriétés disponibles avant de filtrer."
                })

            if 'Format-Table' not in cmd_pattern and 'Format-List' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} | Format-Table -AutoSize",
                    "description": "Affiche les résultats dans un tableau optimisé pour la console. Format-Table -AutoSize ajuste automatiquement la largeur des colonnes pour un affichage lisible. Idéal pour visualiser rapidement les données structurées."
                })

            if 'Export-Csv' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} | Export-Csv C:\\Temp\\Export.csv -NoTypeInformation -Encoding UTF8",
                    "description": "Exporte les résultats vers un fichier CSV exploitable dans Excel. -NoTypeInformation supprime la ligne de métadonnées. -Encoding UTF8 garantit la compatibilité des caractères accentués. Utile pour l'archivage, les rapports et l'analyse externe."
                })

            examples.append({
                "command": f"{base_cmd} | Measure-Object",
                "description": "Compte le nombre total d'objets retournés. Measure-Object retourne Count, Sum, Average, Min, Max selon les propriétés. Utile pour les statistiques rapides et la validation de résultats."
            })

        elif verb in ['Set', 'New', 'Remove', 'Disable', 'Enable'] and len(examples) < 5:
            # Commandes de modification
            if '-WhatIf' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} -WhatIf",
                    "description": "Simulation de l'action sans exécution réelle (dry-run). Le switch -WhatIf affiche ce qui serait modifié sans appliquer les changements. INDISPENSABLE avant toute action de masse ou modification critique. Retirez -WhatIf après validation pour exécuter."
                })

            if '-Confirm' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} -Confirm:$false",
                    "description": "Exécute l'action sans demander de confirmation interactive. -Confirm:$false supprime les invites de validation. Utile dans les scripts automatisés mais à utiliser avec précaution : aucun filet de sécurité."
                })

            if verb in ['Set', 'New'] and '-PassThru' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} -PassThru",
                    "description": "Force la commande à retourner l'objet modifié/créé dans le pipeline. Par défaut, les cmdlets Set-/New- ne retournent rien. -PassThru active le retour pour permettre des opérations en chaîne ou des vérifications immédiates."
                })

            examples.append({
                "command": f"{base_cmd} -Verbose",
                "description": "Affiche des informations détaillées sur l'exécution de la commande. -Verbose active les messages de débogage pour comprendre précisément ce qui se passe. Essentiel pour le troubleshooting et la compréhension des opérations internes."
            })

        elif verb in ['Start', 'Stop', 'Restart'] and len(examples) < 5:
            # Commandes de contrôle de services/processus
            examples.append({
                "command": f"{base_cmd} -PassThru",
                "description": "Exécute l'action et retourne l'objet résultant pour vérification immédiate. -PassThru permet de confirmer que l'opération a réussi en affichant le statut final. Utile pour les scripts avec gestion d'erreurs."
            })

            if '-Force' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} -Force",
                    "description": "Force l'action même en cas de résistance (services dépendants, processus protégés, etc.). -Force contourne certaines sécurités. À utiliser avec précaution après avoir vérifié les dépendances."
                })

        elif verb == 'Test' and len(examples) < 5:
            # Commandes de test
            if '-Verbose' not in cmd_pattern:
                examples.append({
                    "command": f"{base_cmd} -Verbose",
                    "description": "Exécute le test avec des informations détaillées. -Verbose affiche chaque étape du test pour un diagnostic approfondi. Indispensable pour comprendre pourquoi un test échoue."
                })

        # Ajouter des exemples génériques si pas assez
        while len(examples) < 3:
            examples.append({
                "command": f"{base_cmd}",
                "description": f"Exécution standard de la commande {verb}. Consultez la documentation officielle avec Get-Help {base_cmd.split()[0]} -Full pour plus de détails sur les paramètres disponibles et les cas d'usage avancés."
            })
            break

        return examples

    @staticmethod
    def auto_enrich_bash_command(cmd_pattern: str, action: Dict[str, str]) -> List[Dict[str, str]]:
        """Enrichit automatiquement une commande Bash/Linux"""
        examples = []
        current_examples = action.get('examples', [])

        if current_examples:
            examples.extend(deepcopy(current_examples))

        base_cmd = cmd_pattern.split('|')[0].strip() if '|' in cmd_pattern else cmd_pattern.strip()
        cmd_name = base_cmd.split()[0] if base_cmd else ''

        # Ajouter des switchs courants selon la commande
        if len(examples) < 5:
            # Verbose
            if '-v' not in cmd_pattern and '--verbose' not in cmd_pattern:
                if cmd_name in ['cp', 'mv', 'rm', 'mkdir', 'chmod', 'chown', 'tar', 'rsync', 'apt', 'yum', 'dnf']:
                    examples.append({
                        "command": f"{base_cmd} -v",
                        "description": f"Exécute la commande en mode verbeux. Le flag -v (verbose) affiche chaque action effectuée en temps réel. Essentiel pour suivre la progression des opérations longues et vérifier que tout se déroule correctement."
                    })

            # Dry-run
            if '--dry-run' not in cmd_pattern and '-n' not in cmd_pattern:
                if cmd_name in ['rsync', 'apt', 'yum', 'dnf']:
                    examples.append({
                        "command": f"{base_cmd} --dry-run",
                        "description": "Simulation sans exécution réelle (dry-run). --dry-run affiche ce qui serait fait sans appliquer les changements. INDISPENSABLE avant les opérations de masse ou les modifications critiques."
                    })

            # Help
            if '--help' not in cmd_pattern and '-h' not in cmd_pattern:
                examples.append({
                    "command": f"{cmd_name} --help",
                    "description": f"Affiche l'aide complète de la commande {cmd_name}. --help liste tous les paramètres disponibles, les options et des exemples d'utilisation. Première commande à utiliser pour découvrir les capacités d'un outil."
                })

            # Man page
            if cmd_name:
                examples.append({
                    "command": f"man {cmd_name}",
                    "description": f"Affiche le manuel complet de {cmd_name}. Les pages man fournissent la documentation détaillée : syntaxe, paramètres, exemples, notes. Plus complet que --help. Naviguez avec flèches, cherchez avec '/', quittez avec 'q'."
                })

        return examples


class ExampleEnricherV2:
    """Classe principale v2 avec enrichissement massif"""

    def __init__(self, data: Dict[str, Any]):
        self.data = data
        self.actions = data.get('actions', [])
        self.auto_enricher = AutoEnricher()
        self.enrichment_stats = {
            'total_actions': len(self.actions),
            'enriched': 0,
            'examples_added': 0,
            'by_category': {}
        }

    def enrich_all(self) -> Dict[str, Any]:
        """Enrichit toutes les actions"""
        print(f"Enrichissement massif de {len(self.actions)} actions...\n")

        for idx, action in enumerate(self.actions, 1):
            if idx % 50 == 0:
                print(f"  Progression: {idx}/{len(self.actions)} actions traitées")

            initial_count = len(action.get('examples', []))
            self.enrich_action(action)
            final_count = len(action.get('examples', []))

            if final_count > initial_count:
                self.enrichment_stats['enriched'] += 1
                self.enrichment_stats['examples_added'] += (final_count - initial_count)

                category = action.get('category', 'Unknown')
                if category not in self.enrichment_stats['by_category']:
                    self.enrichment_stats['by_category'][category] = {'actions': 0, 'examples': 0}
                self.enrichment_stats['by_category'][category]['actions'] += 1
                self.enrichment_stats['by_category'][category]['examples'] += (final_count - initial_count)

        print(f"\n✅ Enrichissement terminé!")
        print(f"   Actions enrichies: {self.enrichment_stats['enriched']}")
        print(f"   Exemples ajoutés: {self.enrichment_stats['examples_added']}")
        print(f"\n📊 Par catégorie:")
        for cat, stats in sorted(self.enrichment_stats['by_category'].items(), key=lambda x: x[1]['examples'], reverse=True)[:10]:
            print(f"   {cat}: {stats['examples']} exemples ajoutés ({stats['actions']} actions)")

        return self.data

    def enrich_action(self, action: Dict[str, Any]):
        """Enrichit une action spécifique avec logique complète"""
        action_id = action.get('id', '')
        category = action.get('category', '')
        platform = action.get('platform', 0)  # 0=Windows, 1=Linux
        cmd_template = action.get('windowsCommandTemplate') if platform == 0 else action.get('linuxCommandTemplate')
        if not cmd_template:
            return

        cmd_pattern = cmd_template.get('commandPattern', '')
        if not cmd_pattern:
            return

        current_examples = action.get('examples', [])
        target_count = 7  # Objectif: au moins 7 exemples par action

        # Si déjà assez d'exemples, ne rien faire
        if len(current_examples) >= target_count:
            return

        # Enrichissement automatique
        if platform == 0:  # Windows/PowerShell
            new_examples = self.auto_enricher.auto_enrich_powershell_command(cmd_pattern, action)
        else:  # Linux/Bash
            new_examples = self.auto_enricher.auto_enrich_bash_command(cmd_pattern, action)

        # Si toujours pas assez, ajouter des exemples génériques supplémentaires
        if len(new_examples) < target_count:
            new_examples = self.add_generic_examples(new_examples, action, target_count)

        action['examples'] = new_examples

    def add_generic_examples(self, examples: List[Dict[str, str]], action: Dict[str, Any], target: int) -> List[Dict[str, str]]:
        """Ajoute des exemples génériques pour atteindre le target"""
        cmd_template = action.get('windowsCommandTemplate') or action.get('linuxCommandTemplate') or {}
        cmd_pattern = cmd_template.get('commandPattern', '')
        parameters = cmd_template.get('parameters', [])

        # Ajouter des exemples basés sur les paramètres disponibles
        for param in parameters[:target - len(examples)]:
            param_name = param.get('name', '')
            param_desc = param.get('description', '')
            param_default = param.get('defaultValue', '')

            if param_name and len(examples) < target:
                # Remplacer le placeholder dans la commande
                example_cmd = cmd_pattern.replace(f"{{{param_name}}}", str(param_default) if param_default else f"<{param_name}>")

                examples.append({
                    "command": example_cmd,
                    "description": f"Exemple utilisant le paramètre '{param_name}': {param_desc}. Remplacez <{param_name}> par la valeur appropriée selon votre environnement."
                })

        # Si toujours pas assez, ajouter des cas d'usage génériques
        while len(examples) < min(target, 5):
            examples.append({
                "command": cmd_pattern,
                "description": f"Cas d'usage standard. {action.get('description', '')} Consultez la documentation pour des paramètres avancés et des options de filtrage."
            })
            break  # Éviter la boucle infinie

        return examples


def main():
    """Fonction principale"""
    import sys

    input_file = 'data/seed/initial-actions.json'
    output_file = 'data/seed/initial-actions-enriched-v2.json'

    print("=" * 70)
    print("Script d'enrichissement MASSIF des exemples TwinShell V2")
    print("=" * 70)
    print()

    # Charger
    print(f"📖 Chargement de {input_file}...")
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        print(f"✅ Fichier chargé : {len(data.get('actions', []))} actions")
    except Exception as e:
        print(f"❌ Erreur : {e}")
        sys.exit(1)

    print()

    # Enrichir
    enricher = ExampleEnricherV2(data)
    enriched_data = enricher.enrich_all()

    print()

    # Sauvegarder
    print(f"💾 Sauvegarde vers {output_file}...")
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(enriched_data, f, ensure_ascii=False, indent=2)
        print(f"✅ Fichier sauvegardé!")
    except Exception as e:
        print(f"❌ Erreur : {e}")
        sys.exit(1)

    print()
    print("=" * 70)
    print("✨ Enrichissement MASSIF terminé!")
    print("=" * 70)


if __name__ == '__main__':
    main()
