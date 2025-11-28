# Module 3 : Stratégies de Sync

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Choisir entre Auto-Sync et Manual Sync selon l'environnement
- :material-check: Implémenter des Sync Waves pour orchestrer les déploiements
- :material-check: Configurer le Self-Healing pour corriger le drift
- :material-check: Utiliser les Sync Hooks pour des actions personnalisées

---

## 1. Auto-Sync vs Manual Sync

### 1.1 Le Dilemme

```
┌─────────────────────────────────────────────────────────────────┐
│              AUTO-SYNC vs MANUAL SYNC                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   AUTO-SYNC                                                     │
│   ─────────                                                     │
│   Git change → ArgoCD sync automatiquement                      │
│                                                                 │
│   ✅ Avantages :                                                │
│   • Déploiement immédiat après merge                            │
│   • Pas d'intervention humaine                                  │
│   • GitOps "pur" (Git = source de vérité absolue)               │
│                                                                 │
│   ❌ Risques :                                                  │
│   • Un bug mergé = déployé en prod instantanément               │
│   • Pas de fenêtre de maintenance                               │
│   • Rollback plus stressant                                     │
│                                                                 │
│   ─────────────────────────────────────────────────────────────│
│                                                                 │
│   MANUAL SYNC                                                   │
│   ───────────                                                   │
│   Git change → ArgoCD détecte → Humain valide → Sync            │
│                                                                 │
│   ✅ Avantages :                                                │
│   • Contrôle total sur le timing                                │
│   • Validation humaine avant déploiement                        │
│   • Compatible avec les Change Approvals                        │
│                                                                 │
│   ❌ Risques :                                                  │
│   • Drift si oubli de sync                                      │
│   • Bottleneck humain                                           │
│   • Git ≠ Cluster pendant une période                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Recommandation par Environnement

| Environnement | Sync Policy | Justification |
|---------------|-------------|---------------|
| **Development** | Auto-Sync + Self-Heal + Prune | Feedback loop rapide, pas de risque |
| **Staging** | Auto-Sync + Self-Heal + Prune | Proche de la prod, tests automatiques |
| **Production** | Manual Sync | Contrôle humain, fenêtres de maintenance |
| **Hotfix Prod** | Auto-Sync (temporaire) | Correction urgente, puis repasser en manual |

### 1.3 Configuration Auto-Sync

```yaml
# Application Dev : Full Auto
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: frontend-dev
  namespace: argocd
spec:
  project: development
  source:
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: develop  # Branche de dev
    path: apps/frontend/overlays/dev
  destination:
    server: https://dev-cluster.example.com
    namespace: frontend
  syncPolicy:
    automated:
      prune: true      # Supprimer les ressources absentes de Git
      selfHeal: true   # Corriger les modifications manuelles
      allowEmpty: false
    syncOptions:
      - CreateNamespace=true
```

```yaml
# Application Prod : Manual avec options
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: frontend-prod
  namespace: argocd
spec:
  project: production
  source:
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: v2.3.1  # Tag spécifique, pas de branche !
    path: apps/frontend/overlays/prod
  destination:
    server: https://kubernetes.default.svc
    namespace: frontend
  syncPolicy:
    # Pas de automated: → Manual sync requis
    syncOptions:
      - CreateNamespace=true
      - PrunePropagationPolicy=foreground
      - PruneLast=true
    retry:
      limit: 5
      backoff:
        duration: 5s
        factor: 2
        maxDuration: 3m
```

---

## 2. Sync Waves : Orchestrer l'Ordre de Déploiement

### 2.1 Le Problème

```
┌─────────────────────────────────────────────────────────────────┐
│              PROBLÈME : ORDRE DE DÉPLOIEMENT                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Situation :                                                   │
│   ───────────                                                   │
│   • L'API a besoin de la Database pour démarrer                 │
│   • Le Frontend a besoin de l'API pour fonctionner              │
│   • Le Job de migration doit s'exécuter avant l'API             │
│                                                                 │
│   Sans Sync Waves :                                             │
│   ─────────────────                                             │
│   ArgoCD déploie tout en parallèle...                           │
│                                                                 │
│   T=0   Database   → Creating...                                │
│   T=0   Migration  → Running... ❌ DB pas prête !               │
│   T=0   API        → Starting... ❌ DB pas prête !              │
│   T=0   Frontend   → Starting... ❌ API pas prête !             │
│                                                                 │
│   💀 CrashLoopBackOff partout                                   │
│                                                                 │
│   Avec Sync Waves :                                             │
│   ──────────────────                                            │
│   Wave -1 : Database (attendre Healthy)                         │
│   Wave  0 : Migration Job (attendre Completed)                  │
│   Wave  1 : API (attendre Healthy)                              │
│   Wave  2 : Frontend (attendre Healthy)                         │
│                                                                 │
│   ✅ Déploiement ordonné et fiable                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Annotation sync-wave

Les Sync Waves utilisent l'annotation `argocd.argoproj.io/sync-wave` :

```yaml
# Wave -1 : Database (déployée en premier)
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  annotations:
    argocd.argoproj.io/sync-wave: "-1"
spec:
  # ...

---
# Wave 0 : Migration (après la DB)
apiVersion: batch/v1
kind: Job
metadata:
  name: db-migration
  annotations:
    argocd.argoproj.io/sync-wave: "0"
    argocd.argoproj.io/hook: Sync
spec:
  template:
    spec:
      containers:
        - name: migrate
          image: myapp/migrations:v2.3.1
          command: ["./migrate.sh"]
      restartPolicy: Never
  backoffLimit: 3

---
# Wave 1 : API (après la migration)
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
  annotations:
    argocd.argoproj.io/sync-wave: "1"
spec:
  # ...

---
# Wave 2 : Frontend (après l'API)
apiVersion: apps/v1
kind: Deployment
metadata:
  name: frontend
  annotations:
    argocd.argoproj.io/sync-wave: "2"
spec:
  # ...
```

### 2.3 Comportement des Waves

```
┌─────────────────────────────────────────────────────────────────┐
│              EXÉCUTION DES SYNC WAVES                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Wave -1                                                       │
│   ────────                                                      │
│   ┌─────────────┐                                               │
│   │  postgres   │ → Apply → Wait for Healthy ✅                 │
│   │  (SS)       │                                               │
│   └─────────────┘                                               │
│          │                                                      │
│          ▼ (Wave -1 complete)                                   │
│                                                                 │
│   Wave 0                                                        │
│   ───────                                                       │
│   ┌─────────────┐                                               │
│   │ db-migration│ → Apply → Wait for Succeeded ✅               │
│   │  (Job)      │                                               │
│   └─────────────┘                                               │
│          │                                                      │
│          ▼ (Wave 0 complete)                                    │
│                                                                 │
│   Wave 1                                                        │
│   ───────                                                       │
│   ┌─────────────┐                                               │
│   │    api      │ → Apply → Wait for Healthy ✅                 │
│   │  (Deploy)   │                                               │
│   └─────────────┘                                               │
│          │                                                      │
│          ▼ (Wave 1 complete)                                    │
│                                                                 │
│   Wave 2                                                        │
│   ───────                                                       │
│   ┌─────────────┐                                               │
│   │  frontend   │ → Apply → Wait for Healthy ✅                 │
│   │  (Deploy)   │                                               │
│   └─────────────┘                                               │
│          │                                                      │
│          ▼                                                      │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              SYNC COMPLETE ✅                           │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   Notes :                                                       │
│   • Les ressources d'une même wave sont déployées en parallèle  │
│   • ArgoCD attend que TOUTES les ressources d'une wave soient   │
│     "Healthy" avant de passer à la wave suivante                │
│   • Les waves peuvent être négatives (-10, -1, 0, 1, 10, etc.)  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.4 Exemple Complet avec Kustomize

```yaml
# base/kustomization.yaml
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization

resources:
  - namespace.yaml
  - database/
  - migrations/
  - api/
  - frontend/

# Les annotations sync-wave sont dans chaque fichier
```

```yaml
# base/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: myapp
  annotations:
    argocd.argoproj.io/sync-wave: "-10"  # Tout premier
```

```yaml
# base/database/statefulset.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: myapp
  annotations:
    argocd.argoproj.io/sync-wave: "-5"
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
        - name: postgres
          image: postgres:15
          ports:
            - containerPort: 5432
          readinessProbe:
            exec:
              command: ["pg_isready", "-U", "postgres"]
            initialDelaySeconds: 5
            periodSeconds: 5
```

---

## 3. Self-Heal : Correction Automatique du Drift

### 3.1 Qu'est-ce que le Drift ?

```
┌─────────────────────────────────────────────────────────────────┐
│              DRIFT : L'ENNEMI DU GITOPS                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Situation :                                                   │
│   ───────────                                                   │
│   Git dit : replicas = 3                                        │
│   Cluster dit : replicas = 5                                    │
│                                                                 │
│   Comment c'est arrivé ?                                        │
│   ─────────────────────                                         │
│   • Quelqu'un a fait : kubectl scale deployment api --replicas=5│
│   • Un opérateur a modifié le HPA manuellement                  │
│   • Un script legacy a patché le deployment                     │
│   • Un admin a "fix" un problème en urgence                     │
│                                                                 │
│   Conséquences :                                                │
│   ──────────────                                                │
│   ❌ Git n'est plus la source de vérité                         │
│   ❌ Le prochain sync va "casser" le fix manuel                 │
│   ❌ Impossible de savoir l'état réel attendu                   │
│   ❌ Rollback Git ne fonctionne plus                            │
│                                                                 │
│   Solution : Self-Heal                                          │
│   ───────────────────────                                       │
│   ArgoCD détecte la modification et la REVERT automatiquement   │
│   → Retour à replicas = 3 (état Git)                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Configuration Self-Heal

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: api
  namespace: argocd
spec:
  project: production
  source:
    repoURL: https://github.com/worldline/gitops-manifests.git
    targetRevision: main
    path: apps/api/overlays/prod
  destination:
    server: https://kubernetes.default.svc
    namespace: api
  syncPolicy:
    automated:
      prune: true
      selfHeal: true  # ← CRUCIAL : corrige le drift automatiquement
    syncOptions:
      - CreateNamespace=true
```

### 3.3 Comportement du Self-Heal

```
┌─────────────────────────────────────────────────────────────────┐
│              SELF-HEAL EN ACTION                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   T=0    État Git :    replicas = 3                             │
│          État Cluster : replicas = 3                            │
│          Status : Synced ✅                                     │
│                                                                 │
│   T=1    Ops fait : kubectl scale deploy api --replicas=5       │
│          État Git :    replicas = 3                             │
│          État Cluster : replicas = 5                            │
│          Status : OutOfSync ⚠️                                  │
│                                                                 │
│   T=2    ArgoCD détecte le drift (poll ou reconciliation)       │
│          Self-Heal activé → Sync automatique                    │
│                                                                 │
│   T=3    ArgoCD applique : replicas = 3 (état Git)              │
│          État Cluster : replicas = 3                            │
│          Status : Synced ✅                                     │
│                                                                 │
│   📢 Message à l'Ops :                                          │
│   "Ta modification manuelle a été annulée. Passe par Git."      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.4 Ignorer Certains Champs

Parfois, vous voulez autoriser certains drifts (ex: HPA qui modifie replicas) :

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: api
  namespace: argocd
spec:
  # ...
  ignoreDifferences:
    # Ignorer les changements de replicas (géré par HPA)
    - group: apps
      kind: Deployment
      jsonPointers:
        - /spec/replicas

    # Ignorer certaines annotations ajoutées par des controllers
    - group: ""
      kind: Service
      jsonPointers:
        - /metadata/annotations/cloud.google.com~1neg-status

    # Ignorer les labels ajoutés par Istio
    - group: apps
      kind: Deployment
      jqPathExpressions:
        - .spec.template.metadata.labels | select(."istio.io/rev")
```

---

## 4. Sync Hooks : Actions Personnalisées

### 4.1 Types de Hooks

| Hook | Quand | Usage |
|------|-------|-------|
| **PreSync** | Avant le sync | Backup DB, notification Slack |
| **Sync** | Pendant le sync (avec wave) | Migrations |
| **PostSync** | Après sync réussi | Tests smoke, notification |
| **SyncFail** | Après sync échoué | Alerte, rollback |
| **Skip** | Jamais exécuté par ArgoCD | Resources gérées autrement |

### 4.2 Exemple : Migration Pre-Sync

```yaml
# hooks/pre-sync-backup.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: db-backup
  annotations:
    argocd.argoproj.io/hook: PreSync
    argocd.argoproj.io/hook-delete-policy: HookSucceeded
spec:
  template:
    spec:
      containers:
        - name: backup
          image: postgres:15
          command:
            - /bin/sh
            - -c
            - |
              pg_dump -h $DB_HOST -U $DB_USER $DB_NAME | \
              gzip > /backup/backup-$(date +%Y%m%d-%H%M%S).sql.gz
          envFrom:
            - secretRef:
                name: db-credentials
          volumeMounts:
            - name: backup
              mountPath: /backup
      restartPolicy: Never
      volumes:
        - name: backup
          persistentVolumeClaim:
            claimName: backup-pvc
  backoffLimit: 2
```

### 4.3 Exemple : Notification Post-Sync

```yaml
# hooks/post-sync-notify.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: notify-deployment
  annotations:
    argocd.argoproj.io/hook: PostSync
    argocd.argoproj.io/hook-delete-policy: HookSucceeded
spec:
  template:
    spec:
      containers:
        - name: notify
          image: curlimages/curl:latest
          command:
            - /bin/sh
            - -c
            - |
              curl -X POST "$SLACK_WEBHOOK" \
                -H 'Content-type: application/json' \
                -d '{
                  "text": "✅ Deployment successful!",
                  "blocks": [
                    {
                      "type": "section",
                      "text": {
                        "type": "mrkdwn",
                        "text": "*Application:* '"$APP_NAME"'\n*Version:* '"$APP_VERSION"'\n*Environment:* Production"
                      }
                    }
                  ]
                }'
          env:
            - name: SLACK_WEBHOOK
              valueFrom:
                secretKeyRef:
                  name: slack-webhook
                  key: url
            - name: APP_NAME
              value: "frontend"
            - name: APP_VERSION
              value: "v2.3.1"
      restartPolicy: Never
  backoffLimit: 1
```

### 4.4 Hook Delete Policies

```yaml
annotations:
  argocd.argoproj.io/hook-delete-policy: HookSucceeded
  # Autres options :
  # - HookSucceeded : Supprimer si le hook réussit
  # - HookFailed : Supprimer si le hook échoue
  # - BeforeHookCreation : Supprimer avant de recréer (prochain sync)
```

---

## 5. Prune : Nettoyage des Ressources Orphelines

### 5.1 Le Problème des Orphelins

```
┌─────────────────────────────────────────────────────────────────┐
│              RESSOURCES ORPHELINES                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Jour 1 : Git contient                                         │
│   ─────────────────────                                         │
│   ├── deployment.yaml (api)                                     │
│   ├── deployment.yaml (worker)  ← On supprime ce fichier       │
│   ├── service.yaml                                              │
│   └── ingress.yaml                                              │
│                                                                 │
│   Jour 2 : Git contient                                         │
│   ─────────────────────                                         │
│   ├── deployment.yaml (api)                                     │
│   ├── service.yaml                                              │
│   └── ingress.yaml                                              │
│                                                                 │
│   Sans Prune :                                                  │
│   ────────────                                                  │
│   Le deployment "worker" reste dans le cluster !                │
│   → Ressource orpheline                                         │
│   → Consomme des ressources                                     │
│   → Drift invisible                                             │
│                                                                 │
│   Avec Prune :                                                  │
│   ───────────                                                   │
│   ArgoCD supprime automatiquement le deployment "worker"        │
│   → Git = Cluster (vraiment)                                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Options de Prune

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: api
  namespace: argocd
spec:
  # ...
  syncPolicy:
    automated:
      prune: true  # Activer le prune automatique
    syncOptions:
      # Ordre de suppression
      - PruneLast=true  # Supprimer les orphelins en dernier

      # Politique de propagation
      - PrunePropagationPolicy=foreground  # Attendre la suppression effective
      # Autres : background, orphan
```

### 5.3 Protection contre le Prune

```yaml
# Protéger une ressource contre le prune
apiVersion: v1
kind: ConfigMap
metadata:
  name: critical-config
  annotations:
    argocd.argoproj.io/sync-options: Prune=false
data:
  # Cette ConfigMap ne sera JAMAIS supprimée par ArgoCD
```

---

## 6. Bonnes Pratiques Opérationnelles

### 6.1 Checklist par Environnement

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST SYNC STRATEGIES                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   DEVELOPMENT                                                   │
│   ───────────                                                   │
│   ☑ automated.prune: true                                       │
│   ☑ automated.selfHeal: true                                    │
│   ☑ targetRevision: develop (branche)                          │
│   ☑ Sync rapide (webhook GitHub/GitLab)                         │
│   ☐ Notifications : optionnelles                                │
│                                                                 │
│   STAGING                                                       │
│   ───────                                                       │
│   ☑ automated.prune: true                                       │
│   ☑ automated.selfHeal: true                                    │
│   ☑ targetRevision: main ou release/*                          │
│   ☑ Sync Waves pour l'ordre                                     │
│   ☑ Notifications : Slack channel staging                       │
│                                                                 │
│   PRODUCTION                                                    │
│   ──────────                                                    │
│   ☑ automated: false (Manual Sync)                              │
│   ☑ targetRevision: tag (v2.3.1)                               │
│   ☑ Sync Waves obligatoires                                     │
│   ☑ PreSync hooks (backup, healthcheck)                         │
│   ☑ PostSync hooks (smoke tests, notification)                  │
│   ☑ SyncFail hooks (alerte PagerDuty)                           │
│   ☑ Prune: true mais avec PruneLast                             │
│   ☑ ignoreDifferences pour HPA                                  │
│   ☑ Notifications : #prod-deployments + PagerDuty               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Workflow de Déploiement Production

```
┌─────────────────────────────────────────────────────────────────┐
│              WORKFLOW DÉPLOIEMENT PRODUCTION                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. PRÉPARATION                                                │
│   ──────────────                                                │
│   • Créer le tag Git (v2.3.1)                                   │
│   • Mettre à jour le targetRevision dans l'Application          │
│   • Merger la PR dans le repo GitOps                            │
│                                                                 │
│   2. VÉRIFICATION                                               │
│   ───────────────                                               │
│   • ArgoCD détecte : Status = OutOfSync                         │
│   • Revoir le Diff dans l'UI ArgoCD                             │
│   • Vérifier les Sync Waves                                     │
│                                                                 │
│   3. DÉPLOIEMENT                                                │
│   ──────────────                                                │
│   • Cliquer "Sync" dans l'UI (ou argocd app sync)               │
│   • ArgoCD exécute :                                            │
│     - PreSync : Backup DB ✅                                    │
│     - Wave -1 : Database ✅                                     │
│     - Wave 0 : Migrations ✅                                    │
│     - Wave 1 : API ✅                                           │
│     - Wave 2 : Frontend ✅                                      │
│     - PostSync : Smoke tests ✅                                 │
│     - PostSync : Notification Slack ✅                          │
│                                                                 │
│   4. VALIDATION                                                 │
│   ─────────────                                                 │
│   • Vérifier Health = Healthy                                   │
│   • Vérifier les métriques (Grafana)                            │
│   • Valider le ticket de change                                 │
│                                                                 │
│   5. ROLLBACK (si nécessaire)                                   │
│   ────────────────────────────                                  │
│   Option A : argocd app rollback frontend 5                     │
│   Option B : git revert + push + sync                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Commandes de Référence

```bash
# === SYNC ===
# Sync manuel
argocd app sync frontend

# Sync avec prune forcé
argocd app sync frontend --prune

# Sync avec dry-run (preview)
argocd app sync frontend --dry-run

# Sync une ressource spécifique
argocd app sync frontend --resource apps:Deployment:frontend

# === DIFF ===
# Voir les différences
argocd app diff frontend

# Diff avec plus de contexte
argocd app diff frontend --local ./manifests/

# === ROLLBACK ===
# Voir l'historique
argocd app history frontend

# Rollback à une révision
argocd app rollback frontend 5

# === SELF-HEAL ===
# Forcer un refresh (détection de drift)
argocd app get frontend --refresh

# === HOOKS ===
# Voir les hooks d'une app
kubectl get jobs -n frontend -l argocd.argoproj.io/hook

# Logs d'un hook
kubectl logs -n frontend job/db-migration

# === DEBUG ===
# Voir l'état détaillé
argocd app get frontend

# Voir les ressources
argocd app resources frontend

# Voir les events ArgoCD
kubectl get events -n argocd --field-selector reason=ResourceUpdated
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi désactiver Auto-Sync en production ?"
    **Réponse :**

    En production, le Manual Sync offre :

    1. **Contrôle du timing** : Déployer pendant les fenêtres de maintenance
    2. **Validation humaine** : Revue du diff avant apply
    3. **Conformité** : Change Approval Process (ITIL)
    4. **Sécurité** : Un bug mergé n'est pas déployé automatiquement
    5. **Coordination** : Synchroniser avec d'autres équipes (DBA, Ops)

    En dev/staging, Auto-Sync est recommandé pour le feedback rapide.

??? question "Question 2 : Comment garantir que la DB démarre avant l'API ?"
    **Réponse :**

    Utiliser les **Sync Waves** :

    ```yaml
    # Database
    metadata:
      annotations:
        argocd.argoproj.io/sync-wave: "-1"

    # API
    metadata:
      annotations:
        argocd.argoproj.io/sync-wave: "1"
    ```

    ArgoCD va :
    1. Déployer la Database (wave -1)
    2. Attendre qu'elle soit Healthy
    3. Déployer l'API (wave 1)

??? question "Question 3 : Quelqu'un a fait un kubectl edit en prod. Que se passe-t-il ?"
    **Réponse :**

    Cela dépend de la configuration :

    **Avec `selfHeal: true`** :
    - ArgoCD détecte le drift lors du prochain cycle de réconciliation (3 min)
    - Annule automatiquement la modification
    - Remet l'état Git

    **Avec `selfHeal: false`** :
    - ArgoCD affiche "OutOfSync"
    - La modification reste en place
    - Attente d'un sync manuel

    **Bonne pratique** : `selfHeal: true` + notifications pour éduquer les équipes à passer par Git.

---

## Conclusion de la Formation

Vous maîtrisez maintenant ArgoCD et le GitOps en entreprise :

- **Module 1** : Architecture (Controller, Repo Server, API Server)
- **Module 2** : App of Apps (Industrialisation, ApplicationSets)
- **Module 3** : Stratégies de Sync (Auto/Manual, Waves, Self-Heal)

### Prochaines Étapes Recommandées

1. **Déployer ArgoCD** sur votre cluster GKE
2. **Implémenter App of Apps** pour vos microservices
3. **Configurer les notifications** (Slack, Teams)
4. **Explorer** : Argo Rollouts (Canary/Blue-Green), Image Updater

### Ressources Complémentaires

- :material-link: [ArgoCD Documentation](https://argo-cd.readthedocs.io/)
- :material-link: [ArgoCD Best Practices](https://argo-cd.readthedocs.io/en/stable/user-guide/best_practices/)
- :material-link: [Argo Rollouts](https://argoproj.github.io/argo-rollouts/)
- :material-link: [GitOps Principles](https://opengitops.dev/)

---

!!! quote "Le Mantra GitOps"
    *"If it's not in Git, it doesn't exist. If it's in Git, it's deployed."*

---

**Temps estimé :** 60 minutes
**Niveau :** Avancé
