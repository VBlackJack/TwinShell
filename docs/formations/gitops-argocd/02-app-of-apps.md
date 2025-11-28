# Module 2 : App of Apps

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre le problème de la gestion à l'échelle
- :material-check: Implémenter le pattern "App of Apps"
- :material-check: Créer des Applications pointant vers des Helm Charts
- :material-check: Utiliser les ApplicationSets pour l'automatisation

---

## 1. Le Problème : Gérer 50+ Microservices

### 1.1 L'Approche Naïve

```
┌─────────────────────────────────────────────────────────────────┐
│              L'ENFER DE LA GESTION MANUELLE                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Situation : 50 microservices à déployer sur 3 environnements  │
│                                                                 │
│   Approche manuelle :                                           │
│   ───────────────────                                           │
│                                                                 │
│   $ argocd app create frontend-dev ...                          │
│   $ argocd app create frontend-staging ...                      │
│   $ argocd app create frontend-prod ...                         │
│   $ argocd app create backend-dev ...                           │
│   $ argocd app create backend-staging ...                       │
│   $ argocd app create backend-prod ...                          │
│   $ argocd app create auth-dev ...                              │
│   ... (x 150 commandes) ...                                     │
│                                                                 │
│   PROBLÈMES :                                                   │
│   ──────────                                                    │
│   ❌ 150 Applications à créer manuellement                      │
│   ❌ Pas de version control des Applications elles-mêmes        │
│   ❌ Inconsistance entre environnements                         │
│   ❌ Ajout d'un nouveau service = 3 nouvelles Applications      │
│   ❌ Modification d'une policy = 150 updates                    │
│   ❌ "Qui a créé cette Application ? Quand ?"                   │
│                                                                 │
│   💀 CE N'EST PAS SCALABLE                                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Le Paradoxe

> "On utilise GitOps pour versionner nos déploiements... mais les Applications ArgoCD elles-mêmes ne sont pas versionnées ?"

**Solution** : Les Applications ArgoCD sont des ressources Kubernetes. Elles peuvent donc être stockées dans Git et déployées... par ArgoCD !

---

## 2. Le Pattern "App of Apps"

### 2.1 Concept

```
┌─────────────────────────────────────────────────────────────────┐
│              PATTERN APP OF APPS                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    ROOT APPLICATION                     │   │
│   │                    "apps-of-apps"                       │   │
│   │                                                         │   │
│   │   Source: gitops-manifests/argocd/                      │   │
│   │   Path: applications/                                   │   │
│   │                                                         │   │
│   └────────────────────────┬────────────────────────────────┘   │
│                            │                                    │
│                            │ Déploie des Applications           │
│                            │                                    │
│           ┌────────────────┼────────────────┐                   │
│           │                │                │                   │
│           ▼                ▼                ▼                   │
│   ┌───────────────┐ ┌───────────────┐ ┌───────────────┐        │
│   │  Application  │ │  Application  │ │  Application  │        │
│   │   frontend    │ │   backend     │ │   database    │        │
│   └───────┬───────┘ └───────┬───────┘ └───────┬───────┘        │
│           │                 │                 │                 │
│           │ Déploie         │ Déploie         │ Déploie         │
│           │ des Pods        │ des Pods        │ des Pods        │
│           ▼                 ▼                 ▼                 │
│   ┌───────────────┐ ┌───────────────┐ ┌───────────────┐        │
│   │  Deployment   │ │  Deployment   │ │  StatefulSet  │        │
│   │  Service      │ │  Service      │ │  Service      │        │
│   │  Ingress      │ │  ConfigMap    │ │  PVC          │        │
│   └───────────────┘ └───────────────┘ └───────────────┘        │
│                                                                 │
│   HIÉRARCHIE :                                                  │
│   • Niveau 0 : Root App (gère les Applications)                 │
│   • Niveau 1 : Applications (gèrent les workloads)              │
│   • Niveau 2 : Workloads (Pods, Services, etc.)                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Structure du Repository GitOps

```
gitops-manifests/
│
├── argocd/                          # Configuration ArgoCD
│   │
│   ├── root-app.yaml                # L'Application Root (App of Apps)
│   │
│   └── applications/                # Applications enfants
│       ├── frontend.yaml            # Application CRD
│       ├── backend.yaml             # Application CRD
│       ├── auth-service.yaml        # Application CRD
│       ├── payment-service.yaml     # Application CRD
│       └── ...                      # 50+ Applications
│
├── apps/                            # Manifests des applications
│   │
│   ├── frontend/
│   │   ├── base/                    # Kustomize base
│   │   │   ├── deployment.yaml
│   │   │   ├── service.yaml
│   │   │   └── kustomization.yaml
│   │   └── overlays/
│   │       ├── dev/
│   │       ├── staging/
│   │       └── prod/
│   │
│   ├── backend/
│   │   └── ...
│   │
│   └── database/
│       └── ...
│
└── charts/                          # Helm Charts customs
    ├── common-app/
    └── common-job/
```

---

## 3. Implémentation Complète

### 3.1 L'Application Root

```yaml
# argocd/root-app.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: root-apps
  namespace: argocd
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: default

  source:
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: main
    path: argocd/applications  # ← Dossier contenant les Application CRDs

  destination:
    server: https://kubernetes.default.svc
    namespace: argocd  # Les Applications vivent dans le namespace argocd

  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=false
```

### 3.2 Application Enfant (Plain YAML / Kustomize)

```yaml
# argocd/applications/frontend.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: frontend
  namespace: argocd
  labels:
    team: platform
    env: prod
  annotations:
    notifications.argoproj.io/subscribe.on-sync-failed.slack: alerts
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: production

  source:
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: main
    path: apps/frontend/overlays/prod

  destination:
    server: https://kubernetes.default.svc
    namespace: frontend

  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
      - PruneLast=true
    retry:
      limit: 3
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 1m
```

### 3.3 Application avec Helm Chart

```yaml
# argocd/applications/backend.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: backend
  namespace: argocd
  labels:
    team: backend
    env: prod
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: production

  source:
    # Chart depuis un repo Git
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: main
    path: charts/backend

    helm:
      # Fichiers de values (ordre important)
      valueFiles:
        - values.yaml
        - values-prod.yaml

      # Override de paramètres spécifiques
      parameters:
        - name: image.tag
          value: "v2.3.1"
        - name: replicaCount
          value: "5"

      # Ignorer les CRDs (si déjà installés)
      skipCrds: false

  destination:
    server: https://kubernetes.default.svc
    namespace: backend

  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
```

### 3.4 Application avec Helm Chart depuis Registry

```yaml
# argocd/applications/nginx-ingress.yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: nginx-ingress
  namespace: argocd
  finalizers:
    - resources-finalizer.argocd.argoproj.io
spec:
  project: infrastructure

  source:
    # Chart depuis un Helm Registry
    repoURL: https://kubernetes.github.io/ingress-nginx
    chart: ingress-nginx
    targetRevision: 4.8.3  # Version du chart

    helm:
      releaseName: nginx-ingress
      values: |
        controller:
          replicaCount: 3
          service:
            type: LoadBalancer
            loadBalancerIP: 34.78.123.100
          resources:
            requests:
              cpu: 100m
              memory: 128Mi
            limits:
              cpu: 500m
              memory: 256Mi
          metrics:
            enabled: true
            serviceMonitor:
              enabled: true

  destination:
    server: https://kubernetes.default.svc
    namespace: ingress-nginx

  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
```

---

## 4. ApplicationSets : L'Automatisation Ultime

### 4.1 Le Problème Restant

Même avec App of Apps, créer 50 fichiers `frontend-dev.yaml`, `frontend-staging.yaml`, `frontend-prod.yaml`... reste fastidieux.

**Solution** : **ApplicationSet** génère automatiquement des Applications à partir de templates.

### 4.2 Générateur List

```yaml
# argocd/applicationsets/environments.yaml
apiVersion: argoproj.io/v1alpha1
kind: ApplicationSet
metadata:
  name: frontend-environments
  namespace: argocd
spec:
  generators:
    - list:
        elements:
          - env: dev
            cluster: https://dev-cluster.example.com
            replicas: "1"
          - env: staging
            cluster: https://staging-cluster.example.com
            replicas: "2"
          - env: prod
            cluster: https://kubernetes.default.svc
            replicas: "5"

  template:
    metadata:
      name: "frontend-{{env}}"
      namespace: argocd
      labels:
        app: frontend
        env: "{{env}}"
    spec:
      project: "{{env}}"
      source:
        repoURL: https://github.com/worldline/gitops-manifests.git
        targetRevision: main
        path: "apps/frontend/overlays/{{env}}"
      destination:
        server: "{{cluster}}"
        namespace: frontend
      syncPolicy:
        automated:
          prune: true
          selfHeal: true
        syncOptions:
          - CreateNamespace=true
```

### 4.3 Générateur Git Directory

```yaml
# argocd/applicationsets/all-apps.yaml
apiVersion: argoproj.io/v1alpha1
kind: ApplicationSet
metadata:
  name: all-applications
  namespace: argocd
spec:
  generators:
    # Génère une Application pour chaque dossier dans apps/
    - git:
        repoURL: https://github.com/worldline/gitops-manifests.git
        revision: main
        directories:
          - path: "apps/*/overlays/prod"

  template:
    metadata:
      # {{path[1]}} = nom du dossier (frontend, backend, etc.)
      name: "{{path[1]}}"
      namespace: argocd
    spec:
      project: production
      source:
        repoURL: https://github.com/worldline/gitops-manifests.git
        targetRevision: main
        path: "{{path}}"
      destination:
        server: https://kubernetes.default.svc
        namespace: "{{path[1]}}"
      syncPolicy:
        automated:
          prune: true
          selfHeal: true
        syncOptions:
          - CreateNamespace=true
```

### 4.4 Générateur Matrix (Combinaisons)

```yaml
# argocd/applicationsets/matrix.yaml
apiVersion: argoproj.io/v1alpha1
kind: ApplicationSet
metadata:
  name: all-apps-all-envs
  namespace: argocd
spec:
  generators:
    # Produit cartésien : apps × environments
    - matrix:
        generators:
          # Générateur 1 : Liste des apps
          - git:
              repoURL: https://github.com/worldline/gitops-manifests.git
              revision: main
              directories:
                - path: "apps/*"
          # Générateur 2 : Liste des environnements
          - list:
              elements:
                - env: dev
                  cluster: https://dev.example.com
                - env: staging
                  cluster: https://staging.example.com
                - env: prod
                  cluster: https://kubernetes.default.svc

  template:
    metadata:
      name: "{{path.basename}}-{{env}}"
      namespace: argocd
      labels:
        app: "{{path.basename}}"
        env: "{{env}}"
    spec:
      project: "{{env}}"
      source:
        repoURL: https://github.com/worldline/gitops-manifests.git
        targetRevision: main
        path: "{{path}}/overlays/{{env}}"
      destination:
        server: "{{cluster}}"
        namespace: "{{path.basename}}"
      syncPolicy:
        automated:
          prune: true
          selfHeal: true
```

**Résultat** : 50 apps × 3 envs = **150 Applications générées automatiquement** !

---

## 5. Projets ArgoCD : Isolation et Sécurité

### 5.1 Concept

Les **Projects** ArgoCD permettent d'isoler les équipes et de restreindre les droits :

```yaml
# argocd/projects/production.yaml
apiVersion: argoproj.io/v1alpha1
kind: AppProject
metadata:
  name: production
  namespace: argocd
spec:
  description: "Applications de production"

  # Repos autorisés
  sourceRepos:
    - https://github.com/worldline/gitops-manifests.git
    - https://github.com/worldline/helm-charts.git

  # Clusters autorisés
  destinations:
    - namespace: "*"
      server: https://kubernetes.default.svc
    - namespace: "*"
      server: https://prod-cluster.example.com

  # Namespaces interdits
  namespaceResourceBlacklist:
    - group: ""
      kind: Namespace
    - group: rbac.authorization.k8s.io
      kind: ClusterRole

  # Types de ressources autorisés
  clusterResourceWhitelist:
    - group: ""
      kind: Namespace
    - group: networking.k8s.io
      kind: Ingress

  # Rôles RBAC du projet
  roles:
    - name: developer
      description: "Développeurs - sync uniquement"
      policies:
        - p, proj:production:developer, applications, get, production/*, allow
        - p, proj:production:developer, applications, sync, production/*, allow
      groups:
        - worldline-developers

    - name: admin
      description: "Admins - tous les droits"
      policies:
        - p, proj:production:admin, applications, *, production/*, allow
      groups:
        - worldline-platform-team
```

---

## 6. Bonnes Pratiques

### 6.1 Checklist App of Apps

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST APP OF APPS                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   STRUCTURE                                                     │
│   ─────────                                                     │
│   ☑ Root App déployée manuellement (bootstrap)                  │
│   ☑ Applications enfants versionnées dans Git                   │
│   ☑ Un dossier applications/ dédié aux CRDs                     │
│   ☑ Séparation apps/ (workloads) et argocd/ (config)           │
│                                                                 │
│   SÉCURITÉ                                                      │
│   ────────                                                      │
│   ☑ Projects pour isoler les équipes                            │
│   ☑ sourceRepos restrictif par projet                           │
│   ☑ destinations limitées                                       │
│   ☑ Finalizers pour le nettoyage                                │
│                                                                 │
│   AUTOMATISATION                                                │
│   ─────────────                                                 │
│   ☑ ApplicationSets pour multi-env                              │
│   ☑ Labels cohérents (team, env, app)                           │
│   ☑ Notifications configurées                                   │
│                                                                 │
│   HELM                                                          │
│   ────                                                          │
│   ☑ Version du chart épinglée (targetRevision)                  │
│   ☑ Values séparées par environnement                           │
│   ☑ skipCrds si CRDs gérées ailleurs                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Anti-Patterns à Éviter

| Anti-Pattern | Problème | Solution |
|--------------|----------|----------|
| `targetRevision: HEAD` | Version flottante, non reproductible | Utiliser un tag ou SHA |
| Applications sans Project | Pas d'isolation | Créer des Projects par équipe/env |
| Secrets dans le repo GitOps | Fuite de secrets | Sealed Secrets ou External Secrets |
| Sync manuel sur tout | Pas d'automatisation | Auto-sync en dev/staging |
| Pas de finalizer | Ressources orphelines après delete | Toujours ajouter le finalizer |

---

## 7. Commandes de Référence

```bash
# === APP OF APPS ===
# Bootstrap : créer la root app manuellement
kubectl apply -f argocd/root-app.yaml

# Voir les applications générées
argocd app list

# Sync la root app (cascade vers les enfants)
argocd app sync root-apps

# === APPLICATIONSETS ===
# Lister les ApplicationSets
kubectl get applicationsets -n argocd

# Voir les applications générées par un ApplicationSet
argocd app list -l app.kubernetes.io/instance=all-applications

# === PROJECTS ===
# Lister les projets
argocd proj list

# Voir les détails d'un projet
argocd proj get production

# Voir les applications d'un projet
argocd app list -p production

# === DEBUG ===
# Voir pourquoi une app n'est pas créée (ApplicationSet)
kubectl describe applicationset all-applications -n argocd

# Voir les events
kubectl get events -n argocd --sort-by='.lastTimestamp'
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi la Root App est-elle créée manuellement ?"
    **Réponse :**

    C'est le problème de l'œuf et de la poule :

    - ArgoCD gère les Applications via Git
    - Mais il faut une Application pour lire Git
    - Cette première Application (Root) doit être créée manuellement

    **Bootstrap** : `kubectl apply -f root-app.yaml`

    Ensuite, la Root App gère toutes les autres Applications, y compris ses propres mises à jour (si vous modifiez root-app.yaml dans Git, ArgoCD se mettra à jour).

??? question "Question 2 : Quelle est la différence entre App of Apps et ApplicationSet ?"
    **Réponse :**

    | Aspect | App of Apps | ApplicationSet |
    |--------|-------------|----------------|
    | **Définition** | Applications manuelles dans Git | Template + générateur |
    | **Flexibilité** | Totale (chaque app unique) | Template commun |
    | **Maintenance** | 1 fichier par app | 1 fichier pour N apps |
    | **Cas d'usage** | Apps hétérogènes | Apps similaires (multi-env) |

    **Recommandation** : Combiner les deux. ApplicationSet pour les patterns répétitifs, App of Apps pour les configurations uniques.

??? question "Question 3 : Comment ajouter un nouveau microservice au système ?"
    **Réponse :**

    Avec un ApplicationSet (générateur git directories) :

    1. Créer le dossier `apps/nouveau-service/`
    2. Ajouter les overlays `dev/`, `staging/`, `prod/`
    3. Commit + Push

    ArgoCD détecte automatiquement le nouveau dossier et crée les Applications correspondantes. **Zero configuration ArgoCD nécessaire.**

---

## Prochaine Étape

Vous savez maintenant gérer des centaines d'applications. Apprenez à contrôler finement leur synchronisation.

[:octicons-arrow-right-24: Module 3 : Stratégies de Sync](03-sync-strategies.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Avancé
