# Module 4 : Réseau & Latence

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Distinguer bande passante et latence
- :material-check: Surveiller le trafic réseau en temps réel
- :material-check: Investiguer les sockets et connexions
- :material-check: Diagnostiquer la perte de paquets et les problèmes de routage

---

## 1. Bande Passante vs Latence

### 1.1 Deux Concepts Différents

!!! info "La Confusion Courante"
    "J'ai une connexion 1 Gbps, pourquoi mon application est lente ?"

    Parce que **bande passante ≠ latence**. Ce sont deux métriques indépendantes.

```
┌─────────────────────────────────────────────────────────────────┐
│              BANDE PASSANTE vs LATENCE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   BANDE PASSANTE (Throughput)                                   │
│   ───────────────────────────                                   │
│   • Quantité de données pouvant transiter par unité de temps    │
│   • Mesurée en Mbps, Gbps                                       │
│   • Analogie : Largeur d'un tuyau                               │
│   • Impact : Transferts de fichiers, streaming                  │
│                                                                 │
│   LATENCE (RTT - Round Trip Time)                               │
│   ───────────────────────────────                               │
│   • Temps pour qu'un paquet aille et revienne                   │
│   • Mesurée en millisecondes (ms)                               │
│   • Analogie : Longueur du tuyau                                │
│   • Impact : Réactivité, applications interactives              │
│                                                                 │
│   EXEMPLE :                                                     │
│   ┌──────────────────────────────────────────────────────┐      │
│   │ Paris → New York                                     │      │
│   │ Bande passante : 10 Gbps (énorme)                    │      │
│   │ Latence : 80 ms (incompressible - vitesse lumière)   │      │
│   │                                                      │      │
│   │ Une requête HTTP simple :                            │      │
│   │ • DNS lookup : 80 ms                                 │      │
│   │ • TCP handshake : 80 ms                              │      │
│   │ • TLS handshake : 160 ms (2 RTT)                     │      │
│   │ • Requête/réponse : 80 ms                            │      │
│   │ • TOTAL : ~400 ms minimum !                          │      │
│   └──────────────────────────────────────────────────────┘      │
│                                                                 │
│   ⚠️ La latence ne peut pas être "fixée" par plus de bande      │
│      passante. C'est de la physique.                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Impact sur les Applications

| Type d'Application | Métrique Critique | Pourquoi |
|-------------------|-------------------|----------|
| Transfert de fichiers (scp, rsync) | Bande passante | Volume de données |
| Base de données distante | Latence | Nombreuses requêtes courtes |
| API REST | Latence | Requête/réponse rapides |
| Streaming vidéo | Bande passante | Flux continu |
| VoIP / Visio | Latence + Jitter | Temps réel |
| SSH interactif | Latence | Chaque frappe = 1 RTT |

---

## 2. Surveillance du Trafic : iftop & nload

### 2.1 iftop : Vue par Connexion

```bash
# Installation
# Debian/Ubuntu: apt install iftop
# RHEL/Rocky: dnf install iftop

# Lancer sur l'interface principale
sudo iftop -i eth0

# Sans résolution DNS (plus rapide)
sudo iftop -i eth0 -n

# Filtrer par port
sudo iftop -i eth0 -f "port 443"
```

```
┌─────────────────────────────────────────────────────────────────┐
│  iftop - Trafic par Connexion                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   191Mb        381Mb       572Mb       763Mb       954Mb        │
│   └────────────┴───────────┴───────────┴───────────┴──────────  │
│                                                                 │
│   server.local     => 10.0.0.50         45.2Mb  42.1Mb  40.5Mb  │
│                    <=                   12.3Mb  11.8Mb  11.2Mb  │
│                                                                 │
│   server.local     => database.internal  8.5Mb   7.2Mb   6.8Mb  │
│                    <=                   25.4Mb  24.1Mb  23.5Mb  │
│                                                                 │
│   server.local     => 203.0.113.45       2.1Mb   1.8Mb   1.5Mb  │
│                    <=                    0.5Mb   0.4Mb   0.3Mb  │
│                                                                 │
│   ───────────────────────────────────────────────────────────   │
│   TX: cumul:  125MB   peak:  58.5Mb   rates:  55.8Mb  51.1Mb    │
│   RX:         234MB           45.2Mb          38.2Mb  35.5Mb    │
│   TOTAL:      359MB          103.7Mb          94.0Mb  86.6Mb    │
│                                                                 │
│   Touches : n=DNS  s=source  d=dest  p=pause  q=quit            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Touches Utiles :**

| Touche | Action |
|--------|--------|
| `n` | Toggle résolution DNS |
| `s` | Trier par source |
| `d` | Trier par destination |
| `t` | Cycle affichage (2-lignes, 1-ligne, 1-ligne+total) |
| `p` | Pause |
| `P` | Pause + afficher barres |

### 2.2 nload : Vue par Interface

```bash
# Installation
# apt install nload  ou  dnf install nload

# Lancer (naviguez entre interfaces avec ← →)
nload

# Interface spécifique
nload eth0

# Plusieurs interfaces
nload eth0 eth1
```

```
┌─────────────────────────────────────────────────────────────────┐
│  nload - Interface eth0                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Incoming:                                                     │
│                                    ▄▄                           │
│                                  ▄████                          │
│                                ▄██████▄                         │
│                              ▄████████████▄                     │
│                            ▄████████████████▄▄                  │
│   ─────────────────────────────────────────────                 │
│   Curr: 125.45 MBit/s                                           │
│   Avg:   98.23 MBit/s                                           │
│   Min:   12.50 MBit/s                                           │
│   Max:  187.30 MBit/s                                           │
│   Ttl:   45.23 GByte                                            │
│                                                                 │
│   Outgoing:                                                     │
│                        ▄▄▄▄                                     │
│                      ▄██████▄                                   │
│                    ▄██████████▄                                 │
│   ─────────────────────────────────────────────                 │
│   Curr:  45.67 MBit/s                                           │
│   Avg:   38.12 MBit/s                                           │
│   Min:    5.20 MBit/s                                           │
│   Max:   78.90 MBit/s                                           │
│   Ttl:   18.56 GByte                                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Alternatives : sar et nethogs

```bash
# sar pour les statistiques réseau (historique)
sar -n DEV 1 5

# Sortie :
# IFACE   rxpck/s  txpck/s  rxkB/s  txkB/s  rxcmp/s txcmp/s rxmcst/s  %ifutil
# eth0   12543.00  8765.00 15234.56  4567.89    0.00    0.00     0.00   12.35

# nethogs : trafic par processus (comme iotop pour le réseau)
sudo nethogs eth0
```

---

## 3. Investigation des Sockets : ss

### 3.1 ss remplace netstat

`ss` (Socket Statistics) est le remplaçant moderne de `netstat`. Il est plus rapide et plus complet.

```bash
# Connexions TCP établies
ss -t

# Tous les sockets (TCP, UDP, UNIX)
ss -a

# Avec les processus associés
ss -tulpn

# Explication des flags :
# -t : TCP
# -u : UDP
# -l : Listening (en écoute)
# -p : Processus (PID/nom)
# -n : Numérique (pas de résolution DNS)
```

### 3.2 Lecture de la Sortie

```bash
$ ss -tulpn
```

```
Netid  State   Recv-Q  Send-Q   Local Address:Port    Peer Address:Port  Process
tcp    LISTEN  0       128      0.0.0.0:22             0.0.0.0:*          users:(("sshd",pid=1234,fd=3))
tcp    LISTEN  0       511      0.0.0.0:80             0.0.0.0:*          users:(("nginx",pid=5678,fd=6))
tcp    LISTEN  0       128      127.0.0.1:3306         0.0.0.0:*          users:(("mysqld",pid=9012,fd=21))
tcp    ESTAB   0       0        10.0.0.10:22           10.0.0.50:54321    users:(("sshd",pid=2345,fd=4))
tcp    ESTAB   0       52       10.0.0.10:80           203.0.113.45:12345 users:(("nginx",pid=5679,fd=12))
udp    UNCONN  0       0        0.0.0.0:68             0.0.0.0:*          users:(("dhclient",pid=789,fd=6))
```

**Colonnes :**

| Colonne | Description |
|---------|-------------|
| `Netid` | Protocole (tcp, udp, unix) |
| `State` | État de la connexion |
| `Recv-Q` | Données en attente de lecture |
| `Send-Q` | Données en attente d'envoi |
| `Local Address:Port` | Adresse locale |
| `Peer Address:Port` | Adresse distante |
| `Process` | PID et nom du processus |

### 3.3 États TCP à Connaître

```
┌─────────────────────────────────────────────────────────────────┐
│              ÉTATS TCP IMPORTANTS                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   LISTEN      : En attente de connexions entrantes              │
│                 (serveur prêt)                                  │
│                                                                 │
│   ESTABLISHED : Connexion active, données échangées             │
│                 (état normal)                                   │
│                                                                 │
│   TIME_WAIT   : Connexion fermée, attente avant réutilisation   │
│                 (normal, mais beaucoup = problème)              │
│                                                                 │
│   CLOSE_WAIT  : L'autre côté a fermé, attente de notre close()  │
│                 ⚠️ Si beaucoup : bug applicatif (fd non fermé)  │
│                                                                 │
│   SYN_SENT    : Connexion sortante en cours                     │
│                 ⚠️ Si bloqué ici : firewall ou serveur down     │
│                                                                 │
│   SYN_RECV    : Connexion entrante en cours (handshake)         │
│                 ⚠️ Si beaucoup : possible SYN flood attack      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Requêtes Utiles

```bash
# Compter les connexions par état
ss -t state established | wc -l
ss -t state time-wait | wc -l
ss -t state close-wait | wc -l

# Résumé des états
ss -s

# Connexions vers un port spécifique
ss -tn dport = :443

# Connexions depuis une IP
ss -tn src 10.0.0.50

# Connexions d'un processus spécifique
ss -tp | grep nginx

# Trouver quel processus écoute sur un port
ss -tlnp | grep :80
```

---

## 4. Diagnostic de Latence : mtr

### 4.1 mtr = ping + traceroute

`mtr` (My Traceroute) combine ping et traceroute pour une analyse continue de la latence et de la perte de paquets sur chaque saut.

```bash
# Installation
# apt install mtr-tiny  ou  dnf install mtr

# Lancer vers une destination
mtr google.com

# Mode rapport (non-interactif)
mtr -r -c 100 google.com

# Sans résolution DNS
mtr -n google.com

# Avec TCP au lieu de ICMP (utile si ICMP bloqué)
mtr -T -P 443 google.com
```

### 4.2 Lecture de la Sortie

```
                             My traceroute  [v0.94]
server.local (10.0.0.10) -> google.com (142.250.179.110)     2025-01-28T14:35:00

Keys:  Help   Display mode   Restart statistics   Order of fields   quit
                                       Packets               Pings
 Host                                Loss%   Snt   Last   Avg  Best  Wrst StDev
 1. gateway.local                     0.0%   100    0.5   0.6   0.4   1.2   0.1
 2. isp-router.example.net            0.0%   100    5.2   5.5   4.8   8.3   0.5
 3. core-router.isp.net               0.0%   100   12.3  12.8  11.5  18.2   1.2
 4. peering.google.com                0.0%   100   15.1  15.4  14.2  22.1   1.5
 5. 142.250.179.110                   0.0%   100   15.8  16.2  15.0  23.5   1.8
```

**Colonnes :**

| Colonne | Description | Alerte Si |
|---------|-------------|-----------|
| `Loss%` | Pourcentage de paquets perdus | > 1% |
| `Snt` | Paquets envoyés | - |
| `Last` | Latence du dernier paquet (ms) | - |
| `Avg` | Latence moyenne (ms) | Augmentation soudaine |
| `Best` | Meilleure latence (ms) | - |
| `Wrst` | Pire latence (ms) | >> Avg = jitter |
| `StDev` | Écart-type (jitter) | > 5 ms |

### 4.3 Interpréter les Résultats

```
┌─────────────────────────────────────────────────────────────────┐
│              INTERPRÉTATION MTR                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   SCÉNARIO 1 : Perte sur un saut intermédiaire uniquement       │
│   ──────────────────────────────────────────────────────        │
│   Hop 3 :  5.0% loss                                            │
│   Hop 4 :  0.0% loss                                            │
│   Hop 5 :  0.0% loss (destination)                              │
│                                                                 │
│   → FAUX POSITIF : Le routeur au hop 3 rate-limite ICMP         │
│     mais forward le trafic réel. Pas de problème.               │
│                                                                 │
│   SCÉNARIO 2 : Perte qui se propage                             │
│   ─────────────────────────────────                             │
│   Hop 3 :  5.0% loss                                            │
│   Hop 4 :  5.2% loss                                            │
│   Hop 5 :  5.1% loss (destination)                              │
│                                                                 │
│   → VRAI PROBLÈME : Congestion ou défaillance au hop 3          │
│     Impact sur tout le trafic.                                  │
│                                                                 │
│   SCÉNARIO 3 : Latence qui augmente soudainement                │
│   ───────────────────────────────────────────────               │
│   Hop 1 :  Avg 0.5 ms                                           │
│   Hop 2 :  Avg 5.0 ms                                           │
│   Hop 3 :  Avg 150 ms  ← Saut anormal !                         │
│   Hop 4 :  Avg 155 ms                                           │
│                                                                 │
│   → Lien saturé ou problème au hop 3                            │
│     (changement de ~145 ms entre hop 2 et 3)                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Autres Outils Essentiels

### 5.1 ping : Le Basique

```bash
# Ping simple
ping -c 5 google.com

# Avec timestamp
ping -D google.com

# Flood ping (attention !)
sudo ping -f -c 1000 192.168.1.1

# Taille de paquet personnalisée (test MTU)
ping -s 1472 -M do google.com
# Si "Message too long" → MTU trop grand
```

### 5.2 Diagnostic DNS

```bash
# Résolution DNS
dig google.com

# Temps de résolution
dig google.com | grep "Query time"

# Tester un serveur DNS spécifique
dig @8.8.8.8 google.com

# Résolution inverse
dig -x 142.250.179.110

# Version courte
host google.com
```

### 5.3 tcpdump : Capture de Paquets

```bash
# Capturer sur une interface
sudo tcpdump -i eth0

# Filtrer par host
sudo tcpdump -i eth0 host 10.0.0.50

# Filtrer par port
sudo tcpdump -i eth0 port 443

# Sauvegarder dans un fichier (pour Wireshark)
sudo tcpdump -i eth0 -w capture.pcap

# Lire un fichier de capture
tcpdump -r capture.pcap

# Afficher le contenu ASCII
sudo tcpdump -i eth0 -A port 80
```

---

## 6. Scénarios de Diagnostic

### 6.1 "L'application est lente vers la base de données"

```bash
# 1. Vérifier la latence réseau
ping -c 10 database.internal
mtr -r -c 50 database.internal

# 2. Vérifier les connexions
ss -tn dst database.internal

# 3. Vérifier le débit
iftop -f "host database.internal"

# 4. Vérifier les états TCP
ss -tn state time-wait dst database.internal | wc -l
# Beaucoup de TIME_WAIT = connexions courtes, possible pooling manquant

# 5. Vérifier les erreurs interface
ip -s link show eth0
# Regarder RX/TX errors, dropped
```

### 6.2 "Le serveur ne répond plus sur le port 443"

```bash
# 1. Le service écoute-t-il ?
ss -tlnp | grep :443

# 2. Firewall local ?
sudo iptables -L -n | grep 443
# ou
sudo firewall-cmd --list-all

# 3. Connexion depuis l'extérieur ?
# (depuis une autre machine)
nc -zv server.example.com 443
curl -v https://server.example.com

# 4. Beaucoup de connexions en attente ?
ss -tn state syn-recv | wc -l
# Si élevé = possible SYN flood ou backlog trop petit
```

### 6.3 "Perte de paquets intermittente"

```bash
# 1. Analyse continue avec mtr
mtr -r -c 500 destination.com

# 2. Vérifier les erreurs interface
watch -n 1 'ip -s link show eth0 | grep -E "errors|dropped"'

# 3. Vérifier les buffers réseau
cat /proc/net/softnet_stat
# Colonne 2 = dropped, colonne 3 = time_squeeze

# 4. Statistiques détaillées
netstat -s | grep -i error
netstat -s | grep -i drop
```

---

## 7. Commandes de Référence Rapide

```bash
# === BANDE PASSANTE ===
iftop -i eth0                   # Trafic par connexion
nload eth0                      # Trafic par interface
sar -n DEV 1                    # Statistiques réseau

# === SOCKETS ===
ss -tulpn                       # Tous les ports en écoute
ss -tn                          # Connexions TCP établies
ss -s                           # Résumé des états
ss -tn dport = :443             # Connexions vers port 443

# === LATENCE ===
ping -c 10 host                 # Test basique
mtr host                        # Traceroute continu
mtr -r -c 100 host              # Rapport mtr

# === DNS ===
dig domain.com                  # Résolution DNS
dig @8.8.8.8 domain.com         # Via DNS spécifique
host domain.com                 # Résolution simple

# === DIAGNOSTIC ===
ip -s link show eth0            # Stats interface (errors, drops)
netstat -s                      # Stats protocoles
cat /proc/net/softnet_stat      # Stats softirq réseau

# === CAPTURE ===
sudo tcpdump -i eth0 port 443   # Capturer trafic
sudo tcpdump -w capture.pcap    # Sauvegarder
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Une connexion a une bande passante de 10 Gbps mais une latence de 100 ms. Le téléchargement d'un fichier de 1 GB sera-t-il rapide ?"
    **Réponse :** Oui, le transfert sera rapide car un gros fichier exploite la bande passante. Avec 10 Gbps, 1 GB prend ~0.8 seconde en théorie.

    Par contre, une API REST qui fait 1000 requêtes séquentielles prendra au minimum 100 secondes (1000 × 100 ms) quelle que soit la bande passante.

??? question "Question 2 : Vous voyez beaucoup de sockets en état CLOSE_WAIT. Quel est le problème ?"
    **Réponse :** CLOSE_WAIT signifie que le pair distant a fermé la connexion, mais notre application n'a pas appelé `close()` sur son socket.

    C'est généralement un **bug applicatif** : l'application ne ferme pas correctement ses connexions. Les file descriptors s'accumulent jusqu'à atteindre la limite (`ulimit`).

??? question "Question 3 : Dans mtr, le hop 4 montre 15% de perte, mais la destination (hop 6) montre 0%. Est-ce un problème ?"
    **Réponse :** Probablement pas. De nombreux routeurs **rate-limitent les réponses ICMP** pour se protéger, mais forward le trafic réel normalement.

    Ce qui compte, c'est la perte à la **destination finale**. Si elle est à 0%, le chemin fonctionne.

---

## Félicitations !

Vous avez terminé la formation **Investigation de Performance Linux**. Vous maîtrisez maintenant :

- :material-check-circle: La méthode USE pour une investigation systématique
- :material-check-circle: L'analyse CPU, Mémoire, Disque et Réseau
- :material-check-circle: Les outils de diagnostic pour chaque ressource
- :material-check-circle: L'interprétation des métriques critiques

Vous êtes prêt à répondre à : **"Pourquoi le serveur est-il lent ?"**

[:octicons-arrow-left-24: Retour au Module 3 : Disque & I/O](03-disk-io.md)

[:octicons-home: Retour à l'index de la formation](index.md)

---

**Temps estimé :** 45 minutes
**Niveau :** Intermédiaire
