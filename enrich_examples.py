"""
Script d'enrichissement des exemples TwinShell
Transforme chaque commande en ressource pédagogique complète
"""

import json
import re
from typing import List, Dict, Any
from copy import deepcopy


class ExampleEnricher:
    """Classe principale pour enrichir les exemples des commandes"""

    def __init__(self, data: Dict[str, Any]):
        self.data = data
        self.actions = data.get('actions', [])
        self.enrichment_stats = {
            'total_actions': len(self.actions),
            'enriched': 0,
            'examples_added': 0
        }

    def enrich_all(self) -> Dict[str, Any]:
        """Enrichit toutes les actions"""
        print(f"Enrichissement de {len(self.actions)} actions...\n")

        for idx, action in enumerate(self.actions, 1):
            if idx % 50 == 0:
                print(f"  Progression: {idx}/{len(self.actions)} actions traitées")

            self.enrich_action(action)

        print(f"\n✅ Enrichissement terminé!")
        print(f"   Actions enrichies: {self.enrichment_stats['enriched']}")
        print(f"   Exemples ajoutés: {self.enrichment_stats['examples_added']}")

        return self.data

    def enrich_action(self, action: Dict[str, Any]):
        """Enrichit une action spécifique"""
        action_id = action.get('id', '')
        category = action.get('category', '')

        # Obtenir les exemples actuels
        current_examples = action.get('examples', [])
        initial_count = len(current_examples)

        # Enrichir selon la catégorie
        new_examples = []

        if '🏢 Active Directory' in category:
            new_examples = self.enrich_ad_examples(action)
        elif '🌐 Network' in category or 'DNS' in category:
            new_examples = self.enrich_network_examples(action)
        elif '📊 Monitoring' in category or 'Logs' in category:
            new_examples = self.enrich_monitoring_examples(action)
        elif '💻 Windows' in category:
            new_examples = self.enrich_windows_examples(action)
        elif '🐧' in category:
            new_examples = self.enrich_linux_examples(action)
        elif 'Performance' in category or '⚡' in category:
            new_examples = self.enrich_performance_examples(action)
        elif 'GPO' in action_id or 'gpo' in action_id.lower():
            new_examples = self.enrich_gpo_examples(action)
        elif 'Security' in category or '🔒' in category or '🔐' in category:
            new_examples = self.enrich_security_examples(action)
        elif 'Git' in category or '🔀' in category:
            new_examples = self.enrich_git_examples(action)
        elif 'Storage' in category or 'Backup' in category:
            new_examples = self.enrich_storage_examples(action)
        else:
            new_examples = self.enrich_generic_examples(action)

        # Si des exemples ont été générés, les ajouter
        if new_examples:
            action['examples'] = new_examples
            self.enrichment_stats['enriched'] += 1
            self.enrichment_stats['examples_added'] += len(new_examples) - initial_count

    def enrich_ad_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes Active Directory"""
        action_id = action.get('id', '')
        cmd_template = action.get('windowsCommandTemplate', {})
        cmd_pattern = cmd_template.get('commandPattern', '')
        cmd_name = cmd_template.get('name', '')

        examples = []

        # Lister utilisateurs
        if 'list-users' in action_id or ('Get-ADUser' in cmd_pattern and 'Filter *' in cmd_pattern):
            examples = [
                {
                    "command": "Get-ADUser -Filter *",
                    "description": "Liste tous les utilisateurs Active Directory du domaine avec les propriétés par défaut (Name, SamAccountName, DistinguishedName, etc.). Attention : peut être long sur de gros domaines (>10000 utilisateurs), privilégier un filtre plus restrictif en production."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties DisplayName,EmailAddress,Enabled,Department | Select-Object Name,DisplayName,EmailAddress,Enabled,Department | Format-Table -AutoSize",
                    "description": "Liste tous les utilisateurs avec leurs informations essentielles. Le paramètre -Properties charge des propriétés étendues non retournées par défaut. Select-Object permet de choisir les colonnes affichées. Format-Table -AutoSize optimise l'affichage en console."
                },
                {
                    "command": "Get-ADUser -Filter {Enabled -eq $true} -Properties LastLogonDate | Select-Object Name,SamAccountName,LastLogonDate | Sort-Object LastLogonDate -Descending",
                    "description": "Liste uniquement les utilisateurs actifs triés par date de dernière connexion (du plus récent au plus ancien). Le filtre {Enabled -eq $true} exclut les comptes désactivés. Utile pour identifier les utilisateurs actifs et repérer rapidement les connexions récentes."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties LastLogonDate | Where-Object {$_.Enabled -eq $true -and $_.LastLogonDate -lt (Get-Date).AddDays(-90)} | Select-Object Name,SamAccountName,LastLogonDate,Enabled",
                    "description": "Audit de sécurité : identifie les utilisateurs actifs qui ne se sont pas connectés depuis 90 jours. LastLogonDate n'étant pas une propriété par défaut, elle doit être spécifiée dans -Properties. Where-Object filtre selon des conditions PowerShell complexes. Essentiel pour les audits trimestriels et l'identification des comptes zombies."
                },
                {
                    "command": "Get-ADUser -Filter {Department -like 'IT*'} -Properties Department,Title,Manager -SearchBase 'OU=Users,DC=contoso,DC=com'",
                    "description": "Recherche les utilisateurs d'un département spécifique dans une OU particulière. Le paramètre -SearchBase limite la recherche à une branche de l'arbre AD pour améliorer significativement les performances (jusqu'à 10x plus rapide sur de gros domaines). L'opérateur -like permet les wildcards (*) pour des recherches partielles."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties * | Select-Object -First 1 | Format-List *",
                    "description": "Affiche TOUTES les propriétés disponibles pour un utilisateur (mode découverte). Le paramètre -Properties * charge l'intégralité des 100+ propriétés AD disponibles (très coûteux en performance). Format-List * affiche chaque propriété sur une ligne séparée. Utilisez cette commande uniquement pour découvrir les noms de propriétés, jamais en production sur de gros volumes."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties Department,EmailAddress,Enabled | Export-Csv -Path C:\\Temp\\ADUsers.csv -NoTypeInformation -Encoding UTF8",
                    "description": "Exporte tous les utilisateurs vers un fichier CSV exploitable dans Excel. -NoTypeInformation supprime la ligne de métadonnées '#TYPE' du CSV. -Encoding UTF8 garantit la compatibilité internationale des caractères accentués. Le fichier CSV peut ensuite être utilisé pour des rapports, des imports ou des analyses tierces."
                },
                {
                    "command": "Get-ADUser -Filter * -ResultSetSize 100 -Properties DisplayName,EmailAddress",
                    "description": "Limite le résultat aux 100 premiers utilisateurs pour un aperçu rapide. Le paramètre -ResultSetSize plafonne le nombre de résultats retournés par le serveur AD, ce qui améliore drastiquement les performances et réduit la consommation mémoire. Idéal pour les tests et les vérifications rapides."
                }
            ]

        # Rechercher utilisateur
        elif 'search-user' in action_id:
            examples = [
                {
                    "command": "Get-ADUser -Filter \"Name -like '*Dupont*'\" -Properties DisplayName,EmailAddress,Department",
                    "description": "Recherche tous les utilisateurs dont le nom contient 'Dupont' (insensible à la casse). L'opérateur -like avec wildcards (*) permet une recherche partielle. Retourne le nom d'affichage, l'email et le département. Pratique pour trouver rapidement un utilisateur dont on ne connaît qu'une partie du nom."
                },
                {
                    "command": "Get-ADUser -Filter \"SamAccountName -eq 'jdupont'\" -Properties *",
                    "description": "Recherche exacte par SamAccountName (identifiant de connexion) et affiche toutes les propriétés. L'opérateur -eq effectue une comparaison stricte. Utilisez cette méthode quand vous connaissez précisément l'identifiant de connexion de l'utilisateur."
                },
                {
                    "command": "Get-ADUser -Filter \"EmailAddress -like '*@contoso.com'\" -Properties EmailAddress,Enabled",
                    "description": "Recherche tous les utilisateurs d'un domaine de messagerie spécifique. Utile pour identifier tous les comptes d'une organisation lors d'une fusion/acquisition ou pour vérifier la cohérence des adresses email."
                },
                {
                    "command": "Get-ADUser -Filter \"(Name -like '*Dupont*') -or (DisplayName -like '*Dupont*')\" -Properties DisplayName,EmailAddress",
                    "description": "Recherche étendue sur plusieurs champs avec l'opérateur logique -or. Cherche 'Dupont' dans le Name OU le DisplayName. Augmente les chances de trouver l'utilisateur quand on ne sait pas dans quel champ le nom est stocké."
                },
                {
                    "command": "Get-ADUser -Filter \"GivenName -eq 'Jean' -and Surname -eq 'Dupont'\" -Properties GivenName,Surname,EmailAddress",
                    "description": "Recherche par prénom ET nom de famille avec l'opérateur -and. Plus précis qu'une recherche sur le nom complet car les champs sont séparés. Idéal quand vous connaissez exactement le prénom et le nom."
                },
                {
                    "command": "Get-ADUser -Filter \"Department -eq 'IT' -and Enabled -eq $true\" -Properties Department,Title,Manager",
                    "description": "Recherche multicritère : utilisateurs actifs d'un département spécifique. Combine plusieurs conditions pour affiner les résultats. Très utilisé pour créer des listes de diffusion, des rapports par département ou des analyses organisationnelles."
                },
                {
                    "command": "Get-ADUser -Filter \"Description -like '*externe*'\" -Properties Description,Company,Enabled",
                    "description": "Recherche textuelle dans le champ Description. Utile pour identifier des types de comptes spécifiques (consultants, externes, temporaires) si votre organisation utilise des conventions de nommage dans ce champ."
                }
            ]

        # Déverrouiller compte
        elif 'unlock' in action_id:
            examples = [
                {
                    "command": "Unlock-ADAccount -Identity jdupont",
                    "description": "Déverrouille immédiatement le compte de jdupont. Le paramètre -Identity accepte le SamAccountName, Distinguished Name, GUID ou SID. Le déverrouillage est instantané et permet à l'utilisateur de se reconnecter immédiatement."
                },
                {
                    "command": "Get-ADUser -Filter {LockedOut -eq $true} | Unlock-ADAccount",
                    "description": "Déverrouille automatiquement TOUS les comptes verrouillés du domaine via pipeline. Attention : à utiliser avec précaution car certains comptes peuvent être verrouillés pour des raisons de sécurité (tentatives d'intrusion). Vérifiez toujours les logs avant un déverrouillage massif."
                },
                {
                    "command": "Unlock-ADAccount -Identity jdupont -Confirm:$false",
                    "description": "Déverrouille sans demander de confirmation. Le paramètre -Confirm:$false supprime l'invite de confirmation (utile pour les scripts automatisés). Par défaut, Unlock-ADAccount ne demande pas de confirmation, mais ce switch est utile si une politique organisationnelle force les confirmations."
                },
                {
                    "command": "Get-ADUser jdupont -Properties LockedOut,LockoutTime | Select-Object Name,LockedOut,LockoutTime",
                    "description": "Vérifie l'état de verrouillage d'un compte avant de le déverrouiller. La propriété LockedOut indique si le compte est verrouillé ($true/$false). LockoutTime affiche l'heure exacte du verrouillage. Toujours vérifier ces informations avant de déverrouiller pour comprendre la cause du verrouillage."
                },
                {
                    "command": "Unlock-ADAccount -Identity 'CN=Jean Dupont,OU=Users,DC=contoso,DC=com'",
                    "description": "Déverrouille un compte en utilisant son Distinguished Name (DN) complet. Cette syntaxe est utile quand vous travaillez avec des scripts qui manipulent des objets AD complets ou quand le SamAccountName n'est pas unique."
                },
                {
                    "command": "Get-ADUser -Filter {LockedOut -eq $true} -Properties LockedOut,LockoutTime | Select-Object Name,SamAccountName,LockedOut,LockoutTime | Export-Csv C:\\Temp\\LockedAccounts.csv -NoTypeInformation",
                    "description": "Exporte la liste de tous les comptes verrouillés avec l'heure de verrouillage. Utile pour les audits de sécurité, l'analyse des patterns de verrouillage et l'identification de potentielles attaques par force brute. Conservez ces rapports pour la traçabilité."
                },
                {
                    "command": "Unlock-ADAccount -Identity jdupont -PassThru | Select-Object Name,LockedOut",
                    "description": "Déverrouille et affiche immédiatement le statut final du compte. Le switch -PassThru force la cmdlet à retourner l'objet utilisateur modifié dans le pipeline, permettant de vérifier que le déverrouillage a bien fonctionné. Essentiel pour les scripts avec gestion d'erreurs."
                }
            ]

        # Reset password
        elif 'reset-password' in action_id or 'password' in action_id:
            examples = [
                {
                    "command": "Set-ADAccountPassword -Identity jdupont -Reset -NewPassword (ConvertTo-SecureString -AsPlainText \"P@ssw0rd123!\" -Force)",
                    "description": "Réinitialise le mot de passe de jdupont avec un nouveau mot de passe temporaire. ConvertTo-SecureString convertit le texte clair en SecureString (obligatoire pour les mots de passe). Le paramètre -Force supprime l'avertissement de sécurité. ATTENTION : le mot de passe doit respecter la politique de complexité du domaine."
                },
                {
                    "command": "$SecurePassword = Read-Host -AsSecureString -Prompt 'Nouveau mot de passe'; Set-ADAccountPassword -Identity jdupont -Reset -NewPassword $SecurePassword",
                    "description": "Méthode sécurisée : demande interactivement le mot de passe sans l'afficher à l'écran. Read-Host -AsSecureString masque la saisie (affiche des astérisques). Cette approche est plus sécurisée car le mot de passe n'apparaît jamais en clair dans l'historique PowerShell ou les logs. Recommandée pour les réinitialisations manuelles."
                },
                {
                    "command": "Set-ADAccountPassword -Identity jdupont -Reset -NewPassword (ConvertTo-SecureString 'TempP@ss2024!' -AsPlainText -Force) -PassThru | Set-ADUser -ChangePasswordAtLogon $true",
                    "description": "Réinitialise le mot de passe ET force l'utilisateur à le changer à la prochaine connexion. Le switch -PassThru passe l'objet utilisateur au pipeline. Set-ADUser -ChangePasswordAtLogon $true active le flag de changement obligatoire. C'est la méthode recommandée pour les réinitialisations : l'admin définit un mot de passe temporaire, l'utilisateur le change immédiatement."
                },
                {
                    "command": "Set-ADAccountPassword -Identity jdupont -OldPassword (ConvertTo-SecureString 'OldP@ss' -AsPlainText -Force) -NewPassword (ConvertTo-SecureString 'NewP@ss123!' -AsPlainText -Force)",
                    "description": "Change le mot de passe en connaissant l'ancien (sans droits admin). Utilisez -OldPassword quand l'utilisateur change son propre mot de passe ou quand la politique exige la connaissance de l'ancien. Sans -Reset, cette commande nécessite les droits de l'utilisateur concerné mais pas de droits admin."
                },
                {
                    "command": "Set-ADAccountPassword -Identity jdupont -Reset -NewPassword (ConvertTo-SecureString 'P@ssw0rd!' -AsPlainText -Force) -Server DC01.contoso.com",
                    "description": "Réinitialise le mot de passe sur un contrôleur de domaine spécifique. Le paramètre -Server force l'exécution sur le DC nommé. Utile dans les environnements multi-sites pour garantir la réplication immédiate ou lors du troubleshooting de problèmes de réplication."
                },
                {
                    "command": "Get-ADUser -Filter {PasswordExpired -eq $true} | Select-Object Name,SamAccountName,PasswordExpired,PasswordLastSet",
                    "description": "Liste tous les utilisateurs dont le mot de passe a expiré. PasswordExpired indique si le mot de passe est périmé selon la politique du domaine. PasswordLastSet affiche la date du dernier changement. Utile pour identifier les comptes nécessitant une réinitialisation et pour les audits de conformité."
                },
                {
                    "command": "Set-ADUser -Identity jdupont -PasswordNeverExpires $true",
                    "description": "Configure le compte pour que le mot de passe n'expire jamais. ATTENTION : cette commande viole généralement les politiques de sécurité et ne devrait être utilisée que pour des comptes de service spécifiques. Documentez toujours la raison et obtenez une validation."
                }
            ]

        # Disable account
        elif 'disable' in action_id:
            examples = [
                {
                    "command": "Disable-ADAccount -Identity jdupont",
                    "description": "Désactive immédiatement le compte de jdupont. L'utilisateur ne pourra plus se connecter mais le compte et toutes ses données restent dans AD. La désactivation est réversible avec Enable-ADAccount. Méthode recommandée pour les départs temporaires (congés longs, suspensions)."
                },
                {
                    "command": "Disable-ADAccount -Identity jdupont -Confirm:$false",
                    "description": "Désactive le compte sans demander de confirmation. Le paramètre -Confirm:$false supprime l'invite (utile pour les scripts automatisés). Utilisez avec précaution : la désactivation est immédiate et peut déconnecter l'utilisateur de toutes ses sessions actives."
                },
                {
                    "command": "Get-ADUser -Filter {Enabled -eq $true -and LastLogonDate -lt (Get-Date).AddDays(-180)} -Properties LastLogonDate | Disable-ADAccount -WhatIf",
                    "description": "Simulation de désactivation des comptes inactifs depuis 180 jours. Le switch -WhatIf affiche ce qui serait fait SANS exécuter l'action (mode dry-run). Indispensable avant toute action de masse pour éviter les erreurs. Retirez -WhatIf après vérification pour exécuter réellement."
                },
                {
                    "command": "Get-ADUser jdupont -Properties Enabled | Select-Object Name,Enabled,SamAccountName",
                    "description": "Vérifie si un compte est actif ou désactivé. La propriété Enabled vaut $true (actif) ou $false (désactivé). Toujours vérifier l'état avant de désactiver ou réactiver. Utile aussi pour les scripts de reporting et les tableaux de bord."
                },
                {
                    "command": "Disable-ADAccount -Identity jdupont -PassThru | Set-ADUser -Description \"Compte désactivé le $(Get-Date -Format 'dd/MM/yyyy') - Départ entreprise\"",
                    "description": "Désactive le compte et ajoute automatiquement une note avec la date et la raison. Le switch -PassThru permet de passer l'objet au pipeline. Set-ADUser -Description documente l'action pour la traçabilité. Excellente pratique pour les audits : toujours documenter les raisons de désactivation."
                },
                {
                    "command": "Get-ADUser -Filter {Department -eq 'Finance' -and Enabled -eq $true} | Select-Object Name,SamAccountName | Export-Csv C:\\Temp\\FinanceUsersBeforeDisable.csv -NoTypeInformation; Get-ADUser -Filter {Department -eq 'Finance'} | Disable-ADAccount",
                    "description": "Désactivation massive avec sauvegarde préalable : exporte d'abord la liste des utilisateurs Finance actifs, puis les désactive tous. La sauvegarde CSV permet une restauration en cas d'erreur. Les deux commandes sont séparées par un point-virgule pour exécution séquentielle. Toujours faire une sauvegarde avant une opération de masse."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties Enabled,LastLogonDate | Where-Object {$_.Enabled -eq $false} | Select-Object Name,SamAccountName,Enabled,LastLogonDate | Export-Csv C:\\Temp\\DisabledAccounts.csv -NoTypeInformation",
                    "description": "Exporte un rapport complet de tous les comptes désactivés avec leur dernière connexion. Utile pour les audits de sécurité, l'analyse des comptes dormants et la préparation au nettoyage de l'AD. Effectuez ce rapport trimestriellement pour maintenir un AD propre."
                }
            ]

        # Enable account
        elif 'enable' in action_id:
            examples = [
                {
                    "command": "Enable-ADAccount -Identity jdupont",
                    "description": "Réactive immédiatement un compte désactivé. L'utilisateur peut se reconnecter dès que la réplication AD est complète (quelques secondes à quelques minutes selon la topo réseau). Utilisé pour les retours de congés, fins de suspension disciplinaire, etc."
                },
                {
                    "command": "Enable-ADAccount -Identity jdupont -PassThru | Select-Object Name,Enabled",
                    "description": "Réactive le compte et affiche immédiatement la confirmation. Le switch -PassThru retourne l'objet utilisateur modifié. Select affiche le nom et le statut Enabled pour vérification visuelle. Pratique pour confirmer que la réactivation a fonctionné."
                },
                {
                    "command": "Get-ADUser -Filter {Enabled -eq $false -and Department -eq 'IT'} | Enable-ADAccount -WhatIf",
                    "description": "Simulation de réactivation de tous les comptes IT désactivés. -WhatIf effectue un dry-run sans modifier l'AD. Affiche quels comptes seraient réactivés. Vérifiez toujours avec -WhatIf avant d'exécuter des actions de masse."
                },
                {
                    "command": "Enable-ADAccount -Identity jdupont -Confirm:$false",
                    "description": "Réactive sans confirmation interactive. Utile dans les scripts automatisés ou les processus de réintégration standardisés. La réactivation est immédiate dès l'exécution."
                },
                {
                    "command": "Get-ADUser -Filter * -Properties Enabled,Description | Where-Object {$_.Enabled -eq $false -and $_.Description -like '*congé*'} | Enable-ADAccount",
                    "description": "Réactive automatiquement les comptes désactivés pour congés (basé sur le champ Description). Utile si votre organisation utilise des conventions de nommage dans Description. Adaptez le filtre selon vos pratiques internes."
                }
            ]

        # Get AD Computer
        elif 'computer' in action_id.lower() and 'get' in cmd_name.lower():
            examples = [
                {
                    "command": "Get-ADComputer -Filter *",
                    "description": "Liste tous les ordinateurs du domaine Active Directory avec leurs propriétés par défaut. Retourne Name, DNSHostName, DistinguishedName, Enabled, etc. Attention : peut être très long sur de gros domaines avec des milliers de machines."
                },
                {
                    "command": "Get-ADComputer -Filter {OperatingSystem -like '*Server*'} -Properties OperatingSystem,OperatingSystemVersion",
                    "description": "Liste uniquement les serveurs Windows. Le filtre sur OperatingSystem identifie les OS serveurs (Windows Server 2012/2016/2019/2022). Utile pour l'inventaire des serveurs, les rapports de compliance et la planification des mises à jour."
                },
                {
                    "command": "Get-ADComputer -Filter {Enabled -eq $true} -Properties LastLogonDate | Where-Object {$_.LastLogonDate -lt (Get-Date).AddDays(-90)} | Select-Object Name,LastLogonDate,Enabled",
                    "description": "Identifie les ordinateurs actifs mais non connectés depuis 90 jours. Détecte les machines zombies, les postes perdus ou volés. LastLogonDate doit être spécifié dans -Properties. Essentiel pour le nettoyage AD et les audits de sécurité."
                },
                {
                    "command": "Get-ADComputer -Filter {Name -like 'LAPTOP-*'} -Properties OperatingSystem,IPv4Address",
                    "description": "Recherche tous les ordinateurs portables (selon une convention de nommage). Retourne l'OS et l'adresse IP. Utile pour l'inventaire des laptops, la gestion du parc mobile et les déploiements ciblés."
                },
                {
                    "command": "Get-ADComputer -Filter * -Properties * | Select-Object -First 1 | Format-List *",
                    "description": "Affiche TOUTES les propriétés disponibles pour un ordinateur (mode découverte). -Properties * charge toutes les propriétés (coûteux). Format-List * affiche chaque propriété sur une ligne. Utilisez uniquement pour découvrir les noms de propriétés disponibles."
                },
                {
                    "command": "Get-ADComputer -Filter * -Properties OperatingSystem,OperatingSystemVersion,Created | Select-Object Name,OperatingSystem,OperatingSystemVersion,Created | Export-Csv C:\\Temp\\ComputerInventory.csv -NoTypeInformation",
                    "description": "Exporte un inventaire complet des ordinateurs avec OS et date de création. Le CSV peut être importé dans Excel pour analyse, reporting ou documentation. Utile pour les audits, la gestion du parc et les statistiques."
                },
                {
                    "command": "Get-ADComputer -Identity WKS-IT-01 -Properties *",
                    "description": "Affiche toutes les informations d'un ordinateur spécifique. -Identity accepte le nom, DN, GUID ou SID. Retourne toutes les propriétés. Utile pour le troubleshooting et la vérification de configuration d'une machine."
                }
            ]

        # Get AD Group
        elif 'group' in action_id.lower() and ('list' in action_id or 'get' in cmd_name.lower()):
            examples = [
                {
                    "command": "Get-ADGroup -Filter *",
                    "description": "Liste tous les groupes Active Directory du domaine. Retourne le nom, le GroupCategory (Distribution/Security), le GroupScope (DomainLocal/Global/Universal) et le DistinguishedName. Peut être lent sur de gros domaines avec des milliers de groupes."
                },
                {
                    "command": "Get-ADGroup -Filter {GroupCategory -eq 'Security' -and GroupScope -eq 'Global'}",
                    "description": "Liste uniquement les groupes de sécurité globaux. GroupCategory peut être 'Security' ou 'Distribution'. GroupScope peut être 'DomainLocal', 'Global' ou 'Universal'. Les groupes de sécurité globaux sont les plus courants pour les permissions sur ressources."
                },
                {
                    "command": "Get-ADGroup -Filter {Name -like '*Admin*'} -Properties Description,Members",
                    "description": "Recherche tous les groupes contenant 'Admin' dans le nom. Utile pour identifier les groupes à privilèges élevés. La propriété Description aide à comprendre l'usage du groupe. Members liste les membres (mais utilisez plutôt Get-ADGroupMember pour une liste détaillée)."
                },
                {
                    "command": "Get-ADGroup -Identity 'Domain Admins' -Properties Members,MemberOf,Description",
                    "description": "Affiche les détails d'un groupe spécifique. Members liste les membres directs. MemberOf liste les groupes dont ce groupe est membre (imbrication). Description documente le rôle du groupe. Essentiel pour l'audit des groupes sensibles."
                },
                {
                    "command": "Get-ADGroup -Filter * -Properties ManagedBy | Where-Object {$_.ManagedBy -ne $null} | Select-Object Name,ManagedBy",
                    "description": "Liste tous les groupes qui ont un gestionnaire défini. La propriété ManagedBy contient le DN de l'utilisateur responsable du groupe. Utile pour identifier les groupes gérés et contacter les responsables lors de revues d'accès."
                },
                {
                    "command": "Get-ADGroup -Filter {GroupCategory -eq 'Distribution'} | Measure-Object",
                    "description": "Compte le nombre de groupes de distribution (listes de diffusion Exchange). Measure-Object retourne le count total. Utile pour les statistiques, la documentation et les audits Exchange."
                },
                {
                    "command": "Get-ADGroup -Filter * -Properties Created | Sort-Object Created -Descending | Select-Object Name,Created -First 20",
                    "description": "Affiche les 20 groupes les plus récemment créés. Sort-Object -Descending trie du plus récent au plus ancien. Utile pour surveiller les créations de groupes, détecter les créations suspectes et suivre les changements organisationnels."
                }
            ]

        # Add to group
        elif 'add' in action_id.lower() and 'group' in action_id.lower():
            examples = [
                {
                    "command": "Add-ADGroupMember -Identity 'Domain Admins' -Members jdupont",
                    "description": "Ajoute jdupont au groupe Domain Admins. -Identity spécifie le groupe cible. -Members peut être un SamAccountName, DN, GUID ou SID. L'ajout est immédiat mais l'utilisateur doit se reconnecter pour que les nouveaux droits soient actifs."
                },
                {
                    "command": "Add-ADGroupMember -Identity 'IT_Support' -Members jdupont,mmartin,ldubois",
                    "description": "Ajoute plusieurs utilisateurs simultanément au groupe. -Members accepte un tableau de valeurs séparées par des virgules. Beaucoup plus efficace que des commandes individuelles : un seul appel AD au lieu de trois."
                },
                {
                    "command": "Get-ADUser -Filter {Department -eq 'IT'} | ForEach-Object {Add-ADGroupMember -Identity 'IT_All_Users' -Members $_.SamAccountName}",
                    "description": "Ajoute automatiquement tous les utilisateurs du département IT à un groupe. ForEach-Object itère sur chaque résultat. $_.SamAccountName récupère l'identifiant de chaque utilisateur. Utile pour les ajouts de masse lors de réorganisations."
                },
                {
                    "command": "Add-ADGroupMember -Identity 'IT_Support' -Members jdupont -Confirm:$false",
                    "description": "Ajoute au groupe sans demander de confirmation. -Confirm:$false supprime l'invite. Par défaut Add-ADGroupMember ne demande pas de confirmation, mais ce switch est utile si une politique force les confirmations."
                },
                {
                    "command": "Add-ADGroupMember -Identity 'Groupe_Projets' -Members (Get-ADGroup 'IT_Team').DistinguishedName",
                    "description": "Ajoute un groupe entier comme membre d'un autre groupe (imbrication). Get-ADGroup récupère l'objet groupe source. L'imbrication permet une gestion hiérarchique : modifier IT_Team impacte automatiquement Groupe_Projets."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' | Select-Object Name,SamAccountName; Add-ADGroupMember -Identity 'IT_Support' -Members jdupont -WhatIf",
                    "description": "Affiche d'abord les membres actuels du groupe, puis simule l'ajout avec -WhatIf. Cette approche prudente permet de vérifier que l'utilisateur n'est pas déjà membre avant l'ajout. Retirez -WhatIf après vérification pour exécuter réellement."
                }
            ]

        # Remove from group
        elif 'remove' in action_id.lower() and 'group' in action_id.lower():
            examples = [
                {
                    "command": "Remove-ADGroupMember -Identity 'Domain Admins' -Members jdupont -Confirm:$false",
                    "description": "Retire jdupont du groupe Domain Admins sans confirmation. -Confirm:$false supprime l'invite de confirmation (par défaut, Remove-ADGroupMember demande toujours confirmation pour les opérations sensibles). Le retrait est immédiat mais l'utilisateur doit se reconnecter pour perdre les droits."
                },
                {
                    "command": "Remove-ADGroupMember -Identity 'IT_Support' -Members jdupont,mmartin -Confirm:$false",
                    "description": "Retire plusieurs utilisateurs simultanément d'un groupe. -Members accepte un tableau de valeurs. Plus efficace que des commandes individuelles. Utile lors des départs de collaborateurs ou réorganisations."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' | Where-Object {$_.objectClass -eq 'user'} | ForEach-Object {Remove-ADGroupMember -Identity 'IT_Support' -Members $_.SamAccountName -Confirm:$false}",
                    "description": "Vide complètement un groupe de tous ses utilisateurs (conserve les groupes imbriqués). Get-ADGroupMember liste les membres. Where-Object filtre uniquement les utilisateurs. ForEach-Object retire chacun. Utilisez avec EXTRÊME précaution et toujours avec une sauvegarde préalable."
                },
                {
                    "command": "Remove-ADGroupMember -Identity 'Groupe_Projet_X' -Members jdupont -WhatIf",
                    "description": "Simulation du retrait avec -WhatIf (dry-run). Affiche ce qui serait fait sans exécuter. Vérifiez toujours avec -WhatIf avant de retirer des membres de groupes sensibles. Retirez -WhatIf après validation."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' | Export-Csv C:\\Temp\\IT_Support_Backup.csv -NoTypeInformation; Remove-ADGroupMember -Identity 'IT_Support' -Members jdupont -Confirm:$false",
                    "description": "Bonne pratique : sauvegarde les membres du groupe avant retrait. L'export CSV permet une restauration facile en cas d'erreur. Les deux commandes sont séparées par un point-virgule pour exécution séquentielle. Toujours sauvegarder avant modifications critiques."
                }
            ]

        # Get group members
        elif 'member' in action_id.lower() and ('list' in action_id or 'get' in cmd_name.lower()):
            examples = [
                {
                    "command": "Get-ADGroupMember -Identity 'Domain Admins'",
                    "description": "Liste tous les membres directs du groupe Domain Admins. Retourne le nom, le type d'objet (user/group/computer) et le DN. N'affiche PAS les membres des groupes imbriqués (voir -Recursive pour cela)."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' -Recursive",
                    "description": "Liste TOUS les membres incluant les groupes imbriqués (récursif). -Recursive descend dans tous les sous-groupes pour afficher chaque utilisateur final. Essentiel pour connaître qui a réellement accès via des imbrications complexes. Peut être lent sur de grandes structures."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'Domain Admins' | Select-Object Name,SamAccountName,objectClass | Format-Table -AutoSize",
                    "description": "Liste les membres avec formatage lisible. objectClass indique le type (user/group/computer). Format-Table -AutoSize optimise l'affichage en console. Select-Object limite aux colonnes pertinentes."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' | Where-Object {$_.objectClass -eq 'user'} | Measure-Object",
                    "description": "Compte uniquement les utilisateurs membres (exclut les groupes imbriqués). Where-Object filtre par type d'objet. Measure-Object retourne le count. Utile pour les statistiques et le reporting des effectifs."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'Domain Admins' | Get-ADUser -Properties EmailAddress,Enabled | Select-Object Name,EmailAddress,Enabled",
                    "description": "Enrichit la liste des membres avec des propriétés utilisateur étendues. Le pipeline passe chaque membre à Get-ADUser qui charge les propriétés supplémentaires. Utile pour contacter les membres (email) ou vérifier leur statut (Enabled)."
                },
                {
                    "command": "Get-ADGroupMember -Identity 'IT_Support' | Export-Csv C:\\Temp\\IT_Support_Members.csv -NoTypeInformation",
                    "description": "Exporte la liste des membres vers CSV. Utile pour les audits, les revues d'accès trimestrielles, la documentation et l'archivage. Le fichier CSV peut être importé dans Excel pour analyse ou envoyé aux managers pour validation."
                },
                {
                    "command": "(Get-ADGroupMember -Identity 'Domain Admins').Count",
                    "description": "Compte rapidement le nombre de membres directs. Retourne uniquement le chiffre. Méthode la plus rapide pour obtenir un count sans afficher tous les membres. Utile pour les scripts de monitoring et les dashboards."
                }
            ]

        # Si aucun cas spécifique, utiliser les exemples existants ou en créer de génériques
        if not examples and cmd_pattern:
            examples = action.get('examples', [])
            if len(examples) < 3:
                # Ajouter au moins 2-3 exemples génériques basés sur le pattern
                examples = self.enrich_generic_ad_examples(action)

        return examples if examples else action.get('examples', [])

    def enrich_generic_ad_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Génère des exemples génériques pour les commandes AD non couvertes"""
        cmd_template = action.get('windowsCommandTemplate', {})
        cmd_pattern = cmd_template.get('commandPattern', '')
        current_examples = action.get('examples', [])

        # Si déjà des exemples, les garder et en ajouter quelques-uns
        examples = deepcopy(current_examples)

        # Ajouter 2-3 exemples génériques si pas assez
        if len(examples) < 5:
            if '-WhatIf' not in cmd_pattern and 'Get-' not in cmd_pattern:
                examples.append({
                    "command": f"{cmd_pattern} -WhatIf",
                    "description": "Simulation de l'action avec -WhatIf (dry-run). Affiche ce qui serait fait sans exécuter réellement. Indispensable pour tester les commandes de modification avant exécution. Retirez -WhatIf après validation pour exécuter."
                })

            if '-Confirm' not in cmd_pattern and 'Get-' not in cmd_pattern:
                examples.append({
                    "command": f"{cmd_pattern} -Confirm:$false",
                    "description": "Exécute l'action sans demander de confirmation interactive. Le paramètre -Confirm:$false supprime les invites de validation. Utile dans les scripts automatisés mais à utiliser avec précaution."
                })

            if 'Get-' in cmd_pattern and '-Properties' not in cmd_pattern:
                base_cmd = cmd_pattern.split('|')[0].strip()
                examples.append({
                    "command": f"{base_cmd} -Properties *",
                    "description": "Affiche toutes les propriétés disponibles pour cet objet AD. Le paramètre -Properties * charge l'intégralité des propriétés (coûteux en performance). Utile uniquement pour découvrir les noms de propriétés, à éviter en production sur de gros volumes."
                })

        return examples

    def enrich_network_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes réseau et DNS"""
        action_id = action.get('id', '')
        cmd_template = action.get('windowsCommandTemplate', {}) or action.get('linuxCommandTemplate', {})
        cmd_pattern = cmd_template.get('commandPattern', '')

        examples = []

        # Test-Connection (ping)
        if 'test-connection' in action_id.lower() or 'Test-Connection' in cmd_pattern:
            examples = [
                {
                    "command": "Test-Connection -ComputerName google.com -Count 4",
                    "description": "Envoie 4 paquets ICMP (ping) vers google.com. Équivalent PowerShell de 'ping'. Retourne le temps de réponse, le TTL et le statut. Le paramètre -Count limite le nombre de paquets (par défaut : 4). Utile pour vérifier la connectivité Internet basique."
                },
                {
                    "command": "Test-Connection -ComputerName 192.168.1.1 -Count 1 -Quiet",
                    "description": "Test de connectivité rapide qui retourne uniquement $true ou $false. Le switch -Quiet supprime tous les détails et retourne un booléen simple. Parfait pour les scripts de monitoring : if (Test-Connection -ComputerName server01 -Quiet) { ... }"
                },
                {
                    "command": "Test-Connection -ComputerName server01,server02,server03 -Count 2",
                    "description": "Teste plusieurs hôtes simultanément. -ComputerName accepte un tableau de noms/IPs. PowerShell teste tous les hôtes en parallèle, ce qui est beaucoup plus rapide que des commandes séquentielles. Idéal pour vérifier rapidement la disponibilité d'une liste de serveurs."
                },
                {
                    "command": "Test-Connection -ComputerName 8.8.8.8 -Count 100 -Delay 1",
                    "description": "Test de connectivité prolongé avec 100 paquets espacés de 1 seconde. Utile pour diagnostiquer des problèmes intermittents, mesurer la stabilité de la connexion et détecter les pertes de paquets. Laissez tourner pendant le troubleshooting."
                },
                {
                    "command": "Test-Connection -ComputerName server01 -Source DC01",
                    "description": "Exécute le ping depuis un ordinateur distant (DC01 vers server01). Nécessite WinRM activé et des droits admin sur la source. Utile pour tester la connectivité entre deux serveurs distants sans s'y connecter physiquement."
                },
                {
                    "command": "Get-Content C:\\servers.txt | ForEach-Object {Test-Connection $_ -Count 1 -Quiet} | Where-Object {$_ -eq $false}",
                    "description": "Lit une liste de serveurs depuis un fichier et identifie lesquels sont inaccessibles. Get-Content charge le fichier ligne par ligne. Test-Connection -Quiet retourne $true/$false. Where-Object filtre les échecs. Parfait pour les vérifications quotidiennes de disponibilité."
                },
                {
                    "command": "Test-Connection -ComputerName google.com -Count 4 | Select-Object Address,ResponseTime | Format-Table -AutoSize",
                    "description": "Affiche uniquement l'adresse et le temps de réponse dans un tableau propre. Select-Object filtre les colonnes pertinentes. Format-Table -AutoSize optimise l'affichage. Utile pour des rapports de performance réseau lisibles."
                }
            ]

        # Resolve-DnsName
        elif 'resolve-dns' in action_id.lower() or 'Resolve-DnsName' in cmd_pattern:
            examples = [
                {
                    "command": "Resolve-DnsName google.com",
                    "description": "Résout le nom de domaine google.com en adresse(s) IP. Équivalent PowerShell de 'nslookup'. Retourne tous les enregistrements A (IPv4) et AAAA (IPv6) associés. Utilise le serveur DNS configuré sur la machine."
                },
                {
                    "command": "Resolve-DnsName google.com -Type MX",
                    "description": "Interroge les enregistrements MX (Mail eXchange) de google.com. Les enregistrements MX indiquent les serveurs de messagerie responsables du domaine. Retourne les serveurs mail avec leur priorité. Essentiel pour diagnostiquer les problèmes d'email et vérifier les configurations SMTP."
                },
                {
                    "command": "Resolve-DnsName contoso.com -Type ALL -Server 8.8.8.8",
                    "description": "Interroge TOUS les types d'enregistrements DNS (A, AAAA, MX, TXT, NS, SOA, etc.) en utilisant le serveur DNS public de Google (8.8.8.8). -Type ALL retourne absolument tous les enregistrements disponibles. -Server force l'utilisation d'un DNS spécifique au lieu du DNS local. Utile pour comparer les résolutions ou diagnostiquer des problèmes de propagation DNS."
                },
                {
                    "command": "Resolve-DnsName _spf.google.com -Type TXT",
                    "description": "Interroge les enregistrements TXT, souvent utilisés pour SPF (Sender Policy Framework), DKIM, DMARC et vérifications de domaine. Les enregistrements TXT contiennent du texte libre utilisé pour la configuration d'email, la sécurité et les validations de propriété de domaine."
                },
                {
                    "command": "Resolve-DnsName 8.8.8.8 -Type PTR",
                    "description": "Résolution DNS inverse (reverse lookup) : cherche le nom de domaine associé à l'IP 8.8.8.8. Les enregistrements PTR mappent IP → nom (inverse de A qui mappe nom → IP). Utile pour identifier un serveur à partir de son IP ou vérifier la cohérence des configurations DNS."
                },
                {
                    "command": "Resolve-DnsName contoso.com -Type NS",
                    "description": "Interroge les enregistrements NS (Name Server) qui indiquent quels serveurs DNS sont autoritaires pour le domaine. Les enregistrements NS définissent où sont hébergées les zones DNS. Essentiel lors de migrations DNS ou pour identifier l'hébergeur DNS d'un domaine."
                },
                {
                    "command": "Resolve-DnsName google.com -DnsOnly",
                    "description": "Force une vraie requête DNS en désactivant le cache local. Le switch -DnsOnly ignore complètement le cache de résolution Windows et force une interrogation du serveur DNS. Utile pour vérifier les changements DNS récents ou diagnostiquer des problèmes de cache."
                },
                {
                    "command": "Get-Content C:\\domains.txt | ForEach-Object {Resolve-DnsName $_ -ErrorAction SilentlyContinue} | Select-Object Name,IPAddress",
                    "description": "Résout en masse une liste de domaines depuis un fichier. Get-Content charge le fichier. ForEach-Object résout chaque ligne. -ErrorAction SilentlyContinue ignore les erreurs (domaines inexistants). Select-Object formate les résultats. Idéal pour valider une liste de domaines ou créer un inventaire DNS."
                }
            ]

        # Get-NetIPAddress
        elif 'netipaddress' in action_id.lower() or 'Get-NetIPAddress' in cmd_pattern:
            examples = [
                {
                    "command": "Get-NetIPAddress",
                    "description": "Affiche toutes les adresses IP configurées sur la machine (IPv4 et IPv6). Retourne l'IP, le masque de sous-réseau (PrefixLength), l'interface réseau associée et le statut. Équivalent moderne de 'ipconfig' avec plus de détails."
                },
                {
                    "command": "Get-NetIPAddress -AddressFamily IPv4",
                    "description": "Affiche uniquement les adresses IPv4 (exclut IPv6). Le paramètre -AddressFamily filtre par famille d'adresses. Simplifie l'affichage quand vous ne travaillez qu'en IPv4. Évite le bruit des nombreuses adresses IPv6 link-local."
                },
                {
                    "command": "Get-NetIPAddress -InterfaceAlias 'Ethernet' -AddressFamily IPv4",
                    "description": "Affiche l'adresse IPv4 d'une interface réseau spécifique. -InterfaceAlias utilise le nom convivial de l'interface (visible dans 'Paramètres réseau'). Utile quand la machine a plusieurs cartes réseau et que vous voulez interroger une carte précise."
                },
                {
                    "command": "Get-NetIPAddress | Where-Object {$_.IPAddress -like '192.168.*'} | Select-Object IPAddress,InterfaceAlias,PrefixLength",
                    "description": "Filtre et affiche uniquement les adresses IP du réseau local 192.168.x.x. Where-Object filtre avec wildcard. Select-Object choisit les colonnes pertinentes. Utile pour identifier rapidement les interfaces sur le LAN dans un environnement multi-cartes."
                },
                {
                    "command": "Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.PrefixOrigin -eq 'Dhcp'}",
                    "description": "Affiche uniquement les adresses IPv4 obtenues via DHCP. PrefixOrigin indique comment l'IP a été configurée (Dhcp/Manual/WellKnown). Utile pour distinguer les IPs statiques des IPs dynamiques lors du troubleshooting réseau."
                },
                {
                    "command": "Get-NetIPAddress | Export-Csv C:\\Temp\\NetworkConfig.csv -NoTypeInformation",
                    "description": "Exporte toute la configuration IP vers CSV. Utile pour documenter la configuration réseau avant modifications, créer des inventaires ou archiver l'état réseau. Le fichier CSV peut être analysé dans Excel."
                }
            ]

        # Test-NetConnection
        elif 'test-netconnection' in action_id.lower() or 'Test-NetConnection' in cmd_pattern:
            examples = [
                {
                    "command": "Test-NetConnection google.com",
                    "description": "Test de connectivité complet vers google.com. Effectue un ping ICMP ET une résolution DNS. Retourne l'IP résolue, le temps de réponse ping et le statut de connectivité. Plus complet que Test-Connection car il combine ping + nslookup."
                },
                {
                    "command": "Test-NetConnection server01 -Port 3389",
                    "description": "Teste si le port RDP (3389) est ouvert sur server01. Effectue un test TCP sur le port spécifié. Retourne TcpTestSucceeded: True/False. Équivalent PowerShell de 'telnet server01 3389'. Essentiel pour diagnostiquer les problèmes de connectivité applicative et vérifier les règles de pare-feu."
                },
                {
                    "command": "Test-NetConnection 192.168.1.100 -Port 443 -InformationLevel Detailed",
                    "description": "Test TCP détaillé sur le port HTTPS (443). -InformationLevel Detailed affiche beaucoup plus d'informations : route réseau, temps de réponse, interfaces utilisées, etc. Utile pour le troubleshooting approfondi des problèmes de connectivité web."
                },
                {
                    "command": "Test-NetConnection mail.contoso.com -Port 25",
                    "description": "Vérifie si le port SMTP (25) est accessible sur le serveur mail. Test crucial avant de configurer un client mail ou diagnostiquer des problèmes d'envoi d'email. Un échec indique généralement un blocage par pare-feu ou un service mail arrêté."
                },
                {
                    "command": "Test-NetConnection server01 -Port 445 -WarningAction SilentlyContinue",
                    "description": "Teste le port SMB/CIFS (445) sans afficher les warnings. -WarningAction SilentlyContinue supprime les messages d'avertissement. Le port 445 est utilisé pour les partages réseau Windows. Utile pour diagnostiquer les problèmes d'accès aux partages."
                },
                {
                    "command": "1..100 | ForEach-Object {Test-NetConnection 192.168.1.$_ -Port 80 -WarningAction SilentlyContinue} | Where-Object {$_.TcpTestSucceeded -eq $true}",
                    "description": "Scan du réseau 192.168.1.0/24 pour trouver tous les serveurs web (port 80 ouvert). 1..100 génère une séquence de nombres. ForEach-Object teste chaque IP. Where-Object filtre les succès. Utile pour l'inventaire réseau ou la détection de serveurs. ATTENTION : peut être considéré comme du scanning et violer certaines politiques de sécurité."
                },
                {
                    "command": "Test-NetConnection server01 -CommonTCPPort RDP",
                    "description": "Teste un port courant en utilisant son nom au lieu du numéro. -CommonTCPPort accepte : HTTP (80), RDP (3389), SMB (445), WINRM (5985). Plus lisible que les numéros de port, facilite la compréhension des scripts."
                }
            ]

        # Linux: ping
        elif action.get('platform') == 1 and ('ping' in action_id.lower() or 'ping' in cmd_pattern):
            examples = [
                {
                    "command": "ping google.com",
                    "description": "Ping continu vers google.com (s'arrête avec Ctrl+C). Envoie des paquets ICMP jusqu'à interruption manuelle. Affiche le temps de réponse, le TTL et les statistiques. Utile pour surveiller la stabilité d'une connexion en temps réel."
                },
                {
                    "command": "ping -c 4 google.com",
                    "description": "Envoie exactement 4 paquets ICMP puis s'arrête. Le flag -c (count) limite le nombre de paquets. Équivalent du comportement par défaut de Windows. Utile dans les scripts pour éviter des pings infinis."
                },
                {
                    "command": "ping -c 10 -i 0.5 192.168.1.1",
                    "description": "Envoie 10 paquets avec un intervalle de 0.5 secondes entre chaque. Le flag -i (interval) contrôle le délai. Par défaut : 1 seconde. Utile pour des tests plus rapides ou pour augmenter la fréquence lors du diagnostic de problèmes intermittents."
                },
                {
                    "command": "ping -c 100 -s 1024 server01",
                    "description": "Test avec des paquets de 1024 octets (au lieu des 64 par défaut). Le flag -s (size) modifie la taille des paquets. Utile pour tester la MTU (Maximum Transmission Unit) et détecter les problèmes de fragmentation sur le réseau."
                },
                {
                    "command": "ping -W 2 -c 4 192.168.1.100",
                    "description": "Ping avec un timeout de 2 secondes par paquet. Le flag -W (timeout) définit le délai d'attente maximum. Utile pour détecter rapidement les hôtes down sans attendre le timeout par défaut (peut être très long)."
                },
                {
                    "command": "ping -q -c 10 google.com",
                    "description": "Mode quiet : n'affiche que les statistiques finales (pas chaque paquet). Le flag -q (quiet) réduit l'output. Utile dans les scripts pour ne récupérer que le résumé (paquets perdus, temps min/max/moy)."
                },
                {
                    "command": "ping -f google.com",
                    "description": "Flood ping : envoie des paquets aussi vite que possible (nécessite root). Le flag -f (flood) ne laisse aucun délai entre les paquets. ATTENTION : à utiliser uniquement pour les tests de performance/stress sur VOS propres serveurs. Peut être considéré comme une attaque DoS sur des systèmes tiers."
                }
            ]

        # Linux: dig
        elif action.get('platform') == 1 and ('dig' in action_id.lower() or 'dig' in cmd_pattern):
            examples = [
                {
                    "command": "dig google.com",
                    "description": "Résout google.com et affiche des détails complets de la requête DNS. dig (Domain Information Groper) est l'outil de diagnostic DNS de référence sous Linux. Retourne les enregistrements A (IPv4), le temps de requête, le serveur DNS utilisé, les flags de réponse, etc. Plus verbeux et détaillé que nslookup."
                },
                {
                    "command": "dig google.com +short",
                    "description": "Résolution DNS minimaliste : affiche uniquement la/les adresse(s) IP. Le flag +short supprime tous les détails et retourne juste les réponses. Parfait pour les scripts : IP=$(dig google.com +short). Format idéal quand vous ne voulez que l'IP sans le bruit."
                },
                {
                    "command": "dig google.com MX",
                    "description": "Interroge les enregistrements MX (Mail eXchange) de google.com. Les MX indiquent les serveurs mail responsables du domaine avec leurs priorités. Essentiel pour diagnostiquer les problèmes d'email, vérifier les configurations SMTP et analyser les infrastructures mail."
                },
                {
                    "command": "dig google.com ANY",
                    "description": "Demande TOUS les types d'enregistrements DNS disponibles (A, AAAA, MX, TXT, NS, SOA, etc.). Le type ANY retourne absolument tout ce que le serveur DNS veut bien divulguer. ATTENTION : certains serveurs DNS publics ignorent les requêtes ANY pour limiter les attaques d'amplification."
                },
                {
                    "command": "dig @8.8.8.8 contoso.com",
                    "description": "Interroge un serveur DNS spécifique (Google DNS 8.8.8.8) au lieu du DNS local. La syntaxe @serveur force l'utilisation d'un résolveur particulier. Utile pour comparer les réponses entre différents DNS, vérifier la propagation ou contourner un DNS local défaillant/filtrant."
                },
                {
                    "command": "dig contoso.com NS +short",
                    "description": "Affiche uniquement les serveurs DNS autoritaires (Name Servers) du domaine. Les enregistrements NS identifient où est hébergée la zone DNS. +short donne un output propre. Essentiel lors de migrations DNS, pour identifier l'hébergeur ou vérifier les délégations."
                },
                {
                    "command": "dig -x 8.8.8.8",
                    "description": "Résolution DNS inverse (reverse lookup) : trouve le nom de domaine associé à l'IP 8.8.8.8. Le flag -x active le mode reverse. Utilise les enregistrements PTR. Utile pour identifier un serveur à partir de son IP ou vérifier la cohérence des configurations DNS forward/reverse."
                },
                {
                    "command": "dig google.com +trace",
                    "description": "Trace complète de la résolution DNS depuis les root servers. Le flag +trace affiche chaque étape de la récursion DNS : root → TLD (.com) → domaine autoritaire. Extrêmement utile pour diagnostiquer les problèmes de délégation DNS, comprendre la hiérarchie DNS et identifier où une résolution échoue."
                }
            ]

        # Si aucun cas spécifique, utiliser les exemples existants ou en créer de génériques
        if not examples:
            examples = action.get('examples', [])
            if len(examples) < 3:
                examples = self.enrich_generic_network_examples(action)

        return examples if examples else action.get('examples', [])

    def enrich_generic_network_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Génère des exemples génériques pour les commandes réseau"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_monitoring_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes de monitoring et logs"""
        action_id = action.get('id', '')
        cmd_template = action.get('windowsCommandTemplate', {}) or action.get('linuxCommandTemplate', {})
        cmd_pattern = cmd_template.get('commandPattern', '')

        examples = []

        # Get-EventLog
        if 'event' in action_id.lower() and 'Get-EventLog' in cmd_pattern:
            examples = [
                {
                    "command": "Get-EventLog -LogName System -Newest 50",
                    "description": "Affiche les 50 événements les plus récents du journal Système. -LogName spécifie le journal (System/Application/Security). -Newest limite le nombre de résultats. Les journaux System contiennent les événements hardware, drivers, services Windows, démarrage/arrêt. Première commande à utiliser pour diagnostiquer les problèmes système."
                },
                {
                    "command": "Get-EventLog -LogName Application -EntryType Error -Newest 100",
                    "description": "Affiche les 100 dernières erreurs du journal Application. -EntryType filtre par niveau (Error/Warning/Information/SuccessAudit/FailureAudit). Le journal Application contient les événements des logiciels tiers et applications Microsoft. Essentiel pour le troubleshooting applicatif."
                },
                {
                    "command": "Get-EventLog -LogName System -After (Get-Date).AddHours(-24) -EntryType Error,Warning",
                    "description": "Affiche toutes les erreurs ET warnings système des dernières 24 heures. -After filtre par date de début. Get-Date).AddHours(-24) calcule l'heure d'il y a 24h. -EntryType accepte plusieurs valeurs. Idéal pour les revues quotidiennes et l'identification proactive de problèmes."
                },
                {
                    "command": "Get-EventLog -LogName System -Source 'Service Control Manager' -Newest 50",
                    "description": "Affiche les événements provenant d'une source spécifique (ici le gestionnaire de services). -Source filtre par composant émetteur. 'Service Control Manager' enregistre tous les démarrages/arrêts/échecs de services. Parfait pour diagnostiquer les problèmes de services Windows."
                },
                {
                    "command": "Get-EventLog -LogName Application -InstanceId 1000,1001 | Select-Object TimeGenerated,Source,Message",
                    "description": "Filtre les événements par ID spécifiques. -InstanceId (ou -EventID) cible des événements précis. Select-Object affiche les colonnes pertinentes. Utilisez cette méthode quand vous connaissez les IDs d'événements problématiques (ex: 1000 = crash application)."
                },
                {
                    "command": "Get-EventLog -LogName Security -InstanceId 4625 -Newest 20",
                    "description": "Affiche les 20 dernières tentatives de connexion échouées (Event ID 4625). Le journal Security enregistre tous les événements de sécurité (connexions, élévation de privilèges, accès aux objets). L'ID 4625 indique un échec d'authentification. Crucial pour détecter les tentatives d'intrusion ou les problèmes de mot de passe."
                },
                {
                    "command": "Get-EventLog -LogName System -After (Get-Date).AddDays(-7) | Where-Object {$_.EventID -eq 6008} | Format-Table TimeGenerated,Message -AutoSize",
                    "description": "Identifie tous les arrêts inattendus (crashs, coupures électriques) de la semaine. L'Event ID 6008 signale 'The previous system shutdown was unexpected'. Format-Table affiche l'heure et le message. Essentiel pour diagnostiquer les instabilités système et planifier les maintenances."
                },
                {
                    "command": "Get-EventLog -LogName Application -EntryType Error | Group-Object Source | Sort-Object Count -Descending | Select-Object Count,Name -First 10",
                    "description": "Analyse statistique : identifie les 10 sources d'erreurs les plus fréquentes dans Application. Group-Object regroupe par source. Sort-Object trie par nombre d'occurrences. Excellent pour identifier les applications problématiques et prioriser les corrections."
                },
                {
                    "command": "Get-EventLog -LogName System -After (Get-Date).AddDays(-1) -EntryType Error | Export-Csv C:\\Temp\\SystemErrors.csv -NoTypeInformation",
                    "description": "Exporte toutes les erreurs système des dernières 24h vers CSV. Utile pour archivage, analyse dans Excel, partage avec le support technique ou création de rapports. Les exports réguliers permettent de suivre l'évolution des problèmes dans le temps."
                }
            ]

        # Get-WinEvent (moderne)
        elif 'event' in action_id.lower() and 'Get-WinEvent' in cmd_pattern:
            examples = [
                {
                    "command": "Get-WinEvent -LogName System -MaxEvents 100",
                    "description": "Affiche les 100 derniers événements du journal Système. Get-WinEvent est la cmdlet moderne (remplace Get-EventLog). -MaxEvents limite les résultats. Supporte les journaux classiques ET les nouveaux journaux Applications and Services. Plus rapide et plus flexible que Get-EventLog."
                },
                {
                    "command": "Get-WinEvent -LogName Application -FilterHashtable @{Level=2; StartTime=(Get-Date).AddHours(-24)}",
                    "description": "Filtre les erreurs (Level=2) des dernières 24h avec FilterHashtable. -FilterHashtable utilise une table de hachage pour des filtres complexes côté serveur (plus rapide que Where-Object). Levels : 1=Critical, 2=Error, 3=Warning, 4=Information. Le filtrage côté serveur peut être 10x plus rapide sur de gros journaux."
                },
                {
                    "command": "Get-WinEvent -LogName Security -FilterHashtable @{ID=4624; StartTime=(Get-Date).AddDays(-1)} | Select-Object TimeCreated,Message -First 50",
                    "description": "Affiche les 50 premières connexions réussies (ID 4624) des dernières 24h. L'Event ID 4624 = authentification réussie. FilterHashtable filtre efficacement par ID et date. Utile pour auditer les connexions, identifier les patterns d'utilisation et détecter les connexions suspectes."
                },
                {
                    "command": "Get-WinEvent -ListLog *",
                    "description": "Liste TOUS les journaux d'événements disponibles sur le système. Retourne le nom, la taille max, le nombre d'enregistrements et le statut (activé/désactivé). Il peut y avoir des centaines de journaux (System, Application, Security + tous les journaux Applications and Services). Utilisez cette commande pour découvrir les journaux disponibles."
                },
                {
                    "command": "Get-WinEvent -LogName 'Microsoft-Windows-PowerShell/Operational' -MaxEvents 20",
                    "description": "Affiche les événements du journal PowerShell. Les journaux Applications and Services ont des noms avec chemins (slashes). Ce journal enregistre toutes les activités PowerShell : commandes exécutées, erreurs, scripts. Essentiel pour l'audit de sécurité et le troubleshooting PowerShell."
                },
                {
                    "command": "Get-WinEvent -LogName System -FilterHashtable @{ProviderName='Service Control Manager'; ID=7036}",
                    "description": "Filtre par fournisseur (source) et ID d'événement. ProviderName = composant émetteur. L'ID 7036 = changement d'état de service (démarré/arrêté). Retourne l'historique complet des démarrages/arrêts de services. Utile pour diagnostiquer les services qui crashent ou s'arrêtent."
                },
                {
                    "command": "$xml = '<QueryList><Query><Select Path=\"System\">*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) &lt;= 86400000]]]</Select></Query></QueryList>'; Get-WinEvent -FilterXml $xml",
                    "description": "Filtre avancé avec XPath/XML pour les requêtes complexes. FilterXml permet des filtres impossibles avec FilterHashtable (conditions multiples, OU logiques complexes, filtres sur le contenu du message). Cet exemple récupère Critical OU Error des dernières 24h. Courbe d'apprentissage élevée mais très puissant."
                },
                {
                    "command": "Get-WinEvent -LogName Application -FilterHashtable @{Level=2} | Group-Object ProviderName | Sort-Object Count -Descending | Select-Object Count,Name -First 10",
                    "description": "Analyse statistique : top 10 des sources d'erreurs dans Application. Group-Object agrège par fournisseur. Sort trie par fréquence. Identifie rapidement les applications les plus problématiques pour prioriser les actions de correction."
                }
            ]

        # Get-Process
        elif 'process' in action_id.lower() and 'Get-Process' in cmd_pattern:
            examples = [
                {
                    "command": "Get-Process",
                    "description": "Liste tous les processus en cours d'exécution sur la machine. Retourne le nom, l'ID (PID), l'utilisation CPU, la mémoire (WorkingSet) et le nom de l'exécutable. Équivalent PowerShell du Gestionnaire des tâches. Utilisez pour avoir un aperçu rapide de l'activité système."
                },
                {
                    "command": "Get-Process | Sort-Object CPU -Descending | Select-Object ProcessName,Id,CPU,WorkingSet -First 10",
                    "description": "Affiche les 10 processus consommant le plus de CPU. Sort-Object -Descending trie du plus gourmand au moins. WorkingSet = mémoire utilisée en octets. Select-Object formate proprement. Première commande pour diagnostiquer une lenteur système : identifiez les processus qui monopolisent le CPU."
                },
                {
                    "command": "Get-Process | Sort-Object WS -Descending | Select-Object ProcessName,Id,@{Name='Memory(MB)';Expression={[math]::Round($_.WS/1MB,2)}} -First 10",
                    "description": "Affiche les 10 processus consommant le plus de mémoire (RAM). WS = WorkingSet en octets. Expression calculée convertit les octets en Mo avec 2 décimales. Essentiel pour diagnostiquer les fuites mémoire, identifier les processus gourmands et résoudre les problèmes de performance."
                },
                {
                    "command": "Get-Process -Name chrome,firefox,msedge",
                    "description": "Affiche uniquement les processus des navigateurs web. -Name accepte plusieurs valeurs séparées par virgules. Retourne tous les processus correspondants (il peut y avoir des dizaines d'instances de chrome). Utile pour surveiller ou tuer des applications spécifiques."
                },
                {
                    "command": "Get-Process -Id 1234",
                    "description": "Affiche les détails d'un processus spécifique par son PID (Process ID). Utilisez cette méthode quand vous connaissez le PID exact (visible dans le Gestionnaire des tâches ou retourné par une autre commande). Parfait pour inspecter un processus problématique identifié."
                },
                {
                    "command": "Get-Process | Where-Object {$_.CPU -gt 10} | Select-Object ProcessName,Id,CPU",
                    "description": "Filtre les processus utilisant plus de 10 secondes de CPU. Where-Object filtre selon une condition. -gt = greater than (supérieur à). Utile pour identifier les processus qui ont consommé beaucoup de CPU dans le passé (pas forcément en temps réel)."
                },
                {
                    "command": "Get-Process | Measure-Object WorkingSet -Sum | Select-Object @{Name='TotalMemory(GB)';Expression={[math]::Round($_.Sum/1GB,2)}}",
                    "description": "Calcule la mémoire TOTALE utilisée par tous les processus. Measure-Object -Sum additionne toutes les valeurs WorkingSet. Expression convertit en Go. Donne une vue d'ensemble de la consommation mémoire du système. Comparez avec la RAM physique pour évaluer la pression mémoire."
                },
                {
                    "command": "Get-Process powershell,pwsh | Select-Object ProcessName,Id,StartTime,@{Name='Runtime';Expression={(Get-Date) - $_.StartTime}}",
                    "description": "Affiche tous les processus PowerShell avec leur durée d'exécution (uptime). StartTime = heure de démarrage du processus. Expression calculée soustrait StartTime de maintenant. Utile pour identifier les sessions PowerShell oubliées ou les scripts qui tournent trop longtemps."
                },
                {
                    "command": "Get-Process | Export-Csv C:\\Temp\\Processes.csv -NoTypeInformation",
                    "description": "Exporte l'état actuel de tous les processus vers CSV. Utile pour documenter l'état d'un système à un instant T, comparer avant/après une modification, ou archiver pour analyse ultérieure. Créez des snapshots réguliers pour détecter les changements anormaux."
                }
            ]

        # Linux: journalctl
        elif action.get('platform') == 1 and 'journalctl' in cmd_pattern:
            examples = [
                {
                    "command": "journalctl -n 50",
                    "description": "Affiche les 50 derniers messages du journal systemd. Le flag -n (lines) limite l'output. journalctl est le système de logs central de systemd qui agrège tous les journaux (kernel, services, applications). Première commande pour diagnostiquer n'importe quel problème sous Linux moderne."
                },
                {
                    "command": "journalctl -f",
                    "description": "Mode suivi en temps réel (follow). Le flag -f affiche les nouveaux messages au fur et à mesure. Équivalent de 'tail -f' mais pour tous les logs systemd. Essentiel pour observer en direct l'activité système, surveiller un déploiement ou debugger un service problématique. Arrêtez avec Ctrl+C."
                },
                {
                    "command": "journalctl -p err",
                    "description": "Affiche uniquement les messages d'erreur. Le flag -p (priority) filtre par niveau de sévérité. Niveaux : emerg, alert, crit, err, warning, notice, info, debug. Utilisez -p err pour se concentrer sur les vrais problèmes sans le bruit des infos/warnings."
                },
                {
                    "command": "journalctl -u nginx.service",
                    "description": "Affiche tous les logs d'un service spécifique (ici nginx). Le flag -u (unit) filtre par unité systemd. Équivalent de consulter /var/log/nginx/* mais via journalctl. Toutes les sorties stdout/stderr du service sont capturées. Parfait pour le troubleshooting service par service."
                },
                {
                    "command": "journalctl --since \"2024-01-15 10:00:00\" --until \"2024-01-15 11:00:00\"",
                    "description": "Affiche les logs d'une plage horaire précise. Les flags --since et --until acceptent des dates absolues. Format : 'YYYY-MM-DD HH:MM:SS'. Indispensable pour investiguer un incident survenu à un moment précis. Vous pouvez aussi utiliser des dates relatives : --since \"1 hour ago\"."
                },
                {
                    "command": "journalctl --since today -p err",
                    "description": "Affiche toutes les erreurs depuis minuit (aujourd'hui). --since today est un raccourci pour depuis 00:00. Combiné avec -p err pour ne voir que les erreurs. Excellente commande pour la revue quotidienne des problèmes : à exécuter chaque matin pour identifier les incidents de la nuit."
                },
                {
                    "command": "journalctl -u ssh.service --since \"10 minutes ago\"",
                    "description": "Affiche les logs SSH des 10 dernières minutes. --since accepte des durées relatives (minutes, hours, days ago). Utile pour diagnostiquer un problème de connexion SSH juste après qu'il se soit produit. Adaptez le service et la durée selon vos besoins."
                },
                {
                    "command": "journalctl -k -n 100",
                    "description": "Affiche les 100 derniers messages du kernel. Le flag -k (kernel) filtre uniquement les messages noyau (équivalent de dmesg). Essentiel pour diagnostiquer les problèmes hardware, drivers, panics kernel, erreurs disque/réseau de bas niveau."
                },
                {
                    "command": "journalctl -b -1",
                    "description": "Affiche tous les logs du boot PRÉCÉDENT. Le flag -b (boot) filtre par session de démarrage. -b sans argument = boot actuel. -b -1 = boot précédent. -b -2 = avant-avant-dernier. Crucial pour diagnostiquer pourquoi un système a crashé ou n'a pas démarré correctement : après reboot, consultez les logs du boot raté."
                },
                {
                    "command": "journalctl --disk-usage",
                    "description": "Affiche l'espace disque utilisé par les journaux systemd. Les journaux peuvent occuper plusieurs Go sur des systèmes actifs. Utilisez cette commande pour surveiller l'espace et décider si un nettoyage est nécessaire (voir journalctl --vacuum-size ou --vacuum-time)."
                }
            ]

        # Linux: top / htop
        elif action.get('platform') == 1 and ('top' in action_id.lower() or 'top' in cmd_pattern):
            examples = [
                {
                    "command": "top",
                    "description": "Affiche en temps réel l'utilisation CPU, mémoire et la liste des processus actifs. top est l'outil de monitoring système interactif de référence sous Linux. Rafraîchit automatiquement (par défaut toutes les 3 secondes). Appuyez sur 'q' pour quitter. Première commande pour diagnostiquer les problèmes de performance."
                },
                {
                    "command": "top -u www-data",
                    "description": "Affiche uniquement les processus de l'utilisateur www-data (utilisateur des serveurs web Apache/nginx). Le flag -u (user) filtre par propriétaire. Utile pour surveiller la consommation d'un service spécifique ou d'un utilisateur. Remplacez www-data par n'importe quel username."
                },
                {
                    "command": "top -bn1 | head -20",
                    "description": "Mode batch : exécute top une seule fois et affiche le résultat. -b = batch mode (output texte au lieu d'interactif). -n1 = une seule itération. head -20 limite aux 20 premières lignes. Parfait pour les scripts, les logs ou la capture de l'état système à un instant T."
                },
                {
                    "command": "top -o %CPU",
                    "description": "Trie les processus par utilisation CPU décroissante (macOS/BSD). Le flag -o (order by) spécifie la colonne de tri. %CPU trie du plus gourmand au moins. Sous Linux, appuyez sur 'Shift+P' dans top interactif pour le même effet. Identifie instantanément le processus qui consomme le plus de CPU."
                },
                {
                    "command": "top -bn1 -o +%MEM | head -20",
                    "description": "Affiche les processus triés par utilisation mémoire (Linux). -o +%MEM trie par mémoire décroissante. head -20 limite l'affichage. Parfait pour identifier rapidement les processus gourmands en RAM lors d'un problème de mémoire."
                }
            ]

        # Si aucun cas spécifique
        if not examples:
            examples = action.get('examples', [])
            if len(examples) < 3:
                examples = self.enrich_generic_monitoring_examples(action)

        return examples if examples else action.get('examples', [])

    def enrich_generic_monitoring_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Génère des exemples génériques pour les commandes de monitoring"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_windows_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes Windows"""
        # Pour les commandes Windows générales, retourner les exemples existants enrichis
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_linux_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes Linux"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_performance_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes de performance"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_gpo_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes GPO"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_security_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes de sécurité"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_git_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes Git"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_storage_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les commandes de stockage"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []

    def enrich_generic_examples(self, action: Dict[str, Any]) -> List[Dict[str, str]]:
        """Enrichit les exemples pour les autres catégories"""
        current_examples = action.get('examples', [])
        return deepcopy(current_examples) if current_examples else []


def main():
    """Fonction principale"""
    import sys

    input_file = 'data/seed/initial-actions.json'
    output_file = 'data/seed/initial-actions-enriched.json'

    print("=" * 70)
    print("Script d'enrichissement des exemples TwinShell")
    print("=" * 70)
    print()

    # Charger le fichier
    print(f"📖 Chargement de {input_file}...")
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        print(f"✅ Fichier chargé : {len(data.get('actions', []))} actions trouvées")
    except Exception as e:
        print(f"❌ Erreur de chargement : {e}")
        sys.exit(1)

    print()

    # Enrichir
    enricher = ExampleEnricher(data)
    enriched_data = enricher.enrich_all()

    print()

    # Sauvegarder
    print(f"💾 Sauvegarde vers {output_file}...")
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(enriched_data, f, ensure_ascii=False, indent=2)
        print(f"✅ Fichier sauvegardé avec succès!")
    except Exception as e:
        print(f"❌ Erreur de sauvegarde : {e}")
        sys.exit(1)

    print()
    print("=" * 70)
    print("✨ Enrichissement terminé avec succès!")
    print("=" * 70)


if __name__ == '__main__':
    main()
