# Module 3 : Disque & I/O

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre la différence entre IOPS et Throughput
- :material-check: Interpréter les métriques d'`iostat`
- :material-check: Identifier les processus responsables des I/O
- :material-check: Diagnostiquer les problèmes de latence disque

---

## 1. IOPS vs Throughput : Deux Métriques Différentes

### 1.1 Définitions

```
┌─────────────────────────────────────────────────────────────────┐
│              IOPS vs THROUGHPUT                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   IOPS (Input/Output Operations Per Second)                     │
│   ─────────────────────────────────────────                     │
│   • Nombre d'opérations I/O par seconde                         │
│   • Mesure la RÉACTIVITÉ du stockage                            │
│   • Important pour : bases de données, applications             │
│     transactionnelles, accès aléatoires                         │
│   • Typique HDD : 100-200 IOPS                                  │
│   • Typique SSD : 10,000-100,000+ IOPS                          │
│                                                                 │
│   THROUGHPUT (Débit en MB/s)                                    │
│   ──────────────────────────                                    │
│   • Quantité de données transférées par seconde                 │
│   • Mesure la CAPACITÉ de transfert                             │
│   • Important pour : streaming vidéo, backup,                   │
│     transferts de gros fichiers, accès séquentiels              │
│   • Typique HDD : 100-200 MB/s                                  │
│   • Typique SSD : 500-7000 MB/s (NVMe)                          │
│                                                                 │
│   ⚠️ Un disque peut avoir un bon throughput mais des IOPS       │
│      médiocres (HDD) ou l'inverse (rare)                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Analogie

| Métrique | Analogie Autoroute |
|----------|-------------------|
| **IOPS** | Nombre de véhicules pouvant entrer sur l'autoroute par minute |
| **Throughput** | Nombre total de passagers transportés par heure |
| **Latence** | Temps pour qu'un véhicule traverse l'autoroute |

### 1.3 Impact par Workload

| Type d'Application | Métrique Critique | Pourquoi |
|-------------------|-------------------|----------|
| Base de données (OLTP) | IOPS, Latence | Petites requêtes fréquentes |
| Data Warehouse (OLAP) | Throughput | Gros scans séquentiels |
| Serveur de fichiers | Les deux | Mix d'accès |
| Backup/Restore | Throughput | Transferts massifs |
| Serveur Web (logs) | IOPS Write | Écritures fréquentes |

---

## 2. L'Outil Roi : iostat

### 2.1 Commande de Base

```bash
# Installation (si nécessaire)
# Debian/Ubuntu: apt install sysstat
# RHEL/Rocky: dnf install sysstat

# Commande recommandée
iostat -xkz 1
```

- `-x` : Mode étendu (toutes les métriques)
- `-k` : Affichage en KB (ou `-m` pour MB)
- `-z` : Masquer les devices inactifs
- `1` : Rafraîchir toutes les secondes

### 2.2 Lecture de la Sortie

```
Linux 5.4.0-generic    01/28/25    _x86_64_    (4 CPU)

avg-cpu:  %user   %nice %system %iowait  %steal   %idle
          15.25    0.00    5.50   12.75    0.00   66.50

Device     r/s     w/s    rkB/s    wkB/s  rrqm/s  wrqm/s  %rrqm  %wrqm r_await w_await aqu-sz rareq-sz wareq-sz  svctm  %util
sda      125.00  350.00  2048.00  8192.00   5.00   45.00   3.85  11.39    2.50   15.80   1.25    16.38    23.41   1.80  85.50
sdb       10.00   25.00   512.00  1024.00   0.00    5.00   0.00  16.67    0.50    2.00   0.05    51.20    40.96   1.20   4.20
nvme0n1  500.00  200.00 32768.00 16384.00   0.00    0.00   0.00   0.00    0.08    0.12   0.01    65.54    81.92   0.05   3.50
```

### 2.3 Colonnes Critiques

```
┌─────────────────────────────────────────────────────────────────┐
│              COLONNES IOSTAT À MAÎTRISER                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   r/s, w/s (IOPS)                                               │
│   ───────────────                                               │
│   Lectures et écritures par seconde                             │
│   → Charge du disque en opérations                              │
│                                                                 │
│   rkB/s, wkB/s (Throughput)                                     │
│   ─────────────────────────                                     │
│   Débit en KB/s (lecture, écriture)                             │
│   → Volume de données transférées                               │
│                                                                 │
│   await (Latence Globale) ⭐ CRITIQUE                           │
│   ─────────────────────────────────                             │
│   Temps moyen d'une I/O (ms) = temps file + temps service       │
│   • < 1 ms   : Excellent (NVMe)                                 │
│   • 1-5 ms   : Bon (SSD SATA)                                   │
│   • 5-20 ms  : Acceptable (HDD rapide)                          │
│   • > 20 ms  : Problème (HDD saturé ou défaillant)              │
│   • > 100 ms : Critique !                                       │
│                                                                 │
│   %util (Saturation) ⭐ CRITIQUE                                │
│   ───────────────────────────                                   │
│   Pourcentage de temps où le device traite des I/O              │
│   • < 70%  : OK                                                 │
│   • 70-90% : Attention                                          │
│   • > 90%  : Saturation (goulot d'étranglement)                 │
│                                                                 │
│   aqu-sz (Average Queue Size)                                   │
│   ───────────────────────────                                   │
│   Nombre moyen de requêtes en file d'attente                    │
│   • < 1   : Pas de saturation                                   │
│   • > 1   : Les requêtes s'accumulent                           │
│   • > 10  : Saturation sévère                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.4 Interprétation des Scénarios

| %util | await | Diagnostic |
|-------|-------|------------|
| Bas | Bas | Disque sous-utilisé, tout va bien |
| Haut | Bas | Disque bien utilisé, performant |
| Bas | Haut | Problème hardware ou réseau (NFS, SAN) |
| Haut | Haut | **Saturation - Goulot d'étranglement** |

---

## 3. Identifier le Coupable : iotop

### 3.1 Utilisation de Base

```bash
# Nécessite les droits root
sudo iotop

# Mode batch (pour scripts)
sudo iotop -b -n 3

# Afficher seulement les processus actifs
sudo iotop -o

# Accumuler les I/O (total depuis le lancement)
sudo iotop -a
```

### 3.2 Lecture de l'Interface

```
Total DISK READ:       15.23 M/s | Total DISK WRITE:       8.45 M/s
Current DISK READ:     12.50 M/s | Current DISK WRITE:      6.78 M/s

  TID  PRIO  USER     DISK READ  DISK WRITE  SWAPIN     IO>    COMMAND
 1234 be/4 mysql       10.50 M/s    5.25 M/s  0.00 %  85.50 % mysqld
 5678 be/4 www-data     1.25 M/s    2.10 M/s  0.00 %  15.20 % apache2
 9012 be/4 root         0.50 M/s    1.10 M/s  0.00 %   5.30 % rsync
```

**Colonnes :**

| Colonne | Description |
|---------|-------------|
| `TID` | Thread ID |
| `PRIO` | Priorité I/O (be = best effort) |
| `DISK READ/WRITE` | Débit actuel |
| `SWAPIN` | % temps en swap in |
| `IO>` | % temps en attente I/O |
| `COMMAND` | Processus |

### 3.3 Alternatives à iotop

```bash
# pidstat avec I/O (-d)
pidstat -d 1

# Sortie :
# 14:35:01  UID  PID  kB_rd/s  kB_wr/s  kB_ccwr/s  iodelay  Command
# 14:35:02 1000 1234  10240.00  5120.00       0.00      125  mysqld

# Avec les threads
pidstat -d -t 1
```

---

## 4. Analyse par Device

### 4.1 Mapper les Devices aux Montages

```bash
# Voir les montages
df -h

# Voir les devices block
lsblk

# Exemple de sortie lsblk
NAME        MAJ:MIN RM   SIZE RO TYPE MOUNTPOINT
sda           8:0    0   500G  0 disk
├─sda1        8:1    0   512M  0 part /boot
└─sda2        8:2    0 499.5G  0 part /
nvme0n1     259:0    0   1.8T  0 disk
└─nvme0n1p1 259:1    0   1.8T  0 part /data

# Identifier quel processus accède à quel fichier
sudo lsof +D /data | head -20
```

### 4.2 Analyser un Device Spécifique

```bash
# iostat pour un device spécifique
iostat -xkz 1 sda

# Voir les statistiques kernel
cat /sys/block/sda/stat

# Format : reads_completed reads_merged sectors_read ms_reading ...
```

---

## 5. Lab : Simulation de Charge I/O

### 5.1 Générer de la Charge avec dd

```bash
# ⚠️ ATTENTION : À exécuter dans un répertoire temporaire !
# Ne PAS exécuter sur un système de production !

# Test d'écriture séquentielle (throughput)
dd if=/dev/zero of=/tmp/testfile bs=1M count=1024 oflag=direct

# Test de lecture séquentielle
dd if=/tmp/testfile of=/dev/null bs=1M iflag=direct

# Nettoyage
rm /tmp/testfile
```

### 5.2 Générer de la Charge avec stress-ng

```bash
# Installation
# apt install stress-ng  ou  dnf install stress-ng

# Stress I/O : 4 workers, pendant 30 secondes
stress-ng --io 4 --timeout 30s --metrics-brief

# Stress HDD : écriture/lecture
stress-ng --hdd 2 --hdd-bytes 1G --timeout 30s

# Pendant ce temps, dans un autre terminal :
iostat -xkz 1
iotop -o
```

### 5.3 Observer les Métriques

```bash
# Terminal 1 : Lancer la charge
stress-ng --hdd 4 --timeout 60s

# Terminal 2 : Observer iostat
watch -n 1 'iostat -xkz 1 1 | tail -10'

# Terminal 3 : Observer les processus
sudo iotop -o
```

---

## 6. Problèmes Courants et Solutions

### 6.1 Diagnostic Rapide

```
┌─────────────────────────────────────────────────────────────────┐
│              ARBRE DE DÉCISION I/O                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   %util > 90% ?                                                 │
│   │                                                             │
│   ├── OUI → Disque saturé                                       │
│   │         │                                                   │
│   │         ├── await élevé → File d'attente pleine             │
│   │         │                 Actions :                         │
│   │         │                 • Identifier process (iotop)      │
│   │         │                 • Optimiser l'application         │
│   │         │                 • Migrer vers SSD/NVMe            │
│   │         │                                                   │
│   │         └── await normal → Disque à capacité max            │
│   │                           mais performant                   │
│   │                           Actions :                         │
│   │                           • Ajouter des disques (RAID)      │
│   │                           • Distribuer la charge            │
│   │                                                             │
│   └── NON → %util bas                                           │
│             │                                                   │
│             ├── await élevé → Problème hardware/réseau          │
│             │                 (latence intrinsèque)             │
│             │                 Actions :                         │
│             │                 • Vérifier câbles, contrôleur     │
│             │                 • Vérifier réseau SAN/NFS         │
│             │                 • smartctl pour santé disque      │
│             │                                                   │
│             └── await normal → Pas de problème I/O              │
│                               Le goulot est ailleurs            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Vérifier la Santé du Disque

```bash
# SMART status (disques locaux)
sudo smartctl -H /dev/sda

# Attributs SMART détaillés
sudo smartctl -A /dev/sda

# Erreurs récentes
sudo smartctl -l error /dev/sda

# Pour NVMe
sudo nvme smart-log /dev/nvme0n1
```

### 6.3 Tuning I/O Scheduler

```bash
# Voir le scheduler actuel
cat /sys/block/sda/queue/scheduler
# Sortie : [mq-deadline] kyber bfq none

# Schedulers recommandés :
# - mq-deadline : Bon pour les workloads mixtes
# - none/noop   : Pour SSD/NVMe (pas besoin de réordonnancement)
# - bfq         : Pour desktop/interactivité

# Changer temporairement
echo "mq-deadline" | sudo tee /sys/block/sda/queue/scheduler

# Pour NVMe, 'none' est souvent optimal
echo "none" | sudo tee /sys/block/nvme0n1/queue/scheduler
```

---

## 7. Commandes de Référence Rapide

```bash
# === ANALYSE GLOBALE ===
iostat -xkz 1                   # Métriques étendues, par seconde
iostat -xkz 1 5                 # 5 itérations

# === IDENTIFIER LE COUPABLE ===
sudo iotop -o                   # Processus actifs seulement
sudo iotop -a                   # Total accumulé
pidstat -d 1                    # I/O par processus

# === INFORMATIONS SYSTÈME ===
lsblk                           # Arbre des devices
df -h                           # Espace disque
mount | column -t               # Points de montage

# === SANTÉ DISQUE ===
sudo smartctl -H /dev/sda       # Health check
sudo smartctl -A /dev/sda       # Attributs SMART

# === FILES OUVERTES ===
sudo lsof +D /chemin            # Qui accède à ce chemin
sudo fuser -v /chemin           # Processus utilisant ce chemin

# === SCHEDULER ===
cat /sys/block/sda/queue/scheduler
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Un disque affiche 95% util mais un await de 0.5 ms. Est-ce un problème ?"
    **Réponse :** Non, c'est un bon signe ! Le disque est très sollicité (95% util) mais répond très rapidement (0.5 ms). C'est typique d'un SSD NVMe performant sous charge.

    Un problème serait : 95% util + await > 20 ms → saturation avec file d'attente.

??? question "Question 2 : Quelle est la différence entre r_await et w_await ?"
    **Réponse :**

    - **r_await** : Latence moyenne des opérations de **lecture**
    - **w_await** : Latence moyenne des opérations d'**écriture**

    Utile pour identifier si le problème est en lecture ou écriture. Les écritures sont souvent plus lentes car elles doivent être persistées (surtout avec write barriers/fsync).

??? question "Question 3 : Pourquoi 'none' est le scheduler recommandé pour NVMe ?"
    **Réponse :** Les schedulers I/O (comme mq-deadline ou bfq) réordonnent les requêtes pour minimiser les mouvements de tête de lecture sur les HDD.

    Les SSD NVMe n'ont pas de tête de lecture - l'accès à n'importe quel secteur est instantané. Le réordonnancement ajoute de la latence inutile. Le scheduler 'none' passe les requêtes directement au device.

---

## Prochaine Étape

Le disque n'est pas le seul goulot. Le réseau peut aussi être source de latence.

[:octicons-arrow-right-24: Module 4 : Réseau & Latence](04-network-latency.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Intermédiaire à Avancé
