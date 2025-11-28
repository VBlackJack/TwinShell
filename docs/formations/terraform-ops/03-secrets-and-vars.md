# Module 3 : Secrets & Variables

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Distinguer variables sensibles et non-sensibles
- :material-check: Utiliser les fichiers `.tfvars` de manière sécurisée
- :material-check: Injecter des secrets via variables d'environnement (`TF_VAR_*`)
- :material-check: Intégrer GCP Secret Manager avec Terraform

---

## 1. Variables Terraform : Les Bases

### 1.1 Déclaration de Variables

```hcl
# variables.tf

# Variable simple avec valeur par défaut
variable "region" {
  description = "Région GCP de déploiement"
  type        = string
  default     = "europe-west1"
}

# Variable obligatoire (pas de default)
variable "project_id" {
  description = "ID du projet GCP"
  type        = string
}

# Variable avec validation
variable "environment" {
  description = "Environnement de déploiement"
  type        = string

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "L'environnement doit être dev, staging ou prod."
  }
}

# Variable sensible (masquée dans les logs)
variable "db_password" {
  description = "Mot de passe de la base de données"
  type        = string
  sensitive   = true
}

# Variable complexe (object)
variable "gke_config" {
  description = "Configuration du cluster GKE"
  type = object({
    node_count   = number
    machine_type = string
    disk_size_gb = number
    preemptible  = bool
  })
  default = {
    node_count   = 3
    machine_type = "e2-standard-2"
    disk_size_gb = 100
    preemptible  = false
  }
}
```

### 1.2 Ordre de Précédence des Variables

```
┌─────────────────────────────────────────────────────────────────┐
│              ORDRE DE PRÉCÉDENCE DES VARIABLES                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Priorité BASSE → HAUTE                                        │
│   ───────────────────────                                       │
│                                                                 │
│   1. default dans variable "xxx" { default = "..." }            │
│      └─ Valeur par défaut si rien d'autre                       │
│                                                                 │
│   2. terraform.tfvars (chargé automatiquement)                  │
│      └─ Fichier par défaut dans le répertoire courant           │
│                                                                 │
│   3. *.auto.tfvars (chargé automatiquement, ordre alphabétique) │
│      └─ 01-network.auto.tfvars, 02-gke.auto.tfvars              │
│                                                                 │
│   4. -var-file="custom.tfvars" (ligne de commande)              │
│      └─ terraform apply -var-file="prod.tfvars"                 │
│                                                                 │
│   5. TF_VAR_xxx (variable d'environnement)                      │
│      └─ export TF_VAR_db_password="secret"                      │
│                                                                 │
│   6. -var="xxx=value" (ligne de commande)                       │
│      └─ terraform apply -var="region=europe-west1"              │
│                                                                 │
│   ⚠️  Le DERNIER gagne (écrase les précédents)                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. La Règle d'Or : Séparer Sensible et Non-Sensible

### 2.1 Classification des Variables

```
┌─────────────────────────────────────────────────────────────────┐
│              CLASSIFICATION DES VARIABLES                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   NON-SENSIBLE (peut être commité dans Git)                     │
│   ──────────────────────────────────────────                    │
│   • project_id          (visible dans GCP Console)              │
│   • region              (publique)                              │
│   • environment         (dev/staging/prod)                      │
│   • machine_type        (configuration)                         │
│   • node_count          (sizing)                                │
│   • network_name        (publique)                              │
│   • labels/tags         (métadonnées)                           │
│                                                                 │
│   SENSIBLE (NE JAMAIS COMMITER)                                 │
│   ─────────────────────────────                                 │
│   • db_password         (accès direct à la DB)                  │
│   • api_keys            (accès aux services)                    │
│   • private_keys        (authentification)                      │
│   • service_account_key (JSON key - à éviter)                   │
│   • oauth_client_secret (authentification)                      │
│   • encryption_keys     (chiffrement)                           │
│   • tokens              (accès temporaire)                      │
│                                                                 │
│   RÈGLE : Si la valeur permet d'accéder à quelque chose,        │
│           c'est SENSIBLE.                                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Organisation des Fichiers .tfvars

```
environments/prod/
├── main.tf
├── variables.tf
├── backend.tf
│
├── terraform.tfvars        # ❌ À éviter (ambigu)
│
├── config.auto.tfvars      # ✅ Non-sensible (commité)
│   │
│   │  # config.auto.tfvars - PEUT ÊTRE COMMITÉ
│   │  project_id   = "worldline-prod"
│   │  region       = "europe-west1"
│   │  environment  = "prod"
│   │  gke_node_count = 5
│   │
│
└── secrets.tfvars          # ❌ JAMAIS COMMITÉ (.gitignore)
    │
    │  # secrets.tfvars - JAMAIS DANS GIT
    │  db_password      = "SuperSecret123!"
    │  api_key          = "sk-live-xxx..."
    │
```

**.gitignore :**
```gitignore
# Terraform secrets - NEVER COMMIT
secrets.tfvars
*.secrets.tfvars
terraform.tfvars  # Par précaution

# Exceptions explicites si besoin
!config.auto.tfvars
```

---

## 3. Variables d'Environnement : TF_VAR_*

### 3.1 Concept

Terraform lit automatiquement les variables d'environnement préfixées par `TF_VAR_` :

```bash
# Variable: db_password
export TF_VAR_db_password="SuperSecret123!"

# Variable: api_key
export TF_VAR_api_key="sk-live-xxx..."

# Terraform les utilise automatiquement
terraform apply
# → Pas besoin de -var ou -var-file pour ces variables
```

### 3.2 Avantages pour la CI/CD

```
┌─────────────────────────────────────────────────────────────────┐
│              INJECTION DE SECRETS EN CI/CD                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    GitLab CI/CD                         │   │
│   │                                                         │   │
│   │   Settings > CI/CD > Variables                          │   │
│   │   ┌───────────────────────────────────────────────┐     │   │
│   │   │ Key: TF_VAR_db_password                       │     │   │
│   │   │ Value: ••••••••••••••                         │     │   │
│   │   │ [x] Masked                                    │     │   │
│   │   │ [x] Protected (main branch only)              │     │   │
│   │   └───────────────────────────────────────────────┘     │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    Pipeline Job                         │   │
│   │                                                         │   │
│   │   script:                                               │   │
│   │     - terraform init                                    │   │
│   │     - terraform plan -out=tfplan                        │   │
│   │     - terraform apply tfplan                            │   │
│   │                                                         │   │
│   │   # TF_VAR_db_password est automatiquement injecté      │   │
│   │   # par GitLab dans l'environnement du runner           │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ✅ Secrets jamais dans le code                                │
│   ✅ Secrets jamais dans les logs (masked)                      │
│   ✅ Secrets protégés (branches protégées uniquement)           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Exemple GitLab CI

```yaml
# .gitlab-ci.yml

stages:
  - validate
  - plan
  - apply

variables:
  TF_ROOT: environments/prod
  TF_STATE_NAME: prod

.terraform_base:
  image: hashicorp/terraform:1.6
  before_script:
    - cd $TF_ROOT
    - terraform init -backend-config="prefix=$TF_STATE_NAME"

validate:
  extends: .terraform_base
  stage: validate
  script:
    - terraform validate
    - terraform fmt -check

plan:
  extends: .terraform_base
  stage: plan
  script:
    - terraform plan -out=tfplan
  artifacts:
    paths:
      - $TF_ROOT/tfplan
    expire_in: 1 week

apply:
  extends: .terraform_base
  stage: apply
  script:
    - terraform apply -auto-approve tfplan
  dependencies:
    - plan
  when: manual  # Approbation manuelle requise
  only:
    - main
  environment:
    name: production

# Les variables TF_VAR_* sont configurées dans
# Settings > CI/CD > Variables (protected + masked)
```

### 3.4 Script Local Sécurisé

```bash
#!/bin/bash
# deploy.sh - Script de déploiement local sécurisé

set -euo pipefail

# Charger les secrets depuis un fichier local (non commité)
if [[ -f ".secrets.env" ]]; then
    # shellcheck disable=SC1091
    source .secrets.env
else
    echo "ERROR: .secrets.env not found"
    echo "Create it from .secrets.env.example"
    exit 1
fi

# Vérifier que les variables requises sont définies
required_vars=(
    "TF_VAR_db_password"
    "TF_VAR_api_key"
)

for var in "${required_vars[@]}"; do
    if [[ -z "${!var:-}" ]]; then
        echo "ERROR: $var is not set"
        exit 1
    fi
done

# Exécuter Terraform
cd environments/prod
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

**.secrets.env (non commité) :**
```bash
# .secrets.env - JAMAIS DANS GIT
export TF_VAR_db_password="SuperSecret123!"
export TF_VAR_api_key="sk-live-xxx..."
```

**.secrets.env.example (commité) :**
```bash
# .secrets.env.example - Template pour les développeurs
# Copier ce fichier vers .secrets.env et remplir les valeurs
export TF_VAR_db_password=""
export TF_VAR_api_key=""
```

---

## 4. Intégration avec GCP Secret Manager

### 4.1 Pourquoi Secret Manager ?

```
┌─────────────────────────────────────────────────────────────────┐
│              VARIABLES D'ENV vs SECRET MANAGER                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   VARIABLES D'ENVIRONNEMENT (TF_VAR_*)                          │
│   ─────────────────────────────────────                         │
│   ✅ Simple à configurer                                        │
│   ✅ Supporté par toutes les CI/CD                              │
│   ❌ Rotation manuelle                                          │
│   ❌ Pas d'audit centralisé                                     │
│   ❌ Duplication si plusieurs pipelines                         │
│                                                                 │
│   GCP SECRET MANAGER                                            │
│   ──────────────────                                            │
│   ✅ Rotation automatique possible                              │
│   ✅ Audit centralisé (Cloud Audit Logs)                        │
│   ✅ Versionnement des secrets                                  │
│   ✅ IAM granulaire (qui peut lire quel secret)                 │
│   ✅ Source unique de vérité                                    │
│   ❌ Plus complexe à configurer                                 │
│   ❌ Dépendance GCP                                             │
│                                                                 │
│   RECOMMANDATION :                                              │
│   • Secrets "infrastructure" → TF_VAR_* (simples, un par env)   │
│   • Secrets "application" → Secret Manager (partagés, rotés)    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Créer des Secrets dans Secret Manager

```bash
# Créer un secret
echo -n "SuperSecret123!" | gcloud secrets create db-password \
    --project=worldline-prod \
    --replication-policy="user-managed" \
    --locations="europe-west1,europe-west4" \
    --data-file=-

# Ajouter une nouvelle version
echo -n "NewPassword456!" | gcloud secrets versions add db-password \
    --project=worldline-prod \
    --data-file=-

# Lister les versions
gcloud secrets versions list db-password --project=worldline-prod

# Accéder à un secret
gcloud secrets versions access latest --secret=db-password --project=worldline-prod
```

### 4.3 Lire un Secret avec Data Source

```hcl
# data.tf

# Lire un secret depuis Secret Manager
data "google_secret_manager_secret_version" "db_password" {
  project = var.project_id
  secret  = "db-password"
  version = "latest"  # Ou un numéro de version spécifique
}

# Utilisation dans une ressource
resource "google_sql_user" "app_user" {
  project  = var.project_id
  instance = google_sql_database_instance.main.name
  name     = "app"
  password = data.google_secret_manager_secret_version.db_password.secret_data
}
```

!!! warning "Attention au State"
    Même en utilisant Secret Manager, le secret sera stocké dans le state Terraform après le `terraform apply`.

    C'est pourquoi le state doit toujours être protégé (remote backend, chiffrement, IAM).

### 4.4 Créer des Secrets avec Terraform

```hcl
# Créer un secret (le conteneur)
resource "google_secret_manager_secret" "db_password" {
  project   = var.project_id
  secret_id = "db-password"

  replication {
    user_managed {
      replicas {
        location = "europe-west1"
      }
      replicas {
        location = "europe-west4"
      }
    }
  }

  labels = {
    environment = var.environment
    managed_by  = "terraform"
  }
}

# Créer une version du secret (la valeur)
resource "google_secret_manager_secret_version" "db_password_v1" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = var.db_password  # Vient de TF_VAR_db_password

  # Ne pas recréer si la valeur n'a pas changé
  lifecycle {
    ignore_changes = [secret_data]
  }
}

# Donner accès à un service account
resource "google_secret_manager_secret_iam_member" "app_access" {
  project   = var.project_id
  secret_id = google_secret_manager_secret.db_password.secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.app.email}"
}
```

### 4.5 Pattern Complet : Génération et Stockage

```hcl
# Générer un mot de passe aléatoire
resource "random_password" "db_password" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

# Stocker dans Secret Manager
resource "google_secret_manager_secret" "db_password" {
  project   = var.project_id
  secret_id = "${var.environment}-db-password"

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "db_password" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = random_password.db_password.result
}

# Utiliser le secret dans Cloud SQL
resource "google_sql_user" "app" {
  project  = var.project_id
  instance = google_sql_database_instance.main.name
  name     = "app"
  password = random_password.db_password.result
}

# Output pour référence (ne sera pas affiché car sensible)
output "db_password_secret_id" {
  description = "ID du secret dans Secret Manager"
  value       = google_secret_manager_secret.db_password.id
}
```

---

## 5. Variables Sensibles : Bonnes Pratiques

### 5.1 Marquer les Variables comme Sensibles

```hcl
variable "db_password" {
  description = "Mot de passe de la base de données"
  type        = string
  sensitive   = true  # ← CRUCIAL
}

variable "api_key" {
  description = "Clé API du service externe"
  type        = string
  sensitive   = true
}
```

**Effet de `sensitive = true` :**

```bash
$ terraform plan

# Sans sensitive = true :
+ resource "google_sql_user" "app" {
    + password = "SuperSecret123!"  # ❌ Visible !
  }

# Avec sensitive = true :
+ resource "google_sql_user" "app" {
    + password = (sensitive value)  # ✅ Masqué
  }
```

### 5.2 Outputs Sensibles

```hcl
output "db_connection_string" {
  description = "Chaîne de connexion à la DB"
  value       = "postgres://app:${random_password.db.result}@${google_sql_database_instance.main.private_ip_address}/appdb"
  sensitive   = true  # ← Ne sera pas affiché dans les logs
}
```

### 5.3 Éviter les Fuites dans les Logs

```hcl
# ❌ MAUVAIS : Le secret peut apparaître dans les logs
resource "null_resource" "debug" {
  provisioner "local-exec" {
    command = "echo Password is: ${var.db_password}"
  }
}

# ❌ MAUVAIS : Le secret est dans un fichier
resource "local_file" "config" {
  content  = "password=${var.db_password}"
  filename = "config.txt"
}

# ✅ BON : Utiliser Secret Manager dans l'application
resource "google_secret_manager_secret_version" "db_password" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = var.db_password
}
# L'application lit le secret via l'API Secret Manager
```

---

## 6. Checklist Sécurité des Variables

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST SÉCURITÉ DES VARIABLES                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   FICHIERS                                                      │
│   ────────                                                      │
│   ☑ terraform.tfvars dans .gitignore                            │
│   ☑ secrets.tfvars dans .gitignore                              │
│   ☑ *.auto.tfvars pour config non-sensible uniquement           │
│   ☑ .secrets.env.example fourni (template)                      │
│                                                                 │
│   VARIABLES                                                     │
│   ─────────                                                     │
│   ☑ sensitive = true sur toutes les variables sensibles         │
│   ☑ description sur chaque variable                             │
│   ☑ validation sur les variables critiques                      │
│   ☑ Pas de default pour les secrets                             │
│                                                                 │
│   CI/CD                                                         │
│   ─────                                                         │
│   ☑ Secrets en variables CI (masked + protected)                │
│   ☑ Prefix TF_VAR_ pour injection automatique                   │
│   ☑ Pas de secrets dans les scripts ou Dockerfiles              │
│   ☑ Logs sans secrets (vérifier terraform plan output)          │
│                                                                 │
│   SECRET MANAGER                                                │
│   ──────────────                                                │
│   ☑ Secrets applicatifs dans Secret Manager                     │
│   ☑ IAM restrictif (least privilege)                            │
│   ☑ Rotation planifiée pour les secrets critiques               │
│   ☑ Audit logs activés                                          │
│                                                                 │
│   STATE                                                         │
│   ─────                                                         │
│   ☑ Remote backend obligatoire                                  │
│   ☑ Chiffrement at-rest                                         │
│   ☑ IAM restrictif sur le bucket                                │
│   ☑ Versioning activé                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Commandes de Référence

```bash
# === VARIABLES ===
# Voir les variables requises
terraform console
> var.project_id  # Error si non définie

# Passer une variable en ligne de commande
terraform plan -var="environment=dev"

# Utiliser un fichier de variables spécifique
terraform plan -var-file="prod.tfvars"

# === VARIABLES D'ENVIRONNEMENT ===
# Définir une variable
export TF_VAR_db_password="secret"

# Vérifier qu'elle est définie
echo $TF_VAR_db_password

# Lister toutes les TF_VAR_*
env | grep TF_VAR_

# === SECRET MANAGER ===
# Créer un secret
echo -n "password" | gcloud secrets create my-secret --data-file=-

# Lire un secret
gcloud secrets versions access latest --secret=my-secret

# Lister les secrets
gcloud secrets list

# Donner accès à un service account
gcloud secrets add-iam-policy-binding my-secret \
    --member="serviceAccount:app@project.iam.gserviceaccount.com" \
    --role="roles/secretmanager.secretAccessor"

# === DEBUG ===
# Vérifier la valeur d'une variable (avec précaution)
terraform console
> var.region
"europe-west1"

# Voir le plan sans appliquer
terraform plan -out=tfplan

# Inspecter le plan
terraform show tfplan
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi ne pas commiter terraform.tfvars ?"
    **Réponse :**

    Le fichier `terraform.tfvars` est **automatiquement chargé** par Terraform. Si des secrets y sont stockés :

    1. Ils seront dans l'historique Git **pour toujours**
    2. Même après suppression du fichier, ils restent dans `git log`
    3. Tout clone du repo expose les secrets
    4. Les secrets peuvent fuiter via des forks publics

    **Solution** :
    - Utiliser `*.auto.tfvars` pour les configs non-sensibles
    - Utiliser `TF_VAR_*` pour les secrets
    - Ajouter `terraform.tfvars` dans `.gitignore` par précaution

??? question "Question 2 : Quelle est la différence entre TF_VAR_* et Secret Manager ?"
    **Réponse :**

    | Aspect | TF_VAR_* | Secret Manager |
    |--------|----------|----------------|
    | **Scope** | Pipeline/Environnement | Application/Organisation |
    | **Rotation** | Manuelle | Automatisable |
    | **Audit** | Logs CI/CD | Cloud Audit Logs |
    | **Versioning** | Non | Oui |
    | **Accès runtime** | Non (build-time) | Oui (runtime) |

    **Quand utiliser quoi :**
    - **TF_VAR_*** : Secrets Terraform (mots de passe initiaux, API keys pour providers)
    - **Secret Manager** : Secrets applicatifs (accédés par les pods/VMs au runtime)

??? question "Question 3 : Le secret est-il en sécurité s'il est dans Secret Manager ?"
    **Réponse :**

    **Partiellement.** Le secret est sécurisé dans Secret Manager, mais :

    1. **Le state Terraform** contient le secret après `apply`
    2. **Les data sources** ramènent le secret dans le state
    3. **Le plan** peut afficher le secret (d'où `sensitive = true`)

    **Mitigation** :
    - State chiffré et accès restreint
    - Variables marquées `sensitive = true`
    - Générer les secrets avec `random_password` + stockage dans Secret Manager
    - L'application lit depuis Secret Manager (pas depuis le state)

---

## Conclusion de la Formation

Vous maîtrisez maintenant les fondamentaux de Terraform en production :

- **Module 1** : State Management (Remote Backend, Locking)
- **Module 2** : Structure & Modules (DRY, Versioning)
- **Module 3** : Secrets & Variables (TF_VAR_*, Secret Manager)

### Prochaines Étapes Recommandées

1. **Implémenter un Remote Backend** sur votre projet existant
2. **Refactorer en modules** un code monolithique
3. **Migrer les secrets** vers des variables d'environnement CI/CD
4. **Explorer** : Terratest, Atlantis, Terraform Cloud

### Ressources Complémentaires

- :material-link: [Terraform Best Practices](https://developer.hashicorp.com/terraform/cloud-docs/recommended-practices)
- :material-link: [Google Cloud Provider](https://registry.terraform.io/providers/hashicorp/google/latest/docs)
- :material-link: [Terraform Security](https://developer.hashicorp.com/terraform/enterprise/security)

---

**Temps estimé :** 60 minutes
**Niveau :** Avancé
