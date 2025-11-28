# Module 1 : CPU & Load Average

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Interpréter correctement le Load Average
- :material-check: Distinguer CPU-bound et I/O-bound
- :material-check: Diagnostiquer le scénario "High Load, Low CPU"
- :material-check: Identifier le processus responsable de la charge

---

## 1. Le Load Average : La Métrique la Plus Mal Comprise

### 1.1 Ce que Tout le Monde Croit

> "Le Load Average, c'est le pourcentage d'utilisation CPU."

**FAUX.** C'est l'erreur la plus répandue chez les administrateurs système.

### 1.2 La Vraie Définition

!!! info "Définition Correcte"
    Le **Load Average** représente le **nombre moyen de processus** dans la file d'exécution (runnable) **OU** en attente d'I/O (uninterruptible sleep) sur les 1, 5 et 15 dernières minutes.

```bash
$ uptime
 14:32:15 up 45 days,  3:21,  2 users,  load average: 4.52, 3.87, 2.15
                                                      ────  ────  ────
                                                      1min  5min  15min
```

### 1.3 Interprétation

```
┌─────────────────────────────────────────────────────────────────┐
│              INTERPRÉTATION DU LOAD AVERAGE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Serveur avec 4 CPUs :                                         │
│   ─────────────────────                                         │
│                                                                 │
│   Load = 1.0   │ 25% de capacité utilisée                       │
│                │ Tout va bien                                   │
│   ─────────────┼────────────────────────────────────────────    │
│   Load = 4.0   │ 100% de capacité utilisée                      │
│                │ Tous les CPUs sont occupés, pas de file        │
│   ─────────────┼────────────────────────────────────────────    │
│   Load = 8.0   │ 200% ! Surcharge                               │
│                │ 4 processus actifs + 4 en attente              │
│   ─────────────┼────────────────────────────────────────────    │
│   Load = 0.5   │ 12.5% - Le système est sous-utilisé            │
│                │                                                 │
│                                                                 │
│   📐 RÈGLE : Load / Nombre_CPUs = Ratio de charge               │
│              Ratio > 1.0 = Saturation                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

```bash
# Nombre de CPUs (ou cores)
nproc

# Ou
grep -c processor /proc/cpuinfo

# Calcul du ratio
echo "Load: $(uptime | awk -F'load average:' '{print $2}' | cut -d, -f1)"
echo "CPUs: $(nproc)"
```

### 1.4 La Tendance est Importante

```
Load Average: 12.52, 8.87, 4.15
              ─────  ────  ────
              Maintenant   Il y a 15 min

              ↑ La charge AUGMENTE (problème en cours)

Load Average: 4.15, 8.87, 12.52
              ────  ────  ─────
              Maintenant   Il y a 15 min

              ↓ La charge DIMINUE (problème en résolution)
```

---

## 2. Le Mystère : High Load, Low CPU Usage

### 2.1 Le Scénario

Vous observez :

```bash
$ uptime
load average: 25.43, 24.12, 22.87    # Charge très élevée !

$ top
%Cpu(s):  3.2 us,  1.1 sy,  0.0 ni, 94.5 id,  1.2 wa,  0.0 hi,  0.0 si
                                     ────
                                     94.5% IDLE ?!
```

**Comment le CPU peut-il être à 94% idle avec un load de 25 ?!**

### 2.2 L'Explication : I/O Wait

!!! warning "Le Coupable : I/O Wait"
    Le Load Average inclut les processus en état **"D" (Uninterruptible Sleep)**, c'est-à-dire en attente d'I/O disque ou réseau.

    Ces processus sont **comptés dans le load** mais ne consomment **pas de CPU**.

```
┌─────────────────────────────────────────────────────────────────┐
│              PROCESSUS ET LOAD AVERAGE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   État "R" (Running/Runnable)     →  Compte dans le Load        │
│   Le processus utilise ou attend     ET utilise le CPU          │
│   le CPU                                                        │
│                                                                 │
│   État "D" (Uninterruptible Sleep) → Compte dans le Load        │
│   Le processus attend une I/O         mais N'utilise PAS le CPU │
│   (disque, réseau NFS, etc.)                                    │
│                                                                 │
│   État "S" (Sleeping)             →  NE compte PAS              │
│   Le processus dort (attente                                    │
│   d'événement, timer, etc.)                                     │
│                                                                 │
│   CONCLUSION :                                                  │
│   High Load + Low CPU = Beaucoup de processus en état "D"       │
│                       = Problème d'I/O (disque ou réseau)       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Diagnostic

```bash
# Voir les processus en état D (Uninterruptible Sleep)
ps aux | awk '$8 ~ /D/ {print}'

# Ou avec top (regarder la colonne S pour "D")
top -b -n 1 | grep " D "

# Vérifier l'I/O Wait dans vmstat
vmstat 1 5
# Colonne "wa" (wait) dans la section CPU

# Vérifier avec iostat
iostat -x 1 3
# Regarder %util et await
```

---

## 3. Les Outils d'Investigation CPU

### 3.1 htop : La Vue Visuelle

`htop` est une version améliorée de `top` avec une interface interactive :

```bash
htop
```

```
┌─────────────────────────────────────────────────────────────────┐
│  htop - Vue Interactive                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  CPU[||||||||||||||||                    45.2%]   Tasks: 234    │
│  CPU[||||||||||||||||||||||              62.8%]   Load: 4.52    │
│  CPU[|||||||                             18.3%]   Uptime: 45d   │
│  CPU[|||||||||||||||||||||||||||||       78.1%]                 │
│  Mem[||||||||||||||||||||||||||    4.2G/16.0G]                  │
│  Swp[                               0K/2.0G]                    │
│                                                                 │
│  PID  USER   PRI  NI  VIRT   RES   SHR S CPU% MEM%   TIME+  CMD │
│  1234 mysql   20   0 4096M 2048M  128M S 45.2  12.8 123:45 mysql│
│  5678 www     20   0  512M  256M   64M S 18.3   1.6  45:23 php  │
│                                                                 │
│  F1=Aide  F2=Setup  F3=Search  F4=Filter  F5=Tree  F6=Sort     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Touches Utiles :**

| Touche | Action |
|--------|--------|
| `F5` | Vue arborescente (processus parent/enfant) |
| `F6` | Trier par colonne |
| `F4` | Filtrer par nom |
| `u` | Filtrer par utilisateur |
| `H` | Afficher/masquer les threads |
| `t` | Afficher en mode arbre |
| `P` | Trier par CPU |
| `M` | Trier par Mémoire |

### 3.2 mpstat : Analyse Par Cœur

`mpstat` montre l'utilisation CPU **par cœur**, essentiel pour détecter les déséquilibres :

```bash
# Statistiques de tous les CPUs, toutes les secondes
mpstat -P ALL 1
```

```
Linux 5.4.0-generic    01/28/25    _x86_64_    (4 CPU)

14:35:01     CPU    %usr   %nice    %sys %iowait   %irq   %soft  %steal   %idle
14:35:02     all   45.25    0.00   12.31    8.52   0.00    0.25    0.00   33.67
14:35:02       0   92.08    0.00    5.94    0.00   0.00    0.99    0.00    0.99
14:35:02       1   12.00    0.00    4.00   32.00   0.00    0.00    0.00   52.00
14:35:02       2   45.54    0.00   18.81    2.97   0.00    0.00    0.00   32.67
14:35:02       3   31.37    0.00   20.59    0.00   0.00    0.00    0.00   48.04
```

**Colonnes Importantes :**

| Colonne | Description | Alerte Si |
|---------|-------------|-----------|
| `%usr` | Code utilisateur (applications) | > 90% soutenu |
| `%sys` | Code kernel (syscalls) | > 30% (problème kernel) |
| `%iowait` | Attente I/O | > 20% (goulot disque) |
| `%steal` | Volé par hyperviseur | > 10% (VM surcommise) |
| `%idle` | Inactif | < 10% = saturation |

!!! danger "CPU0 à 92% et les autres à 30-50% ?"
    Cela indique un processus **mono-threadé** qui monopolise un seul cœur. Typique des applications legacy non parallélisées.

### 3.3 pidstat : Analyse Par Processus

`pidstat` montre l'utilisation CPU **par processus** :

```bash
# CPU par processus, toutes les secondes
pidstat 1

# Avec les threads (-t)
pidstat -t 1

# Pour un processus spécifique
pidstat -p 1234 1
```

```
Linux 5.4.0-generic    01/28/25    _x86_64_    (4 CPU)

14:40:01      UID       PID    %usr %system  %guest   %wait    %CPU   CPU  Command
14:40:02     1000      1234   85.00   12.00    0.00    3.00   97.00     0  stress
14:40:02       33      5678   15.00    8.00    0.00    0.00   23.00     2  apache2
14:40:02     1001      9012    5.00    2.00    0.00    1.00    7.00     1  python3
```

**Colonnes Clés :**

| Colonne | Description |
|---------|-------------|
| `%usr` | CPU en mode utilisateur |
| `%system` | CPU en mode kernel |
| `%wait` | Temps passé à attendre le CPU |
| `%CPU` | Total (sur tous les cœurs) |
| `CPU` | Numéro du cœur actuellement utilisé |

---

## 4. Scénarios d'Investigation

### 4.1 Scénario : Saturation CPU Classique

**Symptômes :**

- Load Average élevé (> nombre de CPUs)
- `%idle` très bas dans `top`
- Applications lentes

**Investigation :**

```bash
# 1. Confirmer la saturation
mpstat 1 5
# → %idle < 10% sur plusieurs cœurs

# 2. Identifier le processus
pidstat 1 | sort -k 8 -rn | head
# → Trier par %CPU décroissant

# 3. Analyser le processus
top -p <PID>
# ou
htop -p <PID>

# 4. Voir ce qu'il fait (syscalls)
strace -p <PID> -c
# Ctrl+C après quelques secondes pour le rapport
```

### 4.2 Scénario : High Load, Low CPU (I/O Bound)

**Symptômes :**

- Load Average très élevé
- `%idle` > 80%
- `%iowait` visible

**Investigation :**

```bash
# 1. Confirmer l'I/O Wait
vmstat 1 5
# Regarder colonne "wa" (wait) et "b" (blocked)

# 2. Identifier la source d'I/O
iotop -o
# -o = only show processes doing I/O

# 3. Voir les processus en état D
ps aux | awk '$8 ~ /D/'
# ou
for pid in $(ps -eo pid,stat | awk '$2 ~ /D/ {print $1}'); do
    echo "=== PID $pid ==="
    cat /proc/$pid/cmdline 2>/dev/null | tr '\0' ' '
    echo
done

# 4. Analyser le disque
iostat -xz 1
# Regarder %util et await
```

### 4.3 Scénario : Un Seul Core Saturé

**Symptômes :**

- Load Average autour de 1.0
- Un CPU à 100%, les autres idle
- Application mono-threadée lente

**Investigation :**

```bash
# 1. Confirmer avec mpstat
mpstat -P ALL 1
# → CPU0 à 95%, autres à 5%

# 2. Trouver le processus
pidstat 1 | grep -E "CPU|100"
# Colonne "CPU" indique le cœur

# 3. Options de résolution
# - Optimiser l'application (parallélisation)
# - Augmenter la fréquence CPU (scaling_governor)
# - Lancer plusieurs instances
```

---

## 5. Commandes de Référence Rapide

```bash
# === TRIAGE INITIAL ===
uptime                          # Load average rapide
nproc                           # Nombre de CPUs

# === ANALYSE CPU ===
top                             # Vue temps réel (q pour quitter)
htop                            # Vue améliorée
mpstat -P ALL 1                 # Par cœur, chaque seconde
pidstat 1                       # Par processus, chaque seconde
pidstat -t 1                    # Par thread

# === PROCESSUS EN ATTENTE ===
vmstat 1                        # Colonne "r" = runnable, "b" = blocked
ps aux | awk '$8 ~ /D/'         # Processus en I/O wait

# === ANALYSE AVANCÉE ===
perf top                        # Profiling temps réel (nécessite perf)
strace -p <PID> -c              # Syscalls d'un processus
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Un serveur 4 CPUs affiche un load average de 4.0. Est-ce un problème ?"
    **Réponse :** Pas nécessairement. Un load de 4.0 sur 4 CPUs signifie que le système tourne à 100% de sa capacité théorique. Ce n'est pas une surcharge (pas de file d'attente), mais c'est la limite. Au-delà de 4.0, les processus commenceront à attendre.

    La vraie question est : les applications répondent-elles dans des temps acceptables ?

??? question "Question 2 : Vous voyez un load de 50 mais un %idle de 85%. Que se passe-t-il ?"
    **Réponse :** C'est le scénario classique **High Load, Low CPU**. De nombreux processus sont en état "D" (Uninterruptible Sleep), attendant des I/O disque ou réseau. Ils sont comptés dans le load mais ne consomment pas de CPU.

    **Action :** Investiguer le disque avec `iostat` et identifier les processus avec `iotop`.

??? question "Question 3 : Quelle est la différence entre %iowait et %steal ?"
    **Réponse :**

    - **%iowait** : Le CPU est idle mais attend qu'une I/O se termine. Indique un problème de performance disque.

    - **%steal** : Dans une VM, c'est le temps où l'hyperviseur a "volé" du CPU pour d'autres VMs. Indique que l'hôte physique est surchargé.

---

## Prochaine Étape

Le CPU n'est qu'une partie de l'équation. Découvrez comment la mémoire peut affecter les performances.

[:octicons-arrow-right-24: Module 2 : Mémoire & Swap](02-memory-swap.md)

---

**Temps estimé :** 45 minutes
**Niveau :** Intermédiaire
