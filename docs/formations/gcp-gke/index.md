# Formation : Google Kubernetes Engine (GKE)

## Introduction

**Bienvenue dans le cloud-native.**

Google Kubernetes Engine (GKE) est le service managé de Kubernetes de Google Cloud Platform. En tant qu'ingénieurs Worldline migrant depuis l'on-premise, cette formation vous donnera les clés pour opérer GKE de manière sécurisée et efficace.

### Pourquoi GKE ?

```
┌─────────────────────────────────────────────────────────────────┐
│              KUBERNETES ON-PREM vs GKE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ON-PREMISE KUBERNETES                                         │
│   ─────────────────────                                         │
│   • Vous gérez TOUT : etcd, API Server, scheduler, etc.         │
│   • Mises à jour manuelles (downtime planifié)                  │
│   • Monitoring du control plane = votre responsabilité          │
│   • Scaling = commander du hardware                             │
│   • Coût : Équipe dédiée + hardware + électricité               │
│                                                                 │
│   GKE (MANAGED KUBERNETES)                                      │
│   ────────────────────────                                      │
│   • Control Plane géré par Google (SLA 99.95%)                  │
│   • Auto-upgrade : Mises à jour automatiques                    │
│   • Auto-repair : Nodes défaillants remplacés                   │
│   • Scaling : Nouveaux nodes en minutes                         │
│   • Coût : Pay-per-use + pas d'ops control plane                │
│                                                                 │
│   💡 Vous vous concentrez sur vos WORKLOADS, pas sur K8s        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Les Avantages Clés de GKE

| Fonctionnalité | Description | Impact |
|----------------|-------------|--------|
| **Control Plane Managé** | Google gère etcd, API Server, scheduler | Pas de maintenance K8s |
| **Auto-upgrade** | Mises à jour automatiques des nodes | Sécurité continue |
| **Auto-repair** | Remplacement automatique des nodes défaillants | Haute disponibilité |
| **Node Auto-provisioning** | Création automatique de node pools | Scaling intelligent |
| **Workload Identity** | Authentification native GCP | Sécurité sans clés JSON |
| **Binary Authorization** | Validation des images avant déploiement | Supply chain security |

---

## GKE Standard vs Autopilot

!!! warning "Choix Stratégique : GKE Standard"
    Cette formation se concentre sur **GKE Standard**, pas sur GKE Autopilot.

    | Aspect | GKE Standard | GKE Autopilot |
    |--------|--------------|---------------|
    | **Gestion des nodes** | Vous contrôlez | Google gère tout |
    | **Node pools** | Personnalisables | Automatiques |
    | **DaemonSets** | Supportés | Limités |
    | **Privileged containers** | Possibles | Interdits |
    | **SSH aux nodes** | Possible | Impossible |
    | **Tuning système** | Possible (sysctl, etc.) | Impossible |
    | **Coût** | Par node (même idle) | Par pod (pay-per-use) |

    **Pourquoi Standard pour Worldline ?**

    - Contrôle total sur la configuration des nodes
    - Possibilité de DaemonSets pour monitoring/sécurité
    - Tuning avancé pour workloads spécifiques
    - Debugging possible via SSH
    - Conformité SecNumCloud plus flexible

---

## Syllabus de la Formation

Cette formation est organisée en **3 modules** :

### Module 1 : Architecture GKE
:material-kubernetes: **Les Fondamentaux** | :material-clock-outline: ~60 min

- Architecture Master / Nodes
- Clusters Zonaux vs Régionaux
- VPC-Native et IP Aliasing
- Node Pools et Machine Types

[:octicons-arrow-right-24: Accéder au Module 1](01-architecture.md)

---

### Module 2 : Sécurité & IAM
:material-shield-lock: **Le Standard Worldline** | :material-clock-outline: ~75 min

- GCP IAM vs Kubernetes RBAC
- Workload Identity (zero JSON keys)
- Private Clusters
- Network Policies

[:octicons-arrow-right-24: Accéder au Module 2](02-security-iam.md)

---

### Module 3 : Storage & Networking
:material-database: **Persistance & Exposition** | :material-clock-outline: ~60 min

- StorageClasses (pd-standard, pd-ssd)
- Persistent Volume Claims
- Services et Ingress
- GCE Ingress vs Nginx Ingress

[:octicons-arrow-right-24: Accéder au Module 3](03-storage-networking.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **Kubernetes de base** : Pods, Deployments, Services, ConfigMaps
    - **Docker** : Build, run, registries
    - **GCP Fundamentals** : Projets, IAM, VPC, Service Accounts
    - **CLI** : `kubectl` et `gcloud` installés

### Environnement de Lab

=== "Installation gcloud CLI"

    ```bash
    # macOS
    brew install --cask google-cloud-sdk

    # Linux (Debian/Ubuntu)
    echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee -a /etc/apt/sources.list.d/google-cloud-sdk.list
    curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key --keyring /usr/share/keyrings/cloud.google.gpg add -
    sudo apt update && sudo apt install google-cloud-cli

    # Windows
    # Télécharger depuis https://cloud.google.com/sdk/docs/install

    # Initialisation
    gcloud init
    gcloud auth login
    ```

=== "Installation kubectl"

    ```bash
    # Via gcloud (recommandé)
    gcloud components install kubectl

    # Vérifier
    kubectl version --client
    ```

=== "Configuration Projet"

    ```bash
    # Définir le projet par défaut
    gcloud config set project YOUR_PROJECT_ID

    # Définir la région par défaut
    gcloud config set compute/region europe-west1

    # Vérifier la configuration
    gcloud config list
    ```

---

## Architecture de Référence Worldline

```
┌─────────────────────────────────────────────────────────────────┐
│              ARCHITECTURE GKE WORLDLINE                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    VPC Worldline                        │   │
│   │   ┌─────────────────────────────────────────────────┐   │   │
│   │   │            GKE Private Cluster                  │   │   │
│   │   │                                                 │   │   │
│   │   │   ┌─────────────┐    ┌─────────────────────┐   │   │   │
│   │   │   │ Control     │    │    Node Pool        │   │   │   │
│   │   │   │ Plane       │    │    (Private IPs)    │   │   │   │
│   │   │   │ (Managed)   │    │                     │   │   │   │
│   │   │   │             │    │  ┌─────┐ ┌─────┐   │   │   │   │
│   │   │   │  • API      │◄──►│  │ Pod │ │ Pod │   │   │   │   │
│   │   │   │  • etcd     │    │  └─────┘ └─────┘   │   │   │   │
│   │   │   │  • sched    │    │                     │   │   │   │
│   │   │   └─────────────┘    └─────────────────────┘   │   │   │
│   │   │                                                 │   │   │
│   │   └─────────────────────────────────────────────────┘   │   │
│   │                         │                               │   │
│   │                         │ Private Service Connect       │   │
│   │                         ▼                               │   │
│   │   ┌─────────────────────────────────────────────────┐   │   │
│   │   │              Services GCP                       │   │   │
│   │   │  Cloud SQL │ Cloud Storage │ Pub/Sub │ etc.    │   │   │
│   │   └─────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Ressources Complémentaires

### Documentation Officielle

- :material-link: [GKE Documentation](https://cloud.google.com/kubernetes-engine/docs)
- :material-link: [GKE Best Practices](https://cloud.google.com/kubernetes-engine/docs/best-practices)
- :material-link: [GKE Security Overview](https://cloud.google.com/kubernetes-engine/docs/concepts/security-overview)

### Outils Essentiels

| Outil | Usage | Installation |
|-------|-------|--------------|
| `gcloud` | CLI Google Cloud | `brew install google-cloud-sdk` |
| `kubectl` | CLI Kubernetes | `gcloud components install kubectl` |
| `kubectx` | Changer de contexte rapidement | `brew install kubectx` |
| `k9s` | Interface TUI pour K8s | `brew install k9s` |
| `stern` | Logs multi-pods | `brew install stern` |

---

!!! quote "Philosophie Cloud-Native"
    *"Cattle, not pets."*

    En cloud-native, les nodes sont du bétail (cattle), pas des animaux de compagnie (pets). Ils peuvent être remplacés à tout moment. Concevez vos applications en conséquence.

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Cloud Team
