# Module 1 : Gestion du State

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Comprendre le rôle et le contenu du `terraform.tfstate`
- :material-check: Identifier les risques du state local
- :material-check: Configurer un Remote Backend sur GCS
- :material-check: Implémenter le State Locking pour le travail en équipe

---

## 1. Le terraform.tfstate : La Mémoire de Terraform

### 1.1 Qu'est-ce que le State ?

Le **state** est un fichier JSON qui représente la réalité de votre infrastructure déployée. C'est la **source de vérité** que Terraform utilise pour :

1. Mapper les ressources du code aux ressources réelles
2. Calculer les changements nécessaires (plan)
3. Suivre les métadonnées (dépendances, attributs)

```
┌─────────────────────────────────────────────────────────────────┐
│              LE RÔLE DU STATE FILE                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌───────────────────┐        ┌───────────────────┐           │
│   │   Code Terraform  │        │  Infrastructure   │           │
│   │      (.tf)        │        │   Réelle (GCP)    │           │
│   │                   │        │                   │           │
│   │  resource "vm" {  │        │  VM: web-prod-01  │           │
│   │    name = "web"   │        │  IP: 10.0.1.5     │           │
│   │  }                │        │  Zone: eu-west1-b │           │
│   └───────────────────┘        └───────────────────┘           │
│            │                            │                       │
│            │                            │                       │
│            └──────────┐    ┌────────────┘                       │
│                       │    │                                    │
│                       ▼    ▼                                    │
│            ┌───────────────────┐                                │
│            │  terraform.tfstate │                               │
│            │                   │                                │
│            │  "Voici ce qui    │                                │
│            │   existe réellement│                               │
│            │   et comment le   │                                │
│            │   code s'y        │                                │
│            │   rapporte"       │                                │
│            └───────────────────┘                                │
│                                                                 │
│   terraform plan = Comparer Code vs State vs Réel               │
│   terraform apply = Synchroniser Réel avec Code (via State)     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Contenu du State

Voici un exemple de ce que contient réellement un state file :

```json
{
  "version": 4,
  "terraform_version": "1.6.0",
  "serial": 42,
  "lineage": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "outputs": {
    "instance_ip": {
      "value": "34.78.123.45",
      "type": "string"
    }
  },
  "resources": [
    {
      "mode": "managed",
      "type": "google_compute_instance",
      "name": "web",
      "provider": "provider[\"registry.terraform.io/hashicorp/google\"]",
      "instances": [
        {
          "schema_version": 6,
          "attributes": {
            "id": "projects/my-project/zones/europe-west1-b/instances/web-prod-01",
            "name": "web-prod-01",
            "machine_type": "e2-medium",
            "zone": "europe-west1-b",
            "network_interface": [
              {
                "network_ip": "10.0.1.5",
                "access_config": [
                  {
                    "nat_ip": "34.78.123.45"
                  }
                ]
              }
            ],
            "metadata": {
              "ssh-keys": "admin:ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAAB..."
            }
          }
        }
      ]
    },
    {
      "mode": "managed",
      "type": "google_sql_database_instance",
      "name": "main",
      "instances": [
        {
          "attributes": {
            "name": "prod-db-instance",
            "root_password": "SuperSecret123!",
            "ip_address": [
              {
                "ip_address": "10.0.2.50"
              }
            ]
          }
        }
      ]
    }
  ]
}
```

!!! danger "Le State Contient des SECRETS"
    Regardez attentivement l'exemple ci-dessus. Le state contient :

    - **`root_password`** : Le mot de passe root de la base de données **en clair**
    - **`ssh-keys`** : Les clés SSH des utilisateurs
    - **`nat_ip`** : Les IPs publiques de vos serveurs
    - **Toute information sensible** retournée par les APIs GCP

    **Le state file EST un secret. Traitez-le comme tel.**

---

## 2. Le Risque N°1 : Le State Local

### 2.1 Le Scénario Catastrophe

```
┌─────────────────────────────────────────────────────────────────┐
│              SCÉNARIO : STATE LOCAL EN ÉQUIPE                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Jour 1 : Alice déploie l'infrastructure                       │
│   ────────────────────────────────────────                      │
│   $ terraform apply                                             │
│   → Crée : VPC, GKE, Cloud SQL                                  │
│   → State sauvé localement : /home/alice/project/terraform.tfstate│
│                                                                 │
│   Jour 2 : Bob veut ajouter une VM                              │
│   ────────────────────────────────────                          │
│   $ git pull  # Bob récupère le code                            │
│   $ terraform apply                                             │
│                                                                 │
│   ⚠️  PROBLÈME : Bob n'a PAS le state d'Alice !                 │
│                                                                 │
│   Terraform pense que RIEN n'existe.                            │
│   → Essaie de recréer VPC, GKE, Cloud SQL                       │
│   → ÉCHEC : "Resource already exists"                           │
│   → OU PIRE : Crée des doublons                                 │
│                                                                 │
│   Jour 3 : Alice supprime son laptop                            │
│   ─────────────────────────────────────                         │
│   → Le state est PERDU                                          │
│   → Plus aucun moyen de gérer l'infrastructure via Terraform    │
│   → `terraform destroy` ne fonctionne plus                      │
│   → Nettoyage manuel obligatoire                                │
│                                                                 │
│   💀 GAME OVER                                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Les Problèmes du State Local

| Problème | Conséquence |
|----------|-------------|
| **Pas de partage** | Impossible de travailler en équipe |
| **Pas de backup** | Perte du laptop = perte du state |
| **Pas de locking** | Deux apply simultanés = corruption |
| **Secrets en clair** | Sur le disque local, accessible |
| **Pas d'audit** | Qui a fait quoi, quand ? |

!!! danger "Règle Absolue"
    **JAMAIS de state local en production.**

    Le state local est acceptable uniquement pour :
    - Apprentissage personnel
    - Tests locaux temporaires
    - Prototypage rapide

    Dès qu'un projet devient sérieux → **Remote Backend**.

---

## 3. La Solution : Remote Backend (GCS)

### 3.1 Architecture Remote State

```
┌─────────────────────────────────────────────────────────────────┐
│              REMOTE STATE AVEC GCS                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐      │
│   │   Alice     │     │    Bob      │     │    CI/CD    │      │
│   │  (laptop)   │     │  (laptop)   │     │  (runner)   │      │
│   └──────┬──────┘     └──────┬──────┘     └──────┬──────┘      │
│          │                   │                   │              │
│          │ terraform         │ terraform         │ terraform   │
│          │ init/plan/apply   │ init/plan/apply   │ apply       │
│          │                   │                   │              │
│          └───────────────────┼───────────────────┘              │
│                              │                                  │
│                              ▼                                  │
│          ┌───────────────────────────────────────┐              │
│          │         Google Cloud Storage          │              │
│          │                                       │              │
│          │   Bucket: worldline-tf-state          │              │
│          │   ├── dev/terraform.tfstate           │              │
│          │   ├── staging/terraform.tfstate       │              │
│          │   └── prod/terraform.tfstate          │              │
│          │                                       │              │
│          │   ✅ Chiffrement at-rest (AES-256)    │              │
│          │   ✅ Versioning activé                │              │
│          │   ✅ Object Locking                   │              │
│          │   ✅ IAM granulaire                   │              │
│          │   ✅ Audit Logs                       │              │
│          │                                       │              │
│          └───────────────────────────────────────┘              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Création du Bucket GCS

```bash
# Variables
PROJECT_ID="worldline-prod"
BUCKET_NAME="worldline-terraform-state"
REGION="europe-west1"

# Créer le bucket
gcloud storage buckets create gs://${BUCKET_NAME} \
    --project=${PROJECT_ID} \
    --location=${REGION} \
    --uniform-bucket-level-access

# Activer le versioning (CRITIQUE pour la récupération)
gcloud storage buckets update gs://${BUCKET_NAME} \
    --versioning

# Activer le chiffrement avec une clé KMS (optionnel mais recommandé)
gcloud storage buckets update gs://${BUCKET_NAME} \
    --default-encryption-key=projects/${PROJECT_ID}/locations/${REGION}/keyRings/terraform/cryptoKeys/state-key
```

### 3.3 Configuration du Backend

Créez un fichier `backend.tf` :

```hcl
# backend.tf
terraform {
  backend "gcs" {
    bucket = "worldline-terraform-state"
    prefix = "prod"  # Crée prod/default.tfstate
  }
}
```

Ou avec plus d'options :

```hcl
# backend.tf - Configuration complète
terraform {
  backend "gcs" {
    bucket = "worldline-terraform-state"
    prefix = "environments/prod"

    # Optionnel : Impersonation de Service Account
    # impersonate_service_account = "terraform@worldline-prod.iam.gserviceaccount.com"
  }
}
```

### 3.4 Migration du State Local vers Remote

```bash
# Situation : Vous avez un state local et voulez migrer vers GCS

# 1. Ajouter la configuration backend (backend.tf ci-dessus)

# 2. Réinitialiser Terraform
terraform init -migrate-state

# Terraform affiche :
# Initializing the backend...
# Do you want to copy existing state to the new backend?
#   Enter a value: yes

# 3. Vérifier que le state est bien sur GCS
gsutil ls gs://worldline-terraform-state/prod/

# 4. Supprimer le state local (il est maintenant sur GCS)
rm terraform.tfstate terraform.tfstate.backup
```

---

## 4. State Locking : Collaboration Sans Collision

### 4.1 Le Problème de la Concurrence

```
┌─────────────────────────────────────────────────────────────────┐
│              RACE CONDITION SANS LOCKING                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   T=0    Alice lit le state (VM count = 2)                      │
│   T=1    Bob lit le state (VM count = 2)                        │
│   T=2    Alice calcule : "Ajouter 1 VM, total = 3"              │
│   T=3    Bob calcule : "Supprimer 1 VM, total = 1"              │
│   T=4    Alice écrit le state (VM count = 3)                    │
│   T=5    Bob écrit le state (VM count = 1) ← ÉCRASE Alice !     │
│                                                                 │
│   RÉSULTAT :                                                    │
│   - State dit : 1 VM                                            │
│   - Réalité GCP : 3 VMs (Alice a créé avant Bob)               │
│   - State CORROMPU : Terraform ne sait plus ce qui existe       │
│                                                                 │
│   💀 STATE CORRUPTION                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 GCS Native Locking

**Bonne nouvelle** : GCS supporte le locking nativement depuis Terraform 0.14+. Aucune configuration supplémentaire n'est nécessaire.

```
┌─────────────────────────────────────────────────────────────────┐
│              STATE LOCKING AVEC GCS                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   T=0    Alice : terraform apply                                │
│          → Terraform crée un lock file sur GCS                  │
│          → Lock acquis ✅                                       │
│                                                                 │
│   T=1    Bob : terraform apply                                  │
│          → Terraform détecte le lock                            │
│          │                                                      │
│          │  Error: Error acquiring the state lock               │
│          │                                                      │
│          │  Lock Info:                                          │
│          │    ID:        a1b2c3d4-5678-90ab-cdef                │
│          │    Path:      gs://bucket/prod/default.tflock        │
│          │    Operation: OperationTypeApply                     │
│          │    Who:       alice@worldline.com                    │
│          │    Version:   1.6.0                                  │
│          │    Created:   2025-01-28 14:32:15 UTC                │
│          │                                                      │
│          │  Terraform acquires a state lock to protect          │
│          │  the state from being written by multiple users.     │
│          │                                                      │
│          └─────────────────────────────────────────────────     │
│                                                                 │
│   T=10   Alice : apply terminé                                  │
│          → Lock automatiquement libéré                          │
│                                                                 │
│   T=11   Bob : peut maintenant lancer son apply                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.3 Forcer le Déverrouillage (Urgence)

!!! warning "À utiliser avec précaution"
    Le force-unlock ne doit être utilisé que si :

    - Le processus Terraform a crashé
    - Le lock est orphelin (personne ne l'utilise)
    - Vous avez vérifié qu'aucun apply n'est en cours

```bash
# Voir les informations du lock
terraform force-unlock -help

# Forcer le déverrouillage (DANGEREUX)
terraform force-unlock LOCK_ID

# Exemple
terraform force-unlock a1b2c3d4-5678-90ab-cdef
```

---

## 5. Bonnes Pratiques de Gestion du State

### 5.1 Checklist State Management

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST STATE MANAGEMENT                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   BACKEND                                                       │
│   ───────                                                       │
│   ☑ Remote backend configuré (GCS)                              │
│   ☑ Versioning activé sur le bucket                             │
│   ☑ Chiffrement at-rest (default ou KMS)                        │
│   ☑ IAM restrictif (pas de public access)                       │
│                                                                 │
│   SÉCURITÉ                                                      │
│   ────────                                                      │
│   ☑ Bucket non public                                           │
│   ☑ Accès limité (Service Accounts dédiés)                      │
│   ☑ Audit Logs activés                                          │
│   ☑ State jamais commité dans Git (.gitignore)                  │
│                                                                 │
│   ORGANISATION                                                  │
│   ────────────                                                  │
│   ☑ Un state par environnement (dev/staging/prod)               │
│   ☑ Un state par composant si nécessaire (réseau, app, db)      │
│   ☑ Naming convention claire                                    │
│                                                                 │
│   OPÉRATIONS                                                    │
│   ──────────                                                    │
│   ☑ Ne jamais éditer le state manuellement                      │
│   ☑ Utiliser terraform state mv pour renommer                   │
│   ☑ Backup avant opérations risquées                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Fichier .gitignore

```gitignore
# Terraform State - NEVER COMMIT
*.tfstate
*.tfstate.*
.terraform.tfstate.lock.info

# Terraform files
.terraform/
*.tfplan
*.tfplan.json

# Sensitive variable files
*.auto.tfvars
terraform.tfvars
secrets.tfvars

# Override files (local testing)
override.tf
override.tf.json
*_override.tf
*_override.tf.json

# Crash logs
crash.log
crash.*.log
```

### 5.3 Commandes State Utiles

```bash
# === CONSULTATION ===
# Lister toutes les ressources dans le state
terraform state list

# Voir les détails d'une ressource
terraform state show google_compute_instance.web

# Afficher tout le state (JSON)
terraform state pull

# === MANIPULATION (AVEC PRÉCAUTION) ===
# Renommer une ressource (refactoring)
terraform state mv google_compute_instance.web google_compute_instance.frontend

# Supprimer une ressource du state (sans la détruire dans GCP)
terraform state rm google_compute_instance.legacy

# Importer une ressource existante dans le state
terraform import google_compute_instance.existing projects/my-proj/zones/eu-west1-b/instances/existing-vm

# === BACKUP ===
# Sauvegarder le state
terraform state pull > backup-$(date +%Y%m%d-%H%M%S).tfstate

# Restaurer un state (depuis backup GCS avec versioning)
gsutil cp gs://bucket/prod/default.tfstate#1234567890 restored.tfstate
```

---

## 6. State Splitting : Quand Diviser ?

### 6.1 Mono-State vs Multi-State

```
┌─────────────────────────────────────────────────────────────────┐
│              MONO-STATE vs MULTI-STATE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   MONO-STATE (Tout dans un seul state)                          │
│   ─────────────────────────────────────                         │
│                                                                 │
│   terraform.tfstate                                             │
│   ├── VPC                                                       │
│   ├── GKE Cluster                                               │
│   ├── Cloud SQL                                                 │
│   ├── Applications (20+ deployments)                            │
│   └── IAM Bindings                                              │
│                                                                 │
│   ⚠️  Problèmes :                                               │
│   • terraform plan prend 10 minutes                             │
│   • Un changement app bloque tout le monde                      │
│   • Risque : toucher le VPC en modifiant une app                │
│   • Blast radius maximum                                        │
│                                                                 │
│   ─────────────────────────────────────────────────────────────│
│                                                                 │
│   MULTI-STATE (Séparation par composant)                        │
│   ──────────────────────────────────────                        │
│                                                                 │
│   network/terraform.tfstate     ← Équipe Infra (rare changes)  │
│   ├── VPC                                                       │
│   ├── Subnets                                                   │
│   └── Firewall Rules                                            │
│                                                                 │
│   gke/terraform.tfstate         ← Équipe Platform               │
│   ├── GKE Cluster                                               │
│   └── Node Pools                                                │
│                                                                 │
│   database/terraform.tfstate    ← Équipe DBA                    │
│   └── Cloud SQL                                                 │
│                                                                 │
│   app-frontend/terraform.tfstate ← Équipe Dev Frontend          │
│   app-backend/terraform.tfstate  ← Équipe Dev Backend           │
│                                                                 │
│   ✅ Avantages :                                                │
│   • Plans rapides (scope limité)                                │
│   • Équipes autonomes                                           │
│   • Blast radius réduit                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Data Sources pour Lier les States

```hcl
# Dans le projet "app", référencer le VPC du projet "network"

# Option 1 : Data source Terraform Remote State
data "terraform_remote_state" "network" {
  backend = "gcs"
  config = {
    bucket = "worldline-terraform-state"
    prefix = "network"
  }
}

resource "google_compute_instance" "app" {
  # ...
  network_interface {
    network    = data.terraform_remote_state.network.outputs.vpc_id
    subnetwork = data.terraform_remote_state.network.outputs.subnet_id
  }
}

# Option 2 : Data source GCP (plus découplé)
data "google_compute_network" "main" {
  name    = "worldline-vpc"
  project = "worldline-network"
}

resource "google_compute_instance" "app" {
  # ...
  network_interface {
    network = data.google_compute_network.main.id
  }
}
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi le state file est-il considéré comme un secret ?"
    **Réponse :**

    Le state file contient :

    1. **Mots de passe** : Root passwords de bases de données en clair
    2. **Clés SSH** : Clés publiques (et parfois privées) des utilisateurs
    3. **IPs et endpoints** : Informations de réseau internes
    4. **Tokens et secrets** : Tout attribut retourné par les APIs

    Ces informations permettraient à un attaquant de :
    - Se connecter directement aux bases de données
    - Cartographier l'infrastructure
    - Accéder aux systèmes via SSH

    **Traitement** : Chiffrement, accès restreint, jamais dans Git.

??? question "Question 2 : Que se passe-t-il si deux personnes lancent terraform apply simultanément sans locking ?"
    **Réponse :**

    **Race condition** : Les deux processus :

    1. Lisent le même state initial
    2. Calculent leurs changements indépendamment
    3. Appliquent leurs modifications
    4. Écrivent le state final

    Le dernier à écrire **écrase** les changements du premier.

    **Conséquences** :
    - State corrompu (ne reflète pas la réalité)
    - Ressources orphelines (existent mais pas dans le state)
    - `terraform destroy` incomplet
    - Reconstruction manuelle nécessaire

??? question "Question 3 : Comment récupérer un state perdu si le versioning GCS est activé ?"
    **Réponse :**

    ```bash
    # 1. Lister les versions du state
    gsutil ls -a gs://worldline-terraform-state/prod/default.tfstate

    # 2. Identifier la version à restaurer (par date)
    # Format: gs://bucket/object#generation

    # 3. Copier la version vers le state actuel
    gsutil cp "gs://worldline-terraform-state/prod/default.tfstate#1706453245123456" \
        gs://worldline-terraform-state/prod/default.tfstate

    # 4. Vérifier
    terraform state pull | jq '.serial'
    ```

    C'est pourquoi le **versioning GCS est obligatoire** pour les buckets de state.

---

## Prochaine Étape

Maintenant que votre state est sécurisé et partagé, apprenez à structurer votre code Terraform pour le rendre maintenable.

[:octicons-arrow-right-24: Module 2 : Structure & Modules](02-modules-structure.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Intermédiaire
