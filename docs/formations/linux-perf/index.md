# Formation : Investigation de Performance Linux

## Introduction

**"Le serveur est lent."**

Cette phrase, tout ingénieur d'exploitation l'a entendue des centaines de fois. La différence entre un opérateur et un **SRE** (Site Reliability Engineer) réside dans la capacité à transformer cette plainte vague en un diagnostic précis et actionnable.

Cette formation vous donnera les outils et la méthodologie pour répondre méthodiquement à la question : **"Pourquoi le serveur est-il lent ?"**

```
┌─────────────────────────────────────────────────────────────────┐
│              LE PROBLÈME DE L'INVESTIGATION AD-HOC               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Approche Naïve :                                              │
│   ────────────────                                              │
│   1. "Le serveur est lent"                                      │
│   2. Lancer `top`                                               │
│   3. "Je ne vois rien d'anormal..."                             │
│   4. Redémarrer le service                                      │
│   5. "Ça remarche... pour l'instant"                            │
│   6. Le problème revient 3 jours plus tard                      │
│                                                                 │
│   Approche SRE :                                                │
│   ──────────────                                                │
│   1. "Le serveur est lent"                                      │
│   2. Appliquer la méthode USE systématiquement                  │
│   3. Identifier la ressource saturée                            │
│   4. Trouver le processus responsable                           │
│   5. Comprendre la cause racine                                 │
│   6. Corriger ET documenter                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## La Méthode USE

### Présentation

La **méthode USE** (Utilization, Saturation, Errors) a été créée par **Brendan Gregg**, ingénieur performance chez Netflix et auteur des outils BPF/eBPF. C'est une approche systématique pour identifier les goulots d'étranglement.

!!! info "Les Trois Métriques Fondamentales"

    Pour **chaque ressource** du système (CPU, RAM, Disque, Réseau), posez ces trois questions :

    | Métrique | Question | Exemple |
    |----------|----------|---------|
    | **U**tilization | Quel pourcentage de la capacité est utilisé ? | CPU à 85% |
    | **S**aturation | Y a-t-il une file d'attente ? | 10 processus en attente de CPU |
    | **E**rrors | Y a-t-il des erreurs ? | Erreurs disque, paquets droppés |

### Application Systématique

```
┌─────────────────────────────────────────────────────────────────┐
│                    MÉTHODE USE - CHECKLIST                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   RESSOURCE       │ UTILIZATION    │ SATURATION    │ ERRORS    │
│   ────────────────┼────────────────┼───────────────┼───────────│
│   CPU             │ mpstat, top    │ load average  │ dmesg     │
│                   │ %usr + %sys    │ vmstat (r)    │ perf      │
│   ────────────────┼────────────────┼───────────────┼───────────│
│   Mémoire         │ free -h        │ vmstat (si/so)│ dmesg     │
│                   │ /proc/meminfo  │ oom-killer    │ EDAC      │
│   ────────────────┼────────────────┼───────────────┼───────────│
│   Disque          │ iostat %util   │ iostat avgqu  │ smartctl  │
│                   │ sar -d         │ await élevé   │ dmesg     │
│   ────────────────┼────────────────┼───────────────┼───────────│
│   Réseau          │ sar -n DEV     │ ifconfig      │ ifconfig  │
│                   │ nload, iftop   │ (overruns)    │ (errors)  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Au-delà de `top`

`top` est un excellent outil pour un premier aperçu, mais il a ses limites :

| Ce que `top` montre | Ce que `top` ne montre PAS |
|---------------------|---------------------------|
| Usage CPU global | I/O Wait par processus |
| Mémoire utilisée | Activité Swap en temps réel |
| Processus actifs | Latence disque |
| PID et commandes | Débit réseau |

Cette formation vous apprendra à utiliser les **bons outils** pour chaque investigation.

---

## Syllabus de la Formation

Cette formation est organisée en **4 modules** couvrant les 4 ressources critiques :

### Module 1 : CPU & Load Average
:material-cpu-64-bit: **Le Processeur** | :material-clock-outline: ~45 min

- Comprendre le Load Average (ce n'est PAS le % CPU)
- Le mystère du "High Load, Low CPU" → I/O Wait
- Outils : `htop`, `mpstat`, `pidstat`

[:octicons-arrow-right-24: Accéder au Module 1](01-cpu-load.md)

---

### Module 2 : Mémoire & Swap
:material-memory: **La RAM** | :material-clock-outline: ~45 min

- Free vs Available (le piège des buffers/cache)
- Pourquoi le swap détruit les performances
- Outils : `free`, `vmstat`, `/proc/meminfo`

[:octicons-arrow-right-24: Accéder au Module 2](02-memory-swap.md)

---

### Module 3 : Disque & I/O
:material-harddisk: **Le Stockage** | :material-clock-outline: ~60 min

- IOPS vs Throughput (MB/s)
- Latence disque : le tueur silencieux
- Outils : `iostat`, `iotop`, analyse du `await`

[:octicons-arrow-right-24: Accéder au Module 3](03-disk-io.md)

---

### Module 4 : Réseau & Latence
:material-lan: **Le Réseau** | :material-clock-outline: ~45 min

- Bande passante ≠ Latence
- Investigation des sockets et connexions
- Outils : `ss`, `iftop`, `mtr`

[:octicons-arrow-right-24: Accéder au Module 4](04-network-latency.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **Linux CLI** : Navigation, pipes, redirections
    - **Processus** : PID, PPID, signaux (`kill`, `ps`)
    - **Système de fichiers** : Montage, `/proc`, `/sys`
    - **Réseau de base** : IP, ports, TCP/UDP

### Environnement de Lab

=== "Distribution Recommandée"

    ```bash
    # Cette formation est testée sur :
    # - Ubuntu 20.04+ / Debian 11+
    # - RHEL 8+ / Rocky Linux 8+
    # - Amazon Linux 2

    # Vérifier la version du kernel
    uname -r
    # Recommandé : 4.15+ pour les fonctionnalités BPF
    ```

=== "Installation des Outils"

    ```bash
    # Debian/Ubuntu
    sudo apt update
    sudo apt install -y sysstat htop iotop iftop nload mtr-tiny stress-ng

    # RHEL/Rocky/Alma
    sudo dnf install -y sysstat htop iotop iftop nload mtr stress-ng

    # Activer la collecte sysstat (pour sar)
    sudo systemctl enable --now sysstat
    ```

---

## Méthodologie d'Investigation

### Le Workflow SRE

```
┌─────────────────────────────────────────────────────────────────┐
│              WORKFLOW D'INVESTIGATION PERFORMANCE                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. TRIAGE INITIAL (30 secondes)                               │
│      ─────────────────────────────                              │
│      $ uptime           # Load average                          │
│      $ dmesg -T | tail  # Erreurs kernel récentes               │
│      $ free -h          # État mémoire                          │
│                                                                 │
│   2. IDENTIFICATION RESSOURCE (2 minutes)                       │
│      ────────────────────────────────────                       │
│      Appliquer USE sur CPU, RAM, Disk, Network                  │
│      Identifier la ressource saturée                            │
│                                                                 │
│   3. IDENTIFICATION PROCESSUS (2 minutes)                       │
│      ─────────────────────────────────────                      │
│      $ pidstat 1        # CPU par process                       │
│      $ iotop            # I/O par process                       │
│      $ ss -tulpn        # Sockets par process                   │
│                                                                 │
│   4. ANALYSE APPROFONDIE (variable)                             │
│      ──────────────────────────────────                         │
│      Logs applicatifs, traces, profiling                        │
│                                                                 │
│   5. REMÉDIATION & DOCUMENTATION                                │
│      ───────────────────────────────                            │
│      Corriger, documenter, monitorer                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Les 60 Premières Secondes

Brendan Gregg propose cette checklist pour les 60 premières secondes sur un serveur en difficulté :

```bash
# 1. Load et uptime
uptime

# 2. Erreurs kernel
dmesg -T | tail -20

# 3. Statistiques CPU
vmstat 1 5

# 4. Statistiques mémoire
free -h

# 5. I/O disque
iostat -xz 1 3

# 6. Statistiques réseau
sar -n DEV 1 3

# 7. Processus par CPU
pidstat 1 3

# 8. Processus par I/O
iotop -b -n 3

# 9. Vue d'ensemble
top -b -n 1 | head -20
```

---

## Ressources Complémentaires

### Références Essentielles

- :material-link: [Brendan Gregg - USE Method](https://www.brendangregg.com/usemethod.html)
- :material-link: [Linux Performance Analysis in 60s](https://www.brendangregg.com/Articles/Netflix_Linux_Perf_Analysis_60s.pdf)
- :material-link: [Netflix Tech Blog - Linux Performance](https://netflixtechblog.com/linux-performance-analysis-in-60-000-milliseconds-accc10403c55)

### Outils Avancés (Hors Scope)

| Outil | Usage | Niveau |
|-------|-------|--------|
| `perf` | Profiling CPU | Avancé |
| `bpftrace` | Tracing kernel | Expert |
| `flame graphs` | Visualisation CPU | Avancé |
| `strace` | Syscalls debugging | Intermédiaire |

---

!!! quote "Brendan Gregg"
    *"Understanding the USE method means you can apply it to any system, any component, even components that haven't been invented yet."*

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook SRE Team
