# Module 2 : Sécurité & IAM

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Distinguer GCP IAM et Kubernetes RBAC
- :material-check: Configurer Workload Identity (zero clés JSON)
- :material-check: Créer et sécuriser un Private Cluster
- :material-check: Implémenter des Network Policies efficaces

---

## 1. GCP IAM vs Kubernetes RBAC

### 1.1 Deux Systèmes, Deux Périmètres

GKE utilise **deux systèmes d'autorisation distincts** qui travaillent ensemble :

```
┌─────────────────────────────────────────────────────────────────┐
│              MODÈLE D'AUTORISATION GKE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    GCP IAM                              │   │
│   │   "Qui peut ACCÉDER au cluster ?"                       │   │
│   │                                                         │   │
│   │   • roles/container.admin                               │   │
│   │   • roles/container.developer                           │   │
│   │   • roles/container.viewer                              │   │
│   │                                                         │   │
│   │   Scope: Projets GCP, Clusters                          │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         │ L'utilisateur a accès au cluster      │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                 Kubernetes RBAC                         │   │
│   │   "Que peut-il FAIRE dans le cluster ?"                 │   │
│   │                                                         │   │
│   │   • cluster-admin (ClusterRole)                         │   │
│   │   • namespace-admin (Role)                              │   │
│   │   • read-only (Role)                                    │   │
│   │                                                         │   │
│   │   Scope: Namespaces, Resources K8s                      │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Rôles GCP IAM pour GKE

| Rôle IAM | Description | Permissions Clés |
|----------|-------------|------------------|
| `roles/container.admin` | Admin complet | Créer/supprimer clusters, full access |
| `roles/container.clusterAdmin` | Admin cluster | Gérer un cluster existant |
| `roles/container.developer` | Développeur | Deploy apps, pas de gestion infra |
| `roles/container.viewer` | Lecture seule | Voir les clusters et workloads |

```bash
# Attribuer un rôle IAM
gcloud projects add-iam-policy-binding my-project \
    --member="user:dev@worldline.com" \
    --role="roles/container.developer"

# Vérifier les bindings
gcloud projects get-iam-policy my-project \
    --flatten="bindings[].members" \
    --filter="bindings.role:roles/container.*"
```

### 1.3 Kubernetes RBAC

Le RBAC Kubernetes contrôle les actions **à l'intérieur** du cluster :

```yaml
# ClusterRole : Permissions au niveau cluster
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: namespace-admin
rules:
- apiGroups: [""]
  resources: ["pods", "services", "configmaps", "secrets"]
  verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]
- apiGroups: ["apps"]
  resources: ["deployments", "replicasets", "statefulsets"]
  verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]

---
# ClusterRoleBinding : Lier le rôle à un utilisateur GCP
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: dev-team-binding
subjects:
- kind: User
  name: dev@worldline.com  # Email GCP
  apiGroup: rbac.authorization.k8s.io
- kind: Group
  name: devs@worldline.com  # Groupe Google Workspace
  apiGroup: rbac.authorization.k8s.io
roleRef:
  kind: ClusterRole
  name: namespace-admin
  apiGroup: rbac.authorization.k8s.io
```

```yaml
# Role : Permissions limitées à un namespace
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  namespace: team-frontend
  name: pod-reader
rules:
- apiGroups: [""]
  resources: ["pods", "pods/log"]
  verbs: ["get", "list", "watch"]

---
# RoleBinding : Lier dans un namespace spécifique
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: read-pods-frontend
  namespace: team-frontend
subjects:
- kind: User
  name: junior@worldline.com
  apiGroup: rbac.authorization.k8s.io
roleRef:
  kind: Role
  name: pod-reader
  apiGroup: rbac.authorization.k8s.io
```

!!! tip "Bonnes Pratiques RBAC"
    1. **Principe du moindre privilège** : Donnez uniquement les permissions nécessaires
    2. **Utilisez des Roles (pas ClusterRoles)** : Limitez au namespace quand possible
    3. **Groupes Google** : Préférez les groupes aux utilisateurs individuels
    4. **Auditez régulièrement** : `kubectl get rolebindings,clusterrolebindings -A`

---

## 2. Workload Identity : Zero Clés JSON

### 2.1 Le Problème des Service Account Keys

```
┌─────────────────────────────────────────────────────────────────┐
│              ANTI-PATTERN : Clés JSON dans les Pods             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ❌ Méthode dangereuse :                                       │
│                                                                 │
│   1. Créer un Service Account GCP                               │
│   2. Générer une clé JSON                                       │
│   3. La stocker dans un Secret Kubernetes                       │
│   4. La monter dans le Pod                                      │
│                                                                 │
│   PROBLÈMES :                                                   │
│   • Clés qui ne expirent jamais (sauf révocation manuelle)     │
│   • Risque de fuite (logs, commits Git, backups)               │
│   • Rotation complexe (redéployer tous les pods)               │
│   • Audit difficile (qui utilise quelle clé ?)                 │
│                                                                 │
│   ⚠️  INTERDIT en environnement SecNumCloud                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 La Solution : Workload Identity

**Workload Identity** permet à un Pod de s'authentifier auprès des APIs GCP **sans clé JSON** :

```
┌─────────────────────────────────────────────────────────────────┐
│              WORKLOAD IDENTITY                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────┐                                       │
│   │    Pod (app)        │                                       │
│   │                     │                                       │
│   │  ServiceAccount K8s │◄────┐                                 │
│   │  "my-app-sa"        │     │ Annotation                      │
│   └─────────────────────┘     │                                 │
│            │                  │                                 │
│            │ 1. Demande token │                                 │
│            ▼                  │                                 │
│   ┌─────────────────────┐     │                                 │
│   │   GKE Metadata      │     │                                 │
│   │   Server            │     │                                 │
│   └─────────────────────┘     │                                 │
│            │                  │                                 │
│            │ 2. Échange token │                                 │
│            ▼                  │                                 │
│   ┌─────────────────────┐     │                                 │
│   │  GCP IAM            │     │                                 │
│   │                     │     │                                 │
│   │  Service Account    │─────┘                                 │
│   │  "my-app@proj.iam"  │  IAM binding                          │
│   │                     │                                       │
│   │  Roles:             │                                       │
│   │  - storage.admin    │                                       │
│   │  - pubsub.publisher │                                       │
│   └─────────────────────┘                                       │
│            │                                                    │
│            │ 3. Token GCP valide                                │
│            ▼                                                    │
│   ┌─────────────────────┐                                       │
│   │   APIs GCP          │                                       │
│   │   Cloud Storage     │                                       │
│   │   Pub/Sub           │                                       │
│   └─────────────────────┘                                       │
│                                                                 │
│   ✅ Pas de clé JSON                                            │
│   ✅ Tokens courts (1h, renouvelés automatiquement)             │
│   ✅ Audit via Cloud Audit Logs                                 │
│   ✅ Révocation immédiate possible                              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 Configuration Workload Identity

**Étape 1 : Activer Workload Identity sur le cluster**

```bash
# Lors de la création
gcloud container clusters create my-cluster \
    --region europe-west1 \
    --workload-pool=my-project.svc.id.goog

# Sur un cluster existant
gcloud container clusters update my-cluster \
    --region europe-west1 \
    --workload-pool=my-project.svc.id.goog

# Activer sur un node pool existant
gcloud container node-pools update default-pool \
    --cluster my-cluster \
    --region europe-west1 \
    --workload-metadata=GKE_METADATA
```

**Étape 2 : Créer les Service Accounts**

```bash
# Service Account GCP
gcloud iam service-accounts create my-app-sa \
    --display-name="Service Account for My App"

# Attribuer les rôles nécessaires
gcloud projects add-iam-policy-binding my-project \
    --member="serviceAccount:my-app-sa@my-project.iam.gserviceaccount.com" \
    --role="roles/storage.objectViewer"

# Service Account Kubernetes
kubectl create serviceaccount my-app-ksa -n my-namespace
```

**Étape 3 : Lier les Service Accounts**

```bash
# Permettre au KSA d'utiliser le GSA
gcloud iam service-accounts add-iam-policy-binding \
    my-app-sa@my-project.iam.gserviceaccount.com \
    --role="roles/iam.workloadIdentityUser" \
    --member="serviceAccount:my-project.svc.id.goog[my-namespace/my-app-ksa]"

# Annoter le KSA avec le GSA
kubectl annotate serviceaccount my-app-ksa \
    -n my-namespace \
    iam.gke.io/gcp-service-account=my-app-sa@my-project.iam.gserviceaccount.com
```

**Étape 4 : Utiliser dans un Deployment**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-app
  namespace: my-namespace
spec:
  replicas: 2
  selector:
    matchLabels:
      app: my-app
  template:
    metadata:
      labels:
        app: my-app
    spec:
      serviceAccountName: my-app-ksa  # Utilise Workload Identity
      containers:
      - name: app
        image: europe-west1-docker.pkg.dev/my-project/repo/app:v1
        env:
        # Pas besoin de GOOGLE_APPLICATION_CREDENTIALS !
        - name: PROJECT_ID
          value: my-project
```

**Vérification :**

```bash
# Tester depuis un pod
kubectl run test-wi --rm -it --restart=Never \
    --image=google/cloud-sdk:slim \
    --serviceaccount=my-app-ksa \
    -n my-namespace \
    -- gcloud auth list

# Doit afficher le GSA comme compte actif
```

---

## 3. Private Clusters

### 3.1 Concept

Un **Private Cluster** n'a pas d'IPs publiques sur ses nodes :

```
┌─────────────────────────────────────────────────────────────────┐
│              PRIVATE CLUSTER GKE                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Internet                                                      │
│      │                                                          │
│      │ ❌ Pas d'accès direct aux nodes                          │
│      │                                                          │
│      ▼                                                          │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    VPC Worldline                        │   │
│   │                                                         │   │
│   │   ┌─────────────────────────────────────────────────┐   │   │
│   │   │           Private Cluster GKE                   │   │   │
│   │   │                                                 │   │   │
│   │   │   Control Plane (172.16.0.0/28)                │   │   │
│   │   │   ┌─────────────┐                              │   │   │
│   │   │   │  API Server │◄──── Private Endpoint        │   │   │
│   │   │   │  (Private)  │      10.0.0.2                │   │   │
│   │   │   └─────────────┘                              │   │   │
│   │   │         │                                      │   │   │
│   │   │         │ VPC Peering                          │   │   │
│   │   │         ▼                                      │   │   │
│   │   │   ┌──────────┐ ┌──────────┐ ┌──────────┐      │   │   │
│   │   │   │  Node 1  │ │  Node 2  │ │  Node 3  │      │   │   │
│   │   │   │10.0.1.10 │ │10.0.1.11 │ │10.0.1.12 │      │   │   │
│   │   │   │ Private  │ │ Private  │ │ Private  │      │   │   │
│   │   │   └──────────┘ └──────────┘ └──────────┘      │   │   │
│   │   │                                                 │   │   │
│   │   └─────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   │   ┌─────────────────────────────────────────────────┐   │   │
│   │   │  Cloud NAT                                      │   │   │
│   │   │  (Pour accès sortant : registries, APIs)        │   │   │
│   │   └─────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Création d'un Private Cluster

```bash
# Créer le Private Cluster
gcloud container clusters create private-cluster \
    --region europe-west1 \
    --enable-private-nodes \
    --enable-private-endpoint \
    --master-ipv4-cidr 172.16.0.0/28 \
    --enable-ip-alias \
    --network my-vpc \
    --subnetwork my-subnet \
    --enable-master-authorized-networks \
    --master-authorized-networks 10.0.0.0/8,192.168.1.0/24

# Options importantes :
# --enable-private-nodes     : Nodes sans IP publique
# --enable-private-endpoint  : API Server accessible uniquement en privé
# --master-ipv4-cidr        : CIDR pour le Control Plane (VPC Peering)
# --master-authorized-networks : CIDRs autorisés à accéder à l'API
```

### 3.3 Accès au Private Cluster

**Option 1 : Bastion Host (VM dans le VPC)**

```bash
# Créer une VM bastion
gcloud compute instances create bastion \
    --zone europe-west1-b \
    --machine-type e2-micro \
    --network my-vpc \
    --subnet my-subnet \
    --scopes cloud-platform

# Se connecter au bastion
gcloud compute ssh bastion --zone europe-west1-b

# Depuis le bastion, accéder au cluster
gcloud container clusters get-credentials private-cluster --region europe-west1
kubectl get nodes
```

**Option 2 : Cloud Shell avec Authorized Networks**

```bash
# Ajouter l'IP de Cloud Shell aux authorized networks
# (Nécessite de mettre à jour régulièrement car l'IP change)
gcloud container clusters update private-cluster \
    --region europe-west1 \
    --enable-master-authorized-networks \
    --master-authorized-networks $(curl -s ifconfig.me)/32
```

**Option 3 : VPN / Interconnect**

Pour les environnements de production, utilisez Cloud VPN ou Cloud Interconnect pour connecter votre réseau on-premise au VPC GCP.

### 3.4 Cloud NAT pour l'Accès Sortant

Les nodes privés ont besoin de Cloud NAT pour accéder à Internet (pull d'images, APIs externes) :

```bash
# Créer un Cloud Router
gcloud compute routers create nat-router \
    --network my-vpc \
    --region europe-west1

# Créer Cloud NAT
gcloud compute routers nats create nat-config \
    --router nat-router \
    --region europe-west1 \
    --nat-all-subnet-ip-ranges \
    --auto-allocate-nat-external-ips
```

---

## 4. Network Policies

### 4.1 Concept

Les **Network Policies** contrôlent le trafic réseau entre pods au niveau L3/L4 :

```
┌─────────────────────────────────────────────────────────────────┐
│              NETWORK POLICIES                                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Sans Network Policy :                                         │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                                                         │   │
│   │   Pod A ◄────────────────────────────────────► Pod B    │   │
│   │     │                                            │      │   │
│   │     │           TOUT COMMUNIQUE                  │      │   │
│   │     │          AVEC TOUT                         │      │   │
│   │     ▼                                            ▼      │   │
│   │   Pod C ◄────────────────────────────────────► Pod D    │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   Avec Network Policies (Zero Trust) :                          │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                                                         │   │
│   │   Frontend ─────► API ─────► Database                   │   │
│   │      │             │             │                      │   │
│   │      │             │             │                      │   │
│   │      ❌            ❌            ❌ (pas d'accès direct) │   │
│   │      │             │             │                      │   │
│   │      └─────────────┴─────────────┘                      │   │
│   │                                                         │   │
│   │   Seules les communications EXPLICITES sont autorisées  │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Activer les Network Policies

```bash
# Lors de la création du cluster
gcloud container clusters create my-cluster \
    --region europe-west1 \
    --enable-network-policy

# Sur un cluster existant
gcloud container clusters update my-cluster \
    --region europe-west1 \
    --update-addons NetworkPolicy=ENABLED

# Note: Nécessite un redémarrage des nodes
```

### 4.3 Exemples de Network Policies

**Policy 1 : Deny All (Default)**

```yaml
# Bloquer tout le trafic entrant par défaut
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-ingress
  namespace: production
spec:
  podSelector: {}  # Appliqué à tous les pods
  policyTypes:
  - Ingress
```

**Policy 2 : Autoriser le Frontend vers l'API**

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-frontend-to-api
  namespace: production
spec:
  podSelector:
    matchLabels:
      app: api
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: frontend
    ports:
    - protocol: TCP
      port: 8080
```

**Policy 3 : Autoriser l'API vers la Database**

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-api-to-database
  namespace: production
spec:
  podSelector:
    matchLabels:
      app: database
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: api
    ports:
    - protocol: TCP
      port: 5432
```

**Policy 4 : Autoriser le monitoring**

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-prometheus-scrape
  namespace: production
spec:
  podSelector:
    matchLabels:
      monitoring: enabled
  policyTypes:
  - Ingress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: monitoring
      podSelector:
        matchLabels:
          app: prometheus
    ports:
    - protocol: TCP
      port: 9090
```

### 4.4 Tester les Network Policies

```bash
# Créer un pod de test
kubectl run test-client --rm -it --restart=Never \
    --image=busybox \
    -n production \
    --labels="app=test" \
    -- wget -q --timeout=2 -O- http://api:8080/health

# Si la policy bloque : timeout
# Si la policy autorise : réponse de l'API
```

---

## 5. Bonnes Pratiques Sécurité GKE

### 5.1 Checklist Sécurité

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST SÉCURITÉ GKE                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   CLUSTER                                                       │
│   ─────────                                                     │
│   ☐ Private Cluster activé                                      │
│   ☐ Master Authorized Networks configuré                        │
│   ☐ Workload Identity activé                                    │
│   ☐ Network Policies activées                                   │
│   ☐ Binary Authorization activé (production)                    │
│   ☐ Shielded GKE Nodes activés                                  │
│                                                                 │
│   NODES                                                         │
│   ───────                                                       │
│   ☐ Container-Optimized OS (COS)                                │
│   ☐ Auto-upgrade activé                                         │
│   ☐ Auto-repair activé                                          │
│   ☐ Pas de Service Account par défaut sur les nodes             │
│                                                                 │
│   WORKLOADS                                                     │
│   ──────────                                                    │
│   ☐ Pas de conteneurs privileged                                │
│   ☐ runAsNonRoot: true                                          │
│   ☐ readOnlyRootFilesystem: true                                │
│   ☐ Requests et Limits définis                                  │
│   ☐ Images depuis Artifact Registry (pas Docker Hub)            │
│                                                                 │
│   SECRETS                                                       │
│   ────────                                                      │
│   ☐ Secrets chiffrés avec Cloud KMS                             │
│   ☐ Pas de secrets en clair dans les manifests                  │
│   ☐ Utiliser Secret Manager pour les secrets sensibles          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Pod Security Standards

```yaml
# Exemple de SecurityContext restrictif
apiVersion: apps/v1
kind: Deployment
metadata:
  name: secure-app
spec:
  replicas: 2
  selector:
    matchLabels:
      app: secure-app
  template:
    metadata:
      labels:
        app: secure-app
    spec:
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        runAsGroup: 1000
        fsGroup: 1000
        seccompProfile:
          type: RuntimeDefault
      containers:
      - name: app
        image: europe-west1-docker.pkg.dev/my-project/repo/app:v1
        securityContext:
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: true
          capabilities:
            drop:
            - ALL
        volumeMounts:
        - name: tmp
          mountPath: /tmp
        - name: cache
          mountPath: /app/cache
      volumes:
      - name: tmp
        emptyDir: {}
      - name: cache
        emptyDir: {}
```

---

## 6. Commandes de Référence

```bash
# === IAM ===
# Voir les bindings IAM d'un projet
gcloud projects get-iam-policy my-project

# Ajouter un rôle GKE
gcloud projects add-iam-policy-binding my-project \
    --member="user:dev@worldline.com" \
    --role="roles/container.developer"

# === RBAC ===
# Voir tous les RoleBindings
kubectl get rolebindings,clusterrolebindings -A

# Tester les permissions d'un utilisateur
kubectl auth can-i create deployments --as=dev@worldline.com -n production

# === WORKLOAD IDENTITY ===
# Vérifier si activé
gcloud container clusters describe my-cluster \
    --region europe-west1 \
    --format="value(workloadIdentityConfig.workloadPool)"

# Lister les GSA liés à un KSA
kubectl get serviceaccount my-ksa -n my-ns -o yaml | grep gcp-service-account

# === NETWORK POLICIES ===
# Lister les policies
kubectl get networkpolicies -A

# Décrire une policy
kubectl describe networkpolicy allow-frontend-to-api -n production

# === SÉCURITÉ ===
# Vérifier la configuration du cluster
gcloud container clusters describe my-cluster \
    --region europe-west1 \
    --format="table(privateClusterConfig,masterAuthorizedNetworksConfig)"
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Quelle est la différence entre GCP IAM et Kubernetes RBAC ?"
    **Réponse :**

    - **GCP IAM** : Contrôle **qui peut accéder au cluster** (authentification et autorisation au niveau GCP)
    - **Kubernetes RBAC** : Contrôle **ce que l'utilisateur peut faire** dans le cluster (autorisation au niveau K8s)

    Les deux fonctionnent ensemble : IAM donne l'accès, RBAC définit les permissions.

??? question "Question 2 : Pourquoi Workload Identity est-il préféré aux clés JSON ?"
    **Réponse :**

    Workload Identity offre plusieurs avantages :

    1. **Pas de gestion de clés** : Pas de rotation, pas de stockage sécurisé
    2. **Tokens courts** : Validité d'1 heure, renouvelés automatiquement
    3. **Audit** : Traçabilité complète dans Cloud Audit Logs
    4. **Révocation immédiate** : Modifier le binding IAM suffit
    5. **Conformité** : Requis pour SecNumCloud

??? question "Question 3 : Quand utiliser un Private Cluster ?"
    **Réponse :**

    Un Private Cluster est recommandé pour :

    - **Production** : Réduire la surface d'attaque
    - **Conformité** : Exigences SecNumCloud, GDPR
    - **Données sensibles** : PII, données bancaires
    - **Workloads régulés** : Finance, santé

    Note : Un Private Cluster nécessite Cloud NAT pour l'accès sortant et un bastion ou VPN pour l'accès administratif.

---

## Prochaine Étape

Maintenant que votre cluster est sécurisé, découvrez comment gérer le stockage et le réseau.

[:octicons-arrow-right-24: Module 3 : Storage & Networking](03-storage-networking.md)

---

**Temps estimé :** 75 minutes
**Niveau :** Avancé
