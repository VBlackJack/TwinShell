# Module 2 : Mémoire & Swap

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre la gestion mémoire Linux (Buffers, Cache, Available)
- :material-check: Interpréter correctement la sortie de `free`
- :material-check: Détecter et diagnostiquer l'utilisation du swap
- :material-check: Identifier les processus consommateurs de mémoire

---

## 1. La Gestion Mémoire Linux

### 1.1 Le Mythe de la RAM "Utilisée"

!!! danger "Ne Paniquez Pas !"
    Voir 90% de RAM "utilisée" sur un serveur Linux est **parfaitement normal**.

    Linux utilise la RAM inutilisée comme **cache disque** pour améliorer les performances. Cette mémoire est **immédiatement libérable** si une application en a besoin.

### 1.2 Anatomie de la Mémoire Linux

```
┌─────────────────────────────────────────────────────────────────┐
│                    MÉMOIRE LINUX EXPLIQUÉE                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   RAM Totale : 16 GB                                            │
│   ═══════════════════════════════════════════════════════════   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │             UTILISÉE PAR APPLICATIONS (4 GB)            │   │
│   │  MySQL, Apache, Java... - NE PEUT PAS être libérée      │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                  BUFFERS (0.5 GB)                       │   │
│   │  Métadonnées du système de fichiers                     │   │
│   │  PEUT être libéré si besoin                             │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                   CACHE (10 GB)                         │   │
│   │  Cache de lecture disque (page cache)                   │   │
│   │  PEUT être libéré immédiatement si besoin               │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │               RÉELLEMENT LIBRE (1.5 GB)                 │   │
│   │  RAM non allouée                                        │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   📊 "used" dans les anciens outils = 4 + 0.5 + 10 = 14.5 GB   │
│   📊 "available" (moderne) = 1.5 + 10 + 0.5 = 12 GB           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 Free vs Available

| Métrique | Signification | Usage |
|----------|---------------|-------|
| **free** | RAM non allouée du tout | Métrique trompeuse |
| **available** | RAM disponible pour les applications (free + buffers/cache récupérables) | **Métrique à surveiller** |
| **used** | RAM réellement utilisée (hors cache récupérable) | Moderne : fiable |

---

## 2. L'Outil `free`

### 2.1 Lecture de la Sortie

```bash
$ free -h
              total        used        free      shared  buff/cache   available
Mem:           16Gi       4.2Gi       1.5Gi       256Mi       10.3Gi        11.2Gi
Swap:         2.0Gi          0B       2.0Gi
```

```
┌─────────────────────────────────────────────────────────────────┐
│                    DÉCODAGE DE free -h                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   total        = 16 Gi   → RAM physique totale                  │
│                                                                 │
│   used         = 4.2 Gi  → RAM utilisée par les applications    │
│                           (SANS le cache récupérable)           │
│                                                                 │
│   free         = 1.5 Gi  → RAM complètement libre               │
│                           ⚠️ Métrique trompeuse !                │
│                                                                 │
│   shared       = 256 Mi  → Mémoire partagée (tmpfs, shmem)      │
│                                                                 │
│   buff/cache   = 10.3 Gi → Buffers + Page Cache                 │
│                           (récupérable si besoin)               │
│                                                                 │
│   available    = 11.2 Gi → CE QUI COMPTE : mémoire disponible   │
│                           pour lancer de nouvelles apps         │
│                                                                 │
│   ✅ Règle : Surveillez "available", pas "free"                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Options Utiles

```bash
# Format humain (Gi, Mi)
free -h

# En mégaoctets
free -m

# Avec rafraîchissement toutes les 2 secondes
free -h -s 2

# Total (ajoute une ligne de total)
free -h -t

# Affichage étendu
free -h -w   # Sépare buffers et cache
```

!!! tip "La Colonne Importante"
    **available** est la métrique clé. Si `available` est bas (< 10% de total), le système peut commencer à swapper.

---

## 3. Le Swap : Le Tueur de Performance

### 3.1 Qu'est-ce que le Swap ?

Le **swap** est une zone sur le disque utilisée comme extension de la RAM quand celle-ci est pleine. C'est un **filet de sécurité**, pas une solution de performance.

```
┌─────────────────────────────────────────────────────────────────┐
│                    IMPACT DU SWAP                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Accès RAM  : ~100 nanosecondes       (0.0001 ms)              │
│   Accès SSD  : ~100 microsecondes      (0.1 ms)    = 1000x      │
│   Accès HDD  : ~10 millisecondes       (10 ms)     = 100000x    │
│                                                                 │
│   ⚠️ Le swap est 1000 à 100000 fois plus LENT que la RAM !      │
│                                                                 │
│   CONSÉQUENCE :                                                 │
│   Une application qui swappe devient extrêmement lente.         │
│   Les latences explosent de millisecondes à SECONDES.           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Détecter l'Utilisation du Swap

```bash
# Vue basique
free -h
# Regarder la ligne "Swap" - used doit être à 0 ou très bas

# Vue détaillée avec vmstat
vmstat 1 5
```

```
procs -----------memory---------- ---swap-- -----io---- -system-- ------cpu-----
 r  b   swpd   free   buff  cache   si   so    bi    bo   in   cs us sy id wa st
 1  0      0 1567432 234567 9876543    0    0     5    12  125  456 15  5 78  2  0
 2  0      0 1565432 234567 9876543    0    0     0     0  130  478 18  6 74  2  0
 3  2  51200 1234567 234567 9876543  128  256   145   267  180  890 25  8 45 22  0
                                     ───  ───
                                      │    └── Swap OUT (écriture vers swap)
                                      └─────── Swap IN (lecture depuis swap)
```

**Colonnes Critiques de vmstat :**

| Colonne | Signification | Alerte Si |
|---------|---------------|-----------|
| `swpd` | Swap utilisé (KB) | > 0 et croissant |
| `si` | Swap In (KB/s lu depuis swap) | > 0 soutenu |
| `so` | Swap Out (KB/s écrit vers swap) | > 0 soutenu |
| `b` | Processus bloqués (I/O wait) | > 0 soutenu |

!!! danger "Le Signal d'Alarme"
    Si vous voyez des valeurs `si` ou `so` **non nulles de manière soutenue**, le système swappe activement. C'est le symptôme d'un **manque de RAM critique**.

    Actions :
    1. Identifier les processus gourmands
    2. Augmenter la RAM
    3. Ou réduire la charge

### 3.3 Analyse Détaillée du Swap

```bash
# Voir quels processus utilisent le swap
for pid in /proc/[0-9]*; do
    swap=$(awk '/VmSwap/ {print $2}' "$pid/status" 2>/dev/null)
    if [ -n "$swap" ] && [ "$swap" -gt 0 ]; then
        name=$(cat "$pid/comm" 2>/dev/null)
        echo "$swap KB - PID ${pid##*/} - $name"
    fi
done | sort -rn | head -10

# Ou avec smem (si installé)
smem -rs swap | head -10

# Voir l'utilisation du swap par processus
cat /proc/*/status 2>/dev/null | grep -E "^(Name|VmSwap)" | paste - - | sort -k4 -rn | head
```

---

## 4. Analyse Avancée avec /proc/meminfo

### 4.1 Les Métriques Importantes

```bash
cat /proc/meminfo
```

```
MemTotal:       16384000 kB    # RAM totale
MemFree:         1567432 kB    # RAM libre (sans cache)
MemAvailable:   11534567 kB    # RAM disponible (métrique clé)
Buffers:          234567 kB    # Buffer cache (métadonnées FS)
Cached:          9876543 kB    # Page cache (contenu fichiers)
SwapCached:            0 kB    # Swap en cache RAM
SwapTotal:       2097152 kB    # Swap total
SwapFree:        2097152 kB    # Swap libre
Dirty:             12345 kB    # Pages modifiées pas encore écrites
Writeback:             0 kB    # Pages en cours d'écriture
AnonPages:       4321098 kB    # Mémoire anonyme (heap, stack)
Mapped:           567890 kB    # Fichiers mappés en mémoire
Shmem:            262144 kB    # Mémoire partagée (tmpfs)
Slab:             345678 kB    # Cache structures kernel
SReclaimable:     234567 kB    # Slab récupérable
SUnreclaim:       111111 kB    # Slab non récupérable
```

### 4.2 Script de Diagnostic Mémoire

```bash
#!/bin/bash
# mem-check.sh - Diagnostic rapide mémoire

echo "=== Diagnostic Mémoire ==="
echo

# Métriques de base
total=$(awk '/MemTotal/ {print $2}' /proc/meminfo)
available=$(awk '/MemAvailable/ {print $2}' /proc/meminfo)
swap_total=$(awk '/SwapTotal/ {print $2}' /proc/meminfo)
swap_free=$(awk '/SwapFree/ {print $2}' /proc/meminfo)
swap_used=$((swap_total - swap_free))

# Calculs
available_pct=$((available * 100 / total))
swap_used_pct=0
[ "$swap_total" -gt 0 ] && swap_used_pct=$((swap_used * 100 / swap_total))

echo "RAM Total    : $((total / 1024)) MB"
echo "RAM Available: $((available / 1024)) MB ($available_pct%)"
echo "Swap Used    : $((swap_used / 1024)) MB ($swap_used_pct%)"
echo

# Évaluation
if [ "$available_pct" -lt 10 ]; then
    echo "⚠️  ATTENTION: Mémoire disponible < 10%"
elif [ "$available_pct" -lt 20 ]; then
    echo "⚡ AVERTISSEMENT: Mémoire disponible < 20%"
else
    echo "✅ Mémoire OK"
fi

if [ "$swap_used" -gt 0 ]; then
    echo "⚠️  ATTENTION: Swap utilisé ($((swap_used / 1024)) MB)"

    # Vérifier si swap actif
    si=$(vmstat 1 2 | tail -1 | awk '{print $7}')
    so=$(vmstat 1 2 | tail -1 | awk '{print $8}')
    if [ "$si" -gt 0 ] || [ "$so" -gt 0 ]; then
        echo "🔴 CRITIQUE: Swap ACTIF (si=$si so=$so)"
    fi
fi
```

---

## 5. Le OOM Killer

### 5.1 Qu'est-ce que l'OOM Killer ?

Quand le système n'a **vraiment** plus de mémoire (RAM + Swap), le kernel Linux active le **OOM Killer** (Out of Memory Killer) qui tue des processus pour libérer de la mémoire.

```
┌─────────────────────────────────────────────────────────────────┐
│                    OOM KILLER EN ACTION                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. La RAM est pleine                                          │
│   2. Le Swap est plein (ou absent)                              │
│   3. Une application demande plus de mémoire                    │
│   4. Le kernel ne peut pas satisfaire la demande                │
│   5. OOM Killer s'active                                        │
│   6. Il calcule un "score" pour chaque processus                │
│   7. Il TUE le processus avec le score le plus élevé            │
│   8. Souvent = votre application de production !                │
│                                                                 │
│   Score basé sur :                                              │
│   • Quantité de mémoire utilisée (principal)                    │
│   • Âge du processus                                            │
│   • Nice value                                                  │
│   • Ajustement manuel (oom_score_adj)                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Détecter un OOM Kill

```bash
# Dans les logs kernel
dmesg | grep -i "out of memory"
dmesg | grep -i "killed process"

# Ou avec journalctl
journalctl -k | grep -i "oom"

# Exemple de sortie
# [12345.678901] Out of memory: Killed process 1234 (java) total-vm:8765432kB
```

### 5.3 Protéger un Processus du OOM Killer

```bash
# Voir le score OOM d'un processus (plus élevé = plus de chances d'être tué)
cat /proc/<PID>/oom_score

# Ajuster le score (-1000 à +1000)
# -1000 = jamais tué, +1000 = toujours tué en premier
echo -500 > /proc/<PID>/oom_score_adj

# Pour le protéger totalement (déconseillé sauf cas critique)
echo -1000 > /proc/<PID>/oom_score_adj
```

---

## 6. Les Top Consommateurs

### 6.1 Identifier les Processus Gourmands

```bash
# Top 10 par mémoire résidente (RSS)
ps aux --sort=-%mem | head -11

# Avec plus de détails
ps -eo pid,ppid,user,%mem,%cpu,rss,vsz,comm --sort=-%mem | head -15

# Mémoire par utilisateur
ps -eo user,%mem --sort=-%mem | awk '{arr[$1]+=$2} END {for (i in arr) print arr[i], i}' | sort -rn

# Avec top (mode batch, une itération, trié par mémoire)
top -b -n 1 -o %MEM | head -20
```

### 6.2 Colonnes de Mémoire dans ps/top

| Colonne | Signification | Usage |
|---------|---------------|-------|
| `%MEM` | Pourcentage de RAM physique | Vue rapide |
| `RSS` | Resident Set Size : RAM physique utilisée | **Métrique clé** |
| `VSZ` | Virtual Size : Mémoire virtuelle allouée | Souvent trompeur |
| `SHR` | Shared : Mémoire partagée | Bibliothèques communes |

!!! tip "RSS vs VSZ"
    **RSS** est la métrique importante. C'est la mémoire physique réellement utilisée.

    **VSZ** inclut la mémoire virtuelle allouée mais pas forcément utilisée (ex: un processus Java avec -Xmx8G aura un VSZ de 8GB même s'il n'utilise que 500MB).

---

## 7. Commandes de Référence Rapide

```bash
# === VUE D'ENSEMBLE ===
free -h                         # État mémoire
free -h -s 2                    # Rafraîchi toutes les 2s

# === SWAP MONITORING ===
vmstat 1                        # Colonnes si/so pour swap in/out
swapon --show                   # Partitions swap

# === ANALYSE DÉTAILLÉE ===
cat /proc/meminfo               # Toutes les métriques kernel
cat /proc/<PID>/status          # Mémoire d'un processus
cat /proc/<PID>/smaps           # Détails mémoire très détaillés

# === TOP CONSOMMATEURS ===
ps aux --sort=-%mem | head      # Top par RAM
top -b -n 1 -o %MEM | head -20  # Top batch mode

# === OOM KILLER ===
dmesg | grep -i oom             # Détection OOM
cat /proc/<PID>/oom_score       # Score OOM d'un processus

# === LIBÉRER LE CACHE (URGENCE) ===
# ⚠️ Déconseillé en production - peut dégrader les performances
sync; echo 3 > /proc/sys/vm/drop_caches
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Un serveur affiche 'free' à 200 MB sur 16 GB. Faut-il s'inquiéter ?"
    **Réponse :** Pas nécessairement ! Il faut regarder la colonne **available**, pas **free**.

    Linux utilise la RAM inutilisée comme cache disque. Si `available` est à 10+ GB, le système va très bien. Le cache sera libéré automatiquement si une application en a besoin.

??? question "Question 2 : Quelle est la différence entre 'si' et 'so' dans vmstat ?"
    **Réponse :**

    - **si (Swap In)** : Pages lues DEPUIS le swap vers la RAM. Le système récupère des données qu'il avait swappées.

    - **so (Swap Out)** : Pages écrites VERS le swap depuis la RAM. Le système manque de RAM et doit déplacer des données vers le disque.

    Les deux sont mauvais pour les performances, mais `so` indique une pression mémoire active.

??? question "Question 3 : Pourquoi le RSS d'un processus peut être différent de la somme des RSS de ses threads ?"
    **Réponse :** Parce que les threads partagent le même espace mémoire. Le RSS du processus parent compte la mémoire partagée une seule fois, tandis que chaque thread pourrait la "revendiquer" individuellement.

    C'est pourquoi on regarde toujours le RSS du processus principal, pas des threads.

---

## Prochaine Étape

La mémoire est souvent victime, pas coupable. Le vrai problème est souvent le disque. Découvrez l'investigation I/O.

[:octicons-arrow-right-24: Module 3 : Disque & I/O](03-disk-io.md)

---

**Temps estimé :** 45 minutes
**Niveau :** Intermédiaire
