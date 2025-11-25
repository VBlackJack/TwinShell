"""
Script de migration vers des fiches unifiées cross-platform
Regroupe les commandes Windows/Linux équivalentes dans des fiches uniques
"""

import json
import re
from typing import List, Dict, Any, Optional, Tuple
from copy import deepcopy


class CrossPlatformMigrator:
    """Migrateur vers des fiches cross-platform unifiées"""

    def __init__(self, data: Dict[str, Any]):
        self.data = data
        self.actions = data.get('actions', [])
        self.windows_actions = [a for a in self.actions if a.get('platform') == 0]
        self.linux_actions = [a for a in self.actions if a.get('platform') == 1]

        # Définir les paires d'équivalents connus
        self.known_pairs = self.define_known_pairs()

        # Statistiques
        self.stats = {
            'total_actions': len(self.actions),
            'windows_actions': len(self.windows_actions),
            'linux_actions': len(self.linux_actions),
            'unified_created': 0,
            'windows_only': 0,
            'linux_only': 0,
            'pairs_found': 0
        }

    def define_known_pairs(self) -> List[Dict[str, Any]]:
        """Définit les paires de commandes équivalentes connues"""
        return [
            # Services
            {
                'concept': 'Vérifier le statut d\'un service',
                'category': '🔧 Services',
                'windows_patterns': ['Get-Service', 'service.*status', 'statut.*service.*windows'],
                'linux_patterns': ['systemctl status', 'service.*status.*linux', 'statut.*service.*linux']
            },
            {
                'concept': 'Lister les services',
                'category': '🔧 Services',
                'windows_patterns': ['Get-Service', 'list.*service', 'lister.*service'],
                'linux_patterns': ['systemctl list-units', 'list.*service.*linux']
            },
            {
                'concept': 'Démarrer un service',
                'category': '🔧 Services',
                'windows_patterns': ['Start-Service', 'start.*service'],
                'linux_patterns': ['systemctl start', 'start.*service.*linux']
            },
            {
                'concept': 'Arrêter un service',
                'category': '🔧 Services',
                'windows_patterns': ['Stop-Service', 'stop.*service'],
                'linux_patterns': ['systemctl stop', 'stop.*service.*linux']
            },
            {
                'concept': 'Redémarrer un service',
                'category': '🔧 Services',
                'windows_patterns': ['Restart-Service', 'restart.*service'],
                'linux_patterns': ['systemctl restart', 'restart.*service.*linux']
            },

            # Processus
            {
                'concept': 'Lister les processus',
                'category': '⚙️ Processus',
                'windows_patterns': ['Get-Process', 'list.*process', 'lister.*processus'],
                'linux_patterns': ['ps aux', 'ps -ef', 'list.*process.*linux']
            },
            {
                'concept': 'Terminer un processus',
                'category': '⚙️ Processus',
                'windows_patterns': ['Stop-Process', 'kill.*process', 'terminer.*processus'],
                'linux_patterns': ['kill', 'pkill', 'killall', 'terminer.*processus.*linux']
            },

            # Réseau
            {
                'concept': 'Tester la connectivité réseau (ping)',
                'category': '🌐 Réseau',
                'windows_patterns': ['Test-Connection', 'ping.*test', 'connectivité'],
                'linux_patterns': ['ping', 'connectivité.*linux']
            },
            {
                'concept': 'Afficher la configuration IP',
                'category': '🌐 Réseau',
                'windows_patterns': ['Get-NetIPAddress', 'ipconfig', 'ip.*config'],
                'linux_patterns': ['ip addr', 'ifconfig', 'ip.*address']
            },
            {
                'concept': 'Résolution DNS',
                'category': '🌐 Réseau',
                'windows_patterns': ['Resolve-DnsName', 'nslookup', 'dns.*resolution'],
                'linux_patterns': ['dig', 'nslookup', 'dns.*lookup']
            },
            {
                'concept': 'Tester un port TCP',
                'category': '🌐 Réseau',
                'windows_patterns': ['Test-NetConnection.*-Port', 'test.*port'],
                'linux_patterns': ['telnet', 'nc ', 'netcat', 'test.*port']
            },
            {
                'concept': 'Afficher les ports ouverts',
                'category': '🌐 Réseau',
                'windows_patterns': ['Get-NetTCPConnection', 'netstat', 'ports.*ouverts'],
                'linux_patterns': ['netstat', 'ss ', 'lsof', 'ports.*open']
            },

            # Disque
            {
                'concept': 'Vérifier l\'espace disque',
                'category': '💾 Stockage',
                'windows_patterns': ['Get-Volume', 'Get-PSDrive', 'disk.*space', 'espace.*disque'],
                'linux_patterns': ['df -h', 'df', 'disk.*usage', 'espace.*disque.*linux']
            },
            {
                'concept': 'Utilisation disque d\'un dossier',
                'category': '💾 Stockage',
                'windows_patterns': ['Get-ChildItem.*Measure', 'folder.*size', 'taille.*dossier'],
                'linux_patterns': ['du -sh', 'du -h', 'disk.*usage.*directory']
            },

            # Logs
            {
                'concept': 'Consulter les logs système',
                'category': '📊 Logs',
                'windows_patterns': ['Get-EventLog.*System', 'event.*log', 'logs.*système'],
                'linux_patterns': ['journalctl', 'tail.*syslog', 'logs.*system']
            },
            {
                'concept': 'Logs en temps réel',
                'category': '📊 Logs',
                'windows_patterns': ['Get-EventLog.*-Newest', 'tail.*log'],
                'linux_patterns': ['tail -f', 'journalctl -f', 'follow.*log']
            },

            # Fichiers
            {
                'concept': 'Lister les fichiers',
                'category': '📁 Fichiers',
                'windows_patterns': ['Get-ChildItem', 'dir', 'ls', 'list.*file'],
                'linux_patterns': ['ls -la', 'ls -l', 'list.*file']
            },
            {
                'concept': 'Rechercher des fichiers',
                'category': '📁 Fichiers',
                'windows_patterns': ['Get-ChildItem.*-Recurse.*-Filter', 'search.*file'],
                'linux_patterns': ['find', 'locate', 'search.*file']
            },
            {
                'concept': 'Copier des fichiers',
                'category': '📁 Fichiers',
                'windows_patterns': ['Copy-Item', 'copy', 'copier'],
                'linux_patterns': ['cp ', 'copy', 'copier.*linux']
            },

            # Packages/Updates
            {
                'concept': 'Mettre à jour le système',
                'category': '🔄 Updates',
                'windows_patterns': ['Windows.*Update', 'Install.*WindowsUpdate', 'update.*windows'],
                'linux_patterns': ['apt update', 'apt upgrade', 'yum update', 'dnf update']
            },
            {
                'concept': 'Installer un package',
                'category': '📦 Packages',
                'windows_patterns': ['Install-Package', 'Install-Module', 'choco install'],
                'linux_patterns': ['apt install', 'yum install', 'dnf install']
            },

            # Utilisateurs
            {
                'concept': 'Lister les utilisateurs locaux',
                'category': '👤 Utilisateurs',
                'windows_patterns': ['Get-LocalUser', 'net user', 'list.*user.*local'],
                'linux_patterns': ['cat /etc/passwd', 'getent passwd', 'list.*user']
            },
            {
                'concept': 'Créer un utilisateur',
                'category': '👤 Utilisateurs',
                'windows_patterns': ['New-LocalUser', 'net user.*add', 'create.*user'],
                'linux_patterns': ['useradd', 'adduser', 'create.*user.*linux']
            },
        ]

    def find_match(self, action: Dict[str, Any], patterns: List[str]) -> bool:
        """Vérifie si une action correspond aux patterns"""
        title = action.get('title', '').lower()
        description = action.get('description', '').lower()

        win_cmd = action.get('windowsCommandTemplate', {}).get('commandPattern', '').lower()
        linux_cmd = action.get('linuxCommandTemplate', {}).get('commandPattern', '').lower()

        search_text = f"{title} {description} {win_cmd} {linux_cmd}"

        for pattern in patterns:
            if re.search(pattern.lower(), search_text):
                return True
        return False

    def find_pair(self, pair_def: Dict[str, Any]) -> Tuple[Optional[Dict], Optional[Dict]]:
        """Trouve une paire d'actions correspondant à la définition"""
        windows_action = None
        linux_action = None

        # Chercher l'action Windows
        for action in self.windows_actions:
            if self.find_match(action, pair_def['windows_patterns']):
                windows_action = action
                break

        # Chercher l'action Linux
        for action in self.linux_actions:
            if self.find_match(action, pair_def['linux_patterns']):
                linux_action = action
                break

        return windows_action, linux_action

    def create_unified_action(self,
                             concept: str,
                             category: str,
                             windows_action: Optional[Dict],
                             linux_action: Optional[Dict]) -> Dict[str, Any]:
        """Crée une action unifiée à partir d'une paire"""

        # Utiliser l'action Windows comme base si elle existe, sinon Linux
        base_action = windows_action if windows_action else linux_action

        # Créer un ID unifié
        if windows_action and linux_action:
            # Fusionner les IDs
            win_id = windows_action['id'].replace('win-', '').replace('-windows', '')
            linux_id = linux_action['id'].replace('linux-', '').replace('-linux', '')
            unified_id = f"unified-{win_id}" if len(win_id) <= len(linux_id) else f"unified-{linux_id}"
        elif windows_action:
            unified_id = windows_action['id'].replace('win-', 'unified-')
        else:
            unified_id = linux_action['id'].replace('linux-', 'unified-')

        # Construire l'action unifiée
        unified = {
            "id": unified_id,
            "title": concept,
            "description": base_action.get('description', ''),
            "category": category,
            "platform": 2,  # 2 = Cross-platform
            "supportedPlatforms": [],
            "level": base_action.get('level', 0),
            "tags": list(set(base_action.get('tags', []))),
            "notes": "",
            "links": [],
        }

        # Ajouter les commandes Windows si disponibles
        if windows_action:
            unified['supportedPlatforms'].append(0)
            unified['windowsCommandTemplateId'] = windows_action.get('windowsCommandTemplateId')
            unified['windowsCommandTemplate'] = windows_action.get('windowsCommandTemplate')
            unified['windowsExamples'] = windows_action.get('examples', [])

            # Fusionner les tags
            unified['tags'].extend(windows_action.get('tags', []))
            unified['tags'] = list(set(unified['tags']))

            # Fusionner les notes
            win_notes = windows_action.get('notes', '')
            if win_notes:
                unified['notes'] += f"Windows: {win_notes}\n"

            # Fusionner les liens
            unified['links'].extend(windows_action.get('links', []))

        # Ajouter les commandes Linux si disponibles
        if linux_action:
            unified['supportedPlatforms'].append(1)
            unified['linuxCommandTemplateId'] = linux_action.get('linuxCommandTemplateId')
            unified['linuxCommandTemplate'] = linux_action.get('linuxCommandTemplate')
            unified['linuxExamples'] = linux_action.get('examples', [])

            # Fusionner les tags
            unified['tags'].extend(linux_action.get('tags', []))
            unified['tags'] = list(set(unified['tags']))

            # Fusionner les notes
            linux_notes = linux_action.get('notes', '')
            if linux_notes:
                unified['notes'] += f"Linux: {linux_notes}\n"

            # Fusionner les liens
            unified['links'].extend(linux_action.get('links', []))

        # Nettoyer les notes
        unified['notes'] = unified['notes'].strip()

        # Dédupliquer les liens
        seen_urls = set()
        unique_links = []
        for link in unified['links']:
            url = link.get('url', '')
            if url and url not in seen_urls:
                seen_urls.add(url)
                unique_links.append(link)
        unified['links'] = unique_links

        # Ajouter des notes sur les différences cross-platform
        if windows_action and linux_action:
            unified['crossPlatformNotes'] = {
                "differences": self.extract_differences(windows_action, linux_action),
                "commonalities": [
                    f"Les deux permettent de {concept.lower()}",
                    "Fonctionnalité équivalente sur les deux plateformes"
                ]
            }

        return unified

    def extract_differences(self, windows_action: Dict, linux_action: Dict) -> List[str]:
        """Extrait les différences entre les commandes Windows et Linux"""
        differences = []

        # Comparer les paramètres
        win_params = windows_action.get('windowsCommandTemplate', {}).get('parameters', [])
        linux_params = linux_action.get('linuxCommandTemplate', {}).get('parameters', [])

        if len(win_params) != len(linux_params):
            differences.append(f"Windows utilise {len(win_params)} paramètre(s), Linux en utilise {len(linux_params)}")

        # Syntaxe
        win_cmd = windows_action.get('windowsCommandTemplate', {}).get('name', '')
        linux_cmd = linux_action.get('linuxCommandTemplate', {}).get('commandPattern', '').split()[0]

        differences.append(f"Syntaxe: Windows utilise '{win_cmd}', Linux utilise '{linux_cmd}'")

        return differences

    def migrate(self) -> Dict[str, Any]:
        """Effectue la migration complète"""
        print("=" * 80)
        print("MIGRATION VERS FICHES UNIFIÉES CROSS-PLATFORM")
        print("=" * 80)
        print()

        print(f"📊 État initial:")
        print(f"   Actions totales    : {self.stats['total_actions']}")
        print(f"   Actions Windows    : {self.stats['windows_actions']}")
        print(f"   Actions Linux      : {self.stats['linux_actions']}")
        print()

        # Tracker les actions déjà utilisées
        used_windows = set()
        used_linux = set()

        # Actions unifiées créées
        unified_actions = []

        # Traiter chaque paire connue
        print("🔄 Recherche et fusion des paires connues...")
        print()

        for pair_def in self.known_pairs:
            win_action, linux_action = self.find_pair(pair_def)

            if win_action or linux_action:
                self.stats['pairs_found'] += 1

                # Créer l'action unifiée
                unified = self.create_unified_action(
                    pair_def['concept'],
                    pair_def['category'],
                    win_action,
                    linux_action
                )

                unified_actions.append(unified)
                self.stats['unified_created'] += 1

                # Marquer comme utilisées
                if win_action:
                    used_windows.add(win_action['id'])
                if linux_action:
                    used_linux.add(linux_action['id'])

                # Afficher
                status = "✅" if (win_action and linux_action) else "⚠️"
                platforms = []
                if win_action:
                    platforms.append(f"Windows ({len(win_action.get('examples', []))} ex)")
                if linux_action:
                    platforms.append(f"Linux ({len(linux_action.get('examples', []))} ex)")

                print(f"{status} {pair_def['concept']}")
                print(f"   {' + '.join(platforms)}")

        print()
        print(f"✅ {self.stats['pairs_found']} paires trouvées et fusionnées")
        print()

        # Ajouter les actions non appariées (Windows only)
        print("📦 Conservation des actions Windows non appariées...")
        for action in self.windows_actions:
            if action['id'] not in used_windows:
                unified_actions.append(deepcopy(action))
                self.stats['windows_only'] += 1
        print(f"   {self.stats['windows_only']} actions Windows conservées")

        # Ajouter les actions non appariées (Linux only)
        print("📦 Conservation des actions Linux non appariées...")
        for action in self.linux_actions:
            if action['id'] not in used_linux:
                unified_actions.append(deepcopy(action))
                self.stats['linux_only'] += 1
        print(f"   {self.stats['linux_only']} actions Linux conservées")

        print()
        print("=" * 80)
        print("📊 RÉSULTATS DE LA MIGRATION")
        print("=" * 80)
        print(f"   Actions initiales        : {self.stats['total_actions']}")
        print(f"   Actions unifiées créées  : {self.stats['unified_created']}")
        print(f"   Actions Windows only     : {self.stats['windows_only']}")
        print(f"   Actions Linux only       : {self.stats['linux_only']}")
        print(f"   Total final              : {len(unified_actions)}")
        print(f"   Réduction                : {self.stats['total_actions'] - len(unified_actions)} actions (-{(self.stats['total_actions'] - len(unified_actions)) / self.stats['total_actions'] * 100:.1f}%)")
        print()

        # Créer le nouveau fichier de données
        new_data = deepcopy(self.data)
        new_data['actions'] = unified_actions
        new_data['schemaVersion'] = "2.0"  # Nouvelle version du schéma

        return new_data

    def generate_report(self) -> str:
        """Génère un rapport de migration"""
        report = f"""
# 📊 Rapport de Migration Cross-Platform

## Résumé de la Migration

- **Actions initiales** : {self.stats['total_actions']}
- **Paires trouvées** : {self.stats['pairs_found']}
- **Actions unifiées créées** : {self.stats['unified_created']}
- **Actions Windows uniquement** : {self.stats['windows_only']}
- **Actions Linux uniquement** : {self.stats['linux_only']}
- **Total final** : {self.stats['unified_created'] + self.stats['windows_only'] + self.stats['linux_only']}
- **Réduction** : {self.stats['pairs_found']} actions (-{(self.stats['pairs_found'] / self.stats['total_actions'] * 100):.1f}%)

## Impact

La migration a regroupé {self.stats['unified_created']} paires de commandes Windows/Linux équivalentes,
réduisant le nombre total d'actions de {self.stats['pairs_found']} tout en préservant toutes les informations.

Les utilisateurs peuvent désormais voir les deux versions (Windows et Linux) d'une commande dans
une seule fiche, facilitant l'apprentissage cross-platform.

## Prochaines Étapes

1. Valider le fichier généré : `initial-actions-unified.json`
2. Tester le chargement dans l'application
3. Adapter l'interface utilisateur pour afficher les onglets Windows/Linux
4. Déployer en production

**Date** : 2025-11-25
**Script** : migrate_to_unified.py
"""
        return report


def main():
    """Fonction principale"""
    import sys

    input_file = 'data/seed/initial-actions.json'
    output_file = 'data/seed/initial-actions-unified.json'
    report_file = 'RAPPORT_MIGRATION_UNIFIED.md'

    print()
    print("╔════════════════════════════════════════════════════════════════════╗")
    print("║                                                                    ║")
    print("║     MIGRATION VERS FICHES UNIFIÉES CROSS-PLATFORM                 ║")
    print("║                                                                    ║")
    print("╚════════════════════════════════════════════════════════════════════╝")
    print()

    # Charger
    print(f"📖 Chargement de {input_file}...")
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        print(f"✅ Chargé: {len(data.get('actions', []))} actions")
    except Exception as e:
        print(f"❌ Erreur: {e}")
        sys.exit(1)

    print()

    # Migrer
    migrator = CrossPlatformMigrator(data)
    unified_data = migrator.migrate()

    # Sauvegarder
    print(f"💾 Sauvegarde vers {output_file}...")
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(unified_data, f, ensure_ascii=False, indent=2)
        print(f"✅ Sauvegardé!")
    except Exception as e:
        print(f"❌ Erreur: {e}")
        sys.exit(1)

    # Générer le rapport
    print(f"📄 Génération du rapport {report_file}...")
    report = migrator.generate_report()
    try:
        with open(report_file, 'w', encoding='utf-8') as f:
            f.write(report)
        print(f"✅ Rapport généré!")
    except Exception as e:
        print(f"❌ Erreur: {e}")

    print()
    print("╔════════════════════════════════════════════════════════════════════╗")
    print("║                                                                    ║")
    print("║                ✨ MIGRATION TERMINÉE ! ✨                          ║")
    print("║                                                                    ║")
    print("╚════════════════════════════════════════════════════════════════════╝")
    print()
    print(f"📄 Fichier unifié : {output_file}")
    print(f"📊 Rapport : {report_file}")
    print()


if __name__ == '__main__':
    main()
