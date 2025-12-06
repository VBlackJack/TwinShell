import json
import shutil
from pathlib import Path

# Configuration
INPUT_FILE = 'data/seed/initial-actions.json'
OUTPUT_FILE = 'data/seed/initial-actions.json' # On écrase directement pour appliquer les changements
BACKUP_FILE = 'data/seed/initial-actions.json.bak'

# ==============================================================================
# BASE DE CONNAISSANCE "URGENCE & EXPERT"
# ==============================================================================
# Cette structure définit les scénarios précis qui manquent pour l'usage pro/urgence.
# Clé = ID de l'action ou Nom de la commande (si ID introuvable)
# Valeur = Liste d'exemples complets

EMERGENCY_KITS = {
    # --------------------------------------------------------------------------
    # 1. RESEAU (Network)
    # --------------------------------------------------------------------------
    "Test-NetConnection": [
        {
            "command": "Test-NetConnection -ComputerName <Host> -Port 443",
            "description": "🔥 DIAGNOSTIC EXPRESS : Vérifie si un port spécifique (ex: HTTPS) est ouvert. Indispensable pour distinguer un problème de service arrêté d'un problème de pare-feu.",
            "platform": 0
        },
        {
            "command": "Test-NetConnection -ComputerName <Host> -TraceRoute",
            "description": "Diagnostique la route réseau (Hops). Permet d'identifier à quel routeur/firewall les paquets sont perdus.",
            "platform": 0
        },
        {
            "command": "Test-NetConnection -ComputerName <Host> -InformationLevel Detailed",
            "description": "Affiche des détails techniques complets (résolution DNS, adresse source, interface de sortie).",
            "platform": 0
        }
    ],
    "netstat": [
        {
            "command": "netstat -anob",
            "description": "🔥 URGENCE : Affiche TOUS les ports écoutés avec le nom de l'exécutable (-b) et le PID (-o). Le flag -n (numérique) évite les lenteurs de résolution DNS. Nécessite des droits Admin.",
            "platform": 0
        },
        {
            "command": "netstat -an | findstr \"ESTABLISHED\"",
            "description": "Filtre uniquement les connexions actives établies. Utile pour repérer une exfiltration de données ou une activité suspecte en cours.",
            "platform": 0
        }
    ],
    "ss": [ # L'équivalent moderne de netstat sous Linux
        {
            "command": "ss -tulpn",
            "description": "🔥 STANDARD LINUX : Liste tous les ports TCP/UDP en écoute (-tul) avec les processus associés (-p) et sans résolution DNS (-n). La commande de référence pour savoir 'qui écoute sur ce port'.",
            "platform": 1
        },
        {
            "command": "ss -s",
            "description": "Résumé statistique rapide des sockets (Total, TCP, UDP). Utile pour détecter une saturation de sockets ou une attaque DDoS.",
            "platform": 1
        }
    ],
    "tcpdump": [
        {
            "command": "tcpdump -i any port 80 -A -n",
            "description": "🔥 DEBUG APP : Capture le trafic HTTP et affiche le contenu en ASCII (-A). Le flag -n est CRITIQUE pour la vitesse (pas de DNS). Permet de lire les headers HTTP en temps réel.",
            "platform": 1
        },
        {
            "command": "tcpdump -i eth0 host 192.168.1.50 -w capture.pcap",
            "description": "Capture tout le trafic d'une IP cible vers un fichier (-w) pour analyse ultérieure sur Wireshark. Ne sature pas la console.",
            "platform": 1
        },
        {
            "command": "tcpdump -i eth0 dst port 53",
            "description": "Audit DNS : Surveille uniquement les requêtes DNS sortantes. Utile pour diagnostiquer des problèmes de résolution ou repérer des malwares communiquant par DNS.",
            "platform": 1
        }
    ],
    "Resolve-DnsName": [
        {
            "command": "Resolve-DnsName -Name <Domain> -Server 8.8.8.8",
            "description": "Force la résolution via un serveur externe (Google). Permet de savoir si le problème vient de votre DNS interne ou de la propagation mondiale.",
            "platform": 0
        },
        {
            "command": "Resolve-DnsName -Name <Domain> -Type TXT",
            "description": "Vérifie les enregistrements TXT (souvent utilisés pour SPF, DKIM, validation de domaine).",
            "platform": 0
        }
    ],

    # --------------------------------------------------------------------------
    # 2. LOGS & PROCESSUS (Logs & Process)
    # --------------------------------------------------------------------------
    "Get-EventLog": [
        {
            "command": "Get-EventLog -LogName System -EntryType Error -After (Get-Date).AddHours(-1)",
            "description": "🔥 CRITIQUE : Affiche uniquement les ERREURS système de la DERNIÈRE HEURE. C'est la première commande à lancer en cas de plantage serveur.",
            "platform": 0
        },
        {
            "command": "Get-EventLog -LogName Security -InstanceId 4625 -Newest 20",
            "description": "Sécurité : Affiche les 20 dernières tentatives de connexion échouées (Event ID 4625). Indique une potentielle attaque brute-force ou un compte de service verrouillé.",
            "platform": 0
        }
    ],
    "Get-WinEvent": [ # Plus moderne que Get-EventLog
         {
            "command": "Get-WinEvent -FilterHashtable @{LogName='System'; Level=2; StartTime=(Get-Date).AddHours(-24)}",
            "description": "Méthode performante pour filtrer les erreurs (Level=2) des dernières 24h. Beaucoup plus rapide que Get-EventLog sur les gros volumes.",
            "platform": 0
        }
    ],
    "journalctl": [
        {
            "command": "journalctl -xe",
            "description": "🔥 STANDARD : Affiche les logs système les plus récents avec explications étendues. La commande réflexe quand un service Linux refuse de démarrer.",
            "platform": 1
        },
        {
            "command": "journalctl -u nginx -f",
            "description": "Live Tail : Suit en temps réel (-f) les logs d'un service spécifique (ex: nginx, apache2, sshd).",
            "platform": 1
        },
        {
            "command": "journalctl -p 3 -xb",
            "description": "Affiche uniquement les Erreurs (p 3) survenues depuis le dernier démarrage (boot).",
            "platform": 1
        }
    ],
    "Get-Process": [
        {
            "command": "Get-Process | Sort-Object CPU -Descending | Select-Object -First 5",
            "description": "🔥 PERFORMANCE : Identifie les 5 processus qui consomment le plus de CPU actuellement.",
            "platform": 0
        },
        {
            "command": "Get-Process | Where-Object {$_.Responding -eq $false}",
            "description": "Trouve les applications 'Ne répond pas' (Frozen).",
            "platform": 0
        },
        {
            "command": "Get-Process -Name chrome | Stop-Process -Force",
            "description": "Kill Switch : Ferme de force toutes les instances d'un processus spécifique. Utile pour nettoyer des processus zombies.",
            "platform": 0
        }
    ],
    "ps": [
        {
            "command": "ps aux --sort=-%mem | head -n 10",
            "description": "🔥 MEMOIRE : Affiche les 10 processus les plus gourmands en RAM. Indispensable pour diagnostiquer les OOM (Out Of Memory).",
            "platform": 1
        },
        {
            "command": "ps -ef | grep <name>",
            "description": "Recherche classique d'un processus par son nom.",
            "platform": 1
        }
    ]
}

def clean_robotic_examples(examples):
    """Supprime les exemples générés automatiquement qui sont trop verbeux/inutiles."""
    clean_list = []
    for ex in examples:
        desc = ex.get('description', '')
        # On garde l'exemple sauf s'il ressemble trop à un template de remplissage bête
        if "Remplacez 100 par la valeur appropriée" in desc:
            continue
        if "Exemple utilisant le paramètre" in desc and "selon votre environnement" in desc:
            continue
        clean_list.append(ex)
    return clean_list

def enrich_database():
    print(f"Chargement de {INPUT_FILE}...")
    with open(INPUT_FILE, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    actions = data.get('actions', [])
    updates_count = 0
    
    print("Application des kits d'urgence...")
    
    for action in actions:
        # On doit vérifier les DEUX templates (Windows et Linux) car une action peut être hybride
        # et avoir besoin d'enrichissement sur les deux volets, ou l'un des deux.
        
        win_tpl = action.get('windowsCommandTemplate')
        linux_tpl = action.get('linuxCommandTemplate')
        
        # Liste des kits à appliquer pour cette action
        kits_to_apply = []
        
        # Vérification Windows
        if win_tpl:
            win_name = win_tpl.get('name')
            if win_name in EMERGENCY_KITS:
                kits_to_apply.append((win_name, 0)) # 0 = Windows
            elif action.get('id') in EMERGENCY_KITS:
                 kits_to_apply.append((action.get('id'), 0))

        # Vérification Linux
        if linux_tpl:
            linux_name = linux_tpl.get('name')
            if linux_name in EMERGENCY_KITS:
                kits_to_apply.append((linux_name, 1)) # 1 = Linux
            elif action.get('id') in EMERGENCY_KITS:
                 # Eviter doublon si déjà ajouté via ID pour Windows, mais souvent l'ID est unique
                 # Si l'ID est générique (ex: "check-ports"), on peut vouloir ajouter les exemples Linux aussi
                 pass 

        # Si on n'a rien trouvé via les noms exacts, on tente le fallback ID ou Pattern
        if not kits_to_apply:
            # Recherche par ID global
            if action.get('id') in EMERGENCY_KITS:
                # On ne sait pas quelle plateforme, on suppose celle de l'action par défaut
                # Mais attention, nos kits ont souvent une plateforme définie dans l'exemple
                # Pour simplifier, on prend le kit tel quel
                kits_to_apply.append((action.get('id'), action.get('platform', 0)))
            else:
                 # Recherche pattern
                 patterns = []
                 if win_tpl: patterns.append(win_tpl.get('commandPattern', ''))
                 if linux_tpl: patterns.append(linux_tpl.get('commandPattern', ''))
                 
                 for pat in patterns:
                     for key in EMERGENCY_KITS.keys():
                         if key in pat:
                             # On a trouvé un mot clé (ex: tcpdump) dans le pattern
                             # On détermine la plateforme selon le pattern
                             plat = 0 if win_tpl and key in win_tpl.get('commandPattern', '') else 1
                             if (key, plat) not in kits_to_apply:
                                 kits_to_apply.append((key, plat))

        if kits_to_apply:
            print(f"  -> Enrichissement de {action.get('id')} avec {kits_to_apply}")
            
            current_examples = clean_robotic_examples(action.get('examples', []))
            existing_cmds = {ex['command'] for ex in current_examples}
            
            to_add = []
            
            for kit_key, platform_override in kits_to_apply:
                expert_examples = EMERGENCY_KITS.get(kit_key, [])
                
                for exp in expert_examples:
                    if exp['command'] not in existing_cmds:
                        exp_copy = exp.copy()
                        # Si le kit ne spécifie pas de plateforme, on utilise celle détectée
                        # Mais nos kits EMERGENCY_KITS ont déjà la clé "platform" définie correctement.
                        # On ne force la plateforme que si elle manque dans le kit.
                        if 'platform' not in exp_copy:
                            exp_copy['platform'] = platform_override
                        
                        to_add.append(exp_copy)
                        existing_cmds.add(exp['command']) # Eviter doublons intra-ajout

            # Fusion : Experts d'abord
            action['examples'] = to_add + current_examples
            updates_count += 1

    print(f"Sauvegarde... ({updates_count} actions mises à jour)")
    
    # Backup
    shutil.copy(INPUT_FILE, BACKUP_FILE)
    
    with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print("Terminé.")

if __name__ == '__main__':
    enrich_database()
