# Formation : GitOps avec ArgoCD

## Introduction

**ClickOps is dead. Long live GitOps.**

Le GitOps représente l'évolution naturelle du DevOps : **Git devient la source unique de vérité** pour l'infrastructure ET les applications. Plus de `kubectl apply` manuel, plus de déploiements depuis un laptop, plus de "ça marchait sur ma machine".

!!! success "Ce que vous allez apprendre"
    Cette formation vous transformera d'un opérateur Kubernetes classique en un **GitOps Practitioner** capable de :

    - Déployer des centaines de microservices de manière déclarative
    - Auditer chaque changement via l'historique Git
    - Rollback en 30 secondes avec `git revert`
    - Dormir tranquille pendant les déploiements en production

---

## Le Problème : Push-Based Deployments

### L'Architecture Traditionnelle (CI/CD Push)

```
┌─────────────────────────────────────────────────────────────────┐
│              ARCHITECTURE PUSH (ANTI-PATTERN)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐      │
│   │ Développeur │────►│   GitLab    │────►│  CI Runner  │      │
│   │             │     │    Repo     │     │             │      │
│   └─────────────┘     └─────────────┘     └──────┬──────┘      │
│                                                  │              │
│                                                  │ kubectl      │
│                                                  │ apply        │
│                                                  │              │
│                                                  ▼              │
│                                           ┌─────────────┐       │
│                                           │             │       │
│                                           │  Cluster    │       │
│                                           │  Kubernetes │       │
│                                           │             │       │
│                                           └─────────────┘       │
│                                                                 │
│   ⚠️  PROBLÈMES DE SÉCURITÉ :                                   │
│   ─────────────────────────                                     │
│   • Le Runner a un kubeconfig avec accès cluster-admin         │
│   • Ce kubeconfig est stocké dans les variables CI              │
│   • Tout développeur avec accès aux variables CI = admin K8s    │
│   • Si le Runner est compromis = Game Over                      │
│   • Les credentials sortent du cluster (exposition externe)     │
│                                                                 │
│   ⚠️  PROBLÈMES OPÉRATIONNELS :                                 │
│   ──────────────────────────                                    │
│   • Pas de visibilité sur l'état réel du cluster               │
│   • Drift possible (modifs manuelles non trackées)              │
│   • Rollback = relancer un vieux pipeline (lent, risqué)        │
│   • "Ça a marché" != "C'est déployé correctement"               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Le Risque Concret

```yaml
# .gitlab-ci.yml typique (PROBLÉMATIQUE)
deploy:
  stage: deploy
  script:
    - kubectl apply -f manifests/
  variables:
    KUBECONFIG: $KUBE_CONFIG  # ← Secret stocké dans GitLab
  only:
    - main
```

!!! danger "Pourquoi c'est dangereux"
    1. **Surface d'attaque élargie** : Le kubeconfig (avec des droits cluster-admin) est stocké dans GitLab/Jenkins
    2. **Lateral Movement** : Un attaquant qui compromet le CI peut pivoter vers le cluster
    3. **Credentials long-lived** : Le token ne expire souvent jamais
    4. **Pas d'audit Kubernetes** : Les actions sont faites par un Service Account générique
    5. **Blast radius maximum** : Un runner compromis = tous les clusters accessibles

---

## La Solution : Pull-Based Deployments (GitOps)

### L'Opérateur GitOps

```
┌─────────────────────────────────────────────────────────────────┐
│              ARCHITECTURE GITOPS (PULL-BASED)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────┐                          │
│   │ Développeur │────►│   GitLab    │                          │
│   │             │     │    Repo     │                          │
│   └─────────────┘     └──────┬──────┘                          │
│                              │                                  │
│                              │ Webhook / Poll                   │
│                              │                                  │
│   ┌──────────────────────────┼──────────────────────────────┐   │
│   │         CLUSTER KUBERNETES                              │   │
│   │                          │                              │   │
│   │                          ▼                              │   │
│   │                   ┌─────────────┐                       │   │
│   │                   │             │                       │   │
│   │                   │   ArgoCD    │◄──── PULL (pas PUSH)  │   │
│   │                   │  Operator   │                       │   │
│   │                   │             │                       │   │
│   │                   └──────┬──────┘                       │   │
│   │                          │                              │   │
│   │            ┌─────────────┼─────────────┐               │   │
│   │            │             │             │               │   │
│   │            ▼             ▼             ▼               │   │
│   │      ┌──────────┐  ┌──────────┐  ┌──────────┐         │   │
│   │      │ App API  │  │ App Web  │  │ App DB   │         │   │
│   │      └──────────┘  └──────────┘  └──────────┘         │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ✅ AVANTAGES SÉCURITÉ :                                       │
│   ─────────────────────                                         │
│   • Aucun credential externe (tout reste dans le cluster)      │
│   • ArgoCD a uniquement les droits nécessaires (RBAC)          │
│   • Audit complet via Git history                               │
│   • Pas d'accès kubectl depuis l'extérieur                     │
│                                                                 │
│   ✅ AVANTAGES OPÉRATIONNELS :                                  │
│   ────────────────────────                                      │
│   • État désiré = Git, État réel = visible dans ArgoCD         │
│   • Drift détecté automatiquement                               │
│   • Rollback = git revert + push                                │
│   • Self-healing : retour automatique à l'état Git              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Le Principe GitOps en 4 Points

| Principe | Description |
|----------|-------------|
| **Déclaratif** | L'état désiré est décrit dans Git (YAML, Helm, Kustomize) |
| **Versionné** | Tout changement passe par un commit Git (audit trail) |
| **Pull Automatique** | L'opérateur détecte et applique les changements |
| **Réconciliation Continue** | L'état réel converge vers l'état désiré |

---

## Pourquoi ArgoCD ?

### Comparaison des Outils GitOps

| Critère | ArgoCD | Flux CD | Jenkins X |
|---------|--------|---------|-----------|
| **UI Web** | Excellente | Basique | Moyenne |
| **Multi-cluster** | Natif | Via Kustomize | Complexe |
| **Courbe d'apprentissage** | Faible | Moyenne | Élevée |
| **CRD Kubernetes** | Application | GitRepository + Kustomization | Nombreux |
| **RBAC intégré** | Oui (Dex/OIDC) | Via K8s RBAC | Oui |
| **Rollback** | 1 clic | Git revert | Git revert |
| **Adoption** | Leader (CNCF Graduated) | Populaire (CNCF) | En déclin |

!!! success "Choix Worldline : ArgoCD"
    ArgoCD est le standard de facto pour le GitOps en entreprise :

    - **CNCF Graduated** (même niveau que Kubernetes)
    - **UI intuitive** pour les équipes Ops
    - **SSO intégré** (OIDC, LDAP, SAML)
    - **Multi-tenant** natif
    - **Écosystème riche** (Image Updater, Rollouts, Notifications)

---

## Syllabus de la Formation

Cette formation est organisée en **3 modules** :

### Module 1 : Architecture ArgoCD
:material-view-dashboard: **Les Composants** | :material-clock-outline: ~45 min

- Application Controller, Repo Server, API Server
- Le Dashboard Web
- Flux de synchronisation

[:octicons-arrow-right-24: Accéder au Module 1](01-architecture.md)

---

### Module 2 : App of Apps
:material-apps: **Industrialisation** | :material-clock-outline: ~60 min

- Pattern "App of Apps"
- Gestion de centaines de microservices
- ApplicationSets

[:octicons-arrow-right-24: Accéder au Module 2](02-app-of-apps.md)

---

### Module 3 : Stratégies de Sync
:material-sync: **Opérations** | :material-clock-outline: ~60 min

- Auto-Sync vs Manual
- Sync Waves (ordre de déploiement)
- Self-Healing

[:octicons-arrow-right-24: Accéder au Module 3](03-sync-strategies.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **Kubernetes** : Deployments, Services, Namespaces, RBAC
    - **Git** : Branches, commits, pull requests
    - **YAML** : Syntaxe, manifests Kubernetes
    - **Helm** (recommandé) : Charts, values, templates
    - **Formation GKE** : Cluster opérationnel

### Installation ArgoCD

=== "Installation Rapide"

    ```bash
    # Créer le namespace
    kubectl create namespace argocd

    # Installer ArgoCD
    kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

    # Attendre que les pods soient prêts
    kubectl wait --for=condition=Ready pods --all -n argocd --timeout=300s

    # Récupérer le mot de passe admin initial
    kubectl -n argocd get secret argocd-initial-admin-secret \
        -o jsonpath="{.data.password}" | base64 -d
    ```

=== "Installation Production (Helm)"

    ```bash
    # Ajouter le repo Helm
    helm repo add argo https://argoproj.github.io/argo-helm
    helm repo update

    # Installer avec des valeurs personnalisées
    helm install argocd argo/argo-cd \
        --namespace argocd \
        --create-namespace \
        --values values-prod.yaml
    ```

    ```yaml
    # values-prod.yaml
    server:
      replicas: 2
      ingress:
        enabled: true
        ingressClassName: nginx
        hosts:
          - argocd.worldline.internal
        tls:
          - secretName: argocd-tls
            hosts:
              - argocd.worldline.internal

    controller:
      replicas: 2

    repoServer:
      replicas: 2

    dex:
      enabled: true

    configs:
      cm:
        url: https://argocd.worldline.internal
        dex.config: |
          connectors:
            - type: oidc
              id: google
              name: Google
              config:
                issuer: https://accounts.google.com
                clientID: $GOOGLE_CLIENT_ID
                clientSecret: $GOOGLE_CLIENT_SECRET
    ```

=== "CLI ArgoCD"

    ```bash
    # macOS
    brew install argocd

    # Linux
    curl -sSL -o argocd https://github.com/argoproj/argo-cd/releases/latest/download/argocd-linux-amd64
    chmod +x argocd
    sudo mv argocd /usr/local/bin/

    # Login
    argocd login argocd.worldline.internal --sso

    # Vérifier
    argocd version
    ```

---

## GitOps Workflow Worldline

```
┌─────────────────────────────────────────────────────────────────┐
│              WORKFLOW GITOPS WORLDLINE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    DÉVELOPPEUR                          │   │
│   │                                                         │   │
│   │   1. Modifie le code applicatif                         │   │
│   │   2. Push sur feature branch                            │   │
│   │   3. CI build & test                                    │   │
│   │   4. CI push image vers Artifact Registry               │   │
│   │   5. CI met à jour le tag image dans le repo GitOps     │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    REPOS GIT                            │   │
│   │                                                         │   │
│   │   app-code/          (Code applicatif)                  │   │
│   │   └── src/, Dockerfile, tests...                        │   │
│   │                                                         │   │
│   │   gitops-manifests/   (État désiré K8s)                 │   │
│   │   ├── apps/                                             │   │
│   │   │   ├── frontend/                                     │   │
│   │   │   │   ├── base/                                     │   │
│   │   │   │   └── overlays/                                 │   │
│   │   │   │       ├── dev/                                  │   │
│   │   │   │       ├── staging/                              │   │
│   │   │   │       └── prod/                                 │   │
│   │   │   └── backend/                                      │   │
│   │   └── argocd/                                           │   │
│   │       └── applications.yaml                             │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         │ Poll / Webhook                        │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    ARGOCD                               │   │
│   │                                                         │   │
│   │   Détecte : "Le repo Git a changé !"                    │   │
│   │   Compare : État Git vs État Cluster                    │   │
│   │   Action : Sync (apply les différences)                 │   │
│   │   Vérifie : Health checks passent                       │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    CLUSTERS GKE                         │   │
│   │                                                         │   │
│   │   ┌─────────┐   ┌─────────┐   ┌─────────┐              │   │
│   │   │   DEV   │   │ STAGING │   │  PROD   │              │   │
│   │   │         │   │         │   │         │              │   │
│   │   │Auto-Sync│   │Auto-Sync│   │ Manual  │              │   │
│   │   └─────────┘   └─────────┘   └─────────┘              │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Les Commandements du GitOps

!!! danger "Les 5 Commandements GitOps"

    1. **Tu ne feras point de `kubectl apply` manuel**
       → Tout passe par Git

    2. **Tu ne modifieras point les ressources directement dans le cluster**
       → ArgoCD les remettra à l'état Git (Self-Heal)

    3. **Tu sépareras le code applicatif des manifests de déploiement**
       → Deux repos distincts (ou deux dossiers)

    4. **Tu utiliseras des tags d'images immutables**
       → Pas de `latest`, utilisez `v1.2.3` ou `sha256:...`

    5. **Tu revieweras les changements de manifests comme du code**
       → PR review obligatoire sur le repo GitOps

---

## Ressources Complémentaires

### Documentation Officielle

- :material-link: [ArgoCD Documentation](https://argo-cd.readthedocs.io/)
- :material-link: [ArgoCD Best Practices](https://argo-cd.readthedocs.io/en/stable/user-guide/best_practices/)
- :material-link: [GitOps Principles](https://opengitops.dev/)

### Outils de l'Écosystème

| Outil | Usage | Lien |
|-------|-------|------|
| `argocd` | CLI ArgoCD | [Installation](https://argo-cd.readthedocs.io/en/stable/cli_installation/) |
| `Argo Rollouts` | Canary, Blue-Green deployments | [Docs](https://argoproj.github.io/argo-rollouts/) |
| `Argo Image Updater` | MAJ automatique des tags d'images | [Docs](https://argocd-image-updater.readthedocs.io/) |
| `Argo Notifications` | Alertes Slack/Teams/Email | [Docs](https://argocd-notifications.readthedocs.io/) |
| `Kustomize` | Overlays pour multi-env | [Docs](https://kustomize.io/) |
| `Helm` | Packaging d'applications | [Docs](https://helm.sh/) |

---

!!! quote "Philosophie GitOps"
    *"Git is the source of truth for both infrastructure and applications. Operations are performed by making commits to a Git repository, which an automated process then applies to the infrastructure."*

    — Weaveworks (Inventeurs du terme GitOps)

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Cloud Team
