# Module 2 : Structure & Modules

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Organiser un projet Terraform de manière maintenable
- :material-check: Créer des modules réutilisables
- :material-check: Gérer plusieurs environnements (dev, staging, prod)
- :material-check: Verrouiller les versions des providers et modules

---

## 1. Le Problème : Le Fichier Monolithique

### 1.1 L'Anti-Pattern du main.tf Géant

```
┌─────────────────────────────────────────────────────────────────┐
│              L'ANTI-PATTERN : TOUT DANS main.tf                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   projet/                                                       │
│   └── main.tf (2847 lignes)                                     │
│       │                                                         │
│       ├── Lines 1-150     : Variables                           │
│       ├── Lines 151-400   : Provider config                     │
│       ├── Lines 401-800   : VPC + Subnets + Firewall           │
│       ├── Lines 801-1200  : GKE Cluster + Node Pools           │
│       ├── Lines 1201-1600 : Cloud SQL + Users + DBs            │
│       ├── Lines 1601-2000 : IAM Bindings                       │
│       ├── Lines 2001-2400 : Pub/Sub + Cloud Functions          │
│       └── Lines 2401-2847 : Outputs                            │
│                                                                 │
│   PROBLÈMES :                                                   │
│   ─────────                                                     │
│   ❌ Impossible à lire (scrolling infini)                       │
│   ❌ Conflits Git constants (tout le monde touche le même fichier)│
│   ❌ Pas de réutilisation (copier-coller entre projets)         │
│   ❌ Difficile à tester                                         │
│   ❌ Blast radius maximum (un changement = tout re-plan)        │
│   ❌ Onboarding cauchemardesque pour les nouveaux               │
│                                                                 │
│   "Mais ça marchait bien au début..."                           │
│   Oui, jusqu'à ce que le projet grandisse.                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Les Signaux d'Alerte

Votre code Terraform a besoin de refactoring si :

| Signal | Seuil Critique |
|--------|----------------|
| Taille de `main.tf` | > 500 lignes |
| Temps de `terraform plan` | > 2 minutes |
| Copier-coller entre projets | Plus de 2 fois |
| Conflits Git | À chaque PR |
| Nouveaux développeurs | "Je ne comprends rien" |

---

## 2. La Solution : Structure Modulaire

### 2.1 Structure de Projet Recommandée

```
┌─────────────────────────────────────────────────────────────────┐
│              STRUCTURE PROJET TERRAFORM WORLDLINE                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   terraform-infra/                                              │
│   │                                                             │
│   ├── modules/                    # Briques réutilisables       │
│   │   ├── vpc/                                                  │
│   │   │   ├── main.tf                                           │
│   │   │   ├── variables.tf                                      │
│   │   │   ├── outputs.tf                                        │
│   │   │   └── README.md                                         │
│   │   │                                                         │
│   │   ├── gke/                                                  │
│   │   │   ├── main.tf                                           │
│   │   │   ├── variables.tf                                      │
│   │   │   ├── outputs.tf                                        │
│   │   │   └── README.md                                         │
│   │   │                                                         │
│   │   └── cloudsql/                                             │
│   │       ├── main.tf                                           │
│   │       ├── variables.tf                                      │
│   │       ├── outputs.tf                                        │
│   │       └── README.md                                         │
│   │                                                             │
│   ├── environments/               # Déploiements par env        │
│   │   ├── dev/                                                  │
│   │   │   ├── main.tf             # Appelle les modules         │
│   │   │   ├── backend.tf          # Backend GCS (dev state)     │
│   │   │   ├── variables.tf                                      │
│   │   │   ├── terraform.tfvars    # Valeurs dev                 │
│   │   │   └── outputs.tf                                        │
│   │   │                                                         │
│   │   ├── staging/                                              │
│   │   │   ├── main.tf                                           │
│   │   │   ├── backend.tf          # Backend GCS (staging state) │
│   │   │   ├── variables.tf                                      │
│   │   │   ├── terraform.tfvars                                  │
│   │   │   └── outputs.tf                                        │
│   │   │                                                         │
│   │   └── prod/                                                 │
│   │       ├── main.tf                                           │
│   │       ├── backend.tf          # Backend GCS (prod state)    │
│   │       ├── variables.tf                                      │
│   │       ├── terraform.tfvars                                  │
│   │       └── outputs.tf                                        │
│   │                                                             │
│   ├── .gitignore                                                │
│   ├── .terraform-version          # Version Terraform (tfenv)   │
│   └── README.md                                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Conventions de Fichiers

| Fichier | Contenu | Obligatoire |
|---------|---------|-------------|
| `main.tf` | Ressources et appels de modules | Oui |
| `variables.tf` | Déclaration des variables d'entrée | Oui |
| `outputs.tf` | Valeurs exposées | Oui |
| `backend.tf` | Configuration du remote state | Oui (env) |
| `providers.tf` | Configuration des providers | Oui |
| `versions.tf` | Contraintes de versions | Recommandé |
| `locals.tf` | Variables locales calculées | Optionnel |
| `data.tf` | Data sources | Optionnel |

---

## 3. Création de Modules Réutilisables

### 3.1 Anatomie d'un Module

```
┌─────────────────────────────────────────────────────────────────┐
│              ANATOMIE D'UN MODULE TERRAFORM                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   modules/vpc/                                                  │
│   │                                                             │
│   ├── main.tf         # Ressources du module                    │
│   │   │                                                         │
│   │   │  resource "google_compute_network" "main" {             │
│   │   │    name                    = var.name                   │
│   │   │    auto_create_subnetworks = false                      │
│   │   │  }                                                      │
│   │   │                                                         │
│   │   │  resource "google_compute_subnetwork" "subnets" {       │
│   │   │    for_each      = var.subnets                          │
│   │   │    name          = each.key                             │
│   │   │    ip_cidr_range = each.value.cidr                      │
│   │   │    region        = each.value.region                    │
│   │   │    network       = google_compute_network.main.id       │
│   │   │  }                                                      │
│   │   │                                                         │
│   │                                                             │
│   ├── variables.tf    # Inputs (paramètres)                     │
│   │   │                                                         │
│   │   │  variable "name" {                                      │
│   │   │    description = "Nom du VPC"                           │
│   │   │    type        = string                                 │
│   │   │  }                                                      │
│   │   │                                                         │
│   │   │  variable "subnets" {                                   │
│   │   │    description = "Map des subnets"                      │
│   │   │    type = map(object({                                  │
│   │   │      cidr   = string                                    │
│   │   │      region = string                                    │
│   │   │    }))                                                  │
│   │   │  }                                                      │
│   │   │                                                         │
│   │                                                             │
│   ├── outputs.tf      # Outputs (valeurs exposées)              │
│   │   │                                                         │
│   │   │  output "network_id" {                                  │
│   │   │    description = "ID du VPC"                            │
│   │   │    value       = google_compute_network.main.id         │
│   │   │  }                                                      │
│   │   │                                                         │
│   │   │  output "subnet_ids" {                                  │
│   │   │    description = "Map des IDs de subnets"               │
│   │   │    value = {                                            │
│   │   │      for k, v in google_compute_subnetwork.subnets :    │
│   │   │      k => v.id                                          │
│   │   │    }                                                    │
│   │   │  }                                                      │
│   │   │                                                         │
│   │                                                             │
│   └── README.md       # Documentation du module                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Exemple Complet : Module VPC

**modules/vpc/main.tf**
```hcl
# modules/vpc/main.tf

resource "google_compute_network" "main" {
  name                    = var.name
  project                 = var.project_id
  auto_create_subnetworks = false
  routing_mode            = var.routing_mode
  description             = var.description
}

resource "google_compute_subnetwork" "subnets" {
  for_each = var.subnets

  name                     = each.key
  project                  = var.project_id
  network                  = google_compute_network.main.id
  ip_cidr_range           = each.value.cidr
  region                  = each.value.region
  private_ip_google_access = true

  dynamic "secondary_ip_range" {
    for_each = lookup(each.value, "secondary_ranges", {})
    content {
      range_name    = secondary_ip_range.key
      ip_cidr_range = secondary_ip_range.value
    }
  }

  dynamic "log_config" {
    for_each = var.flow_logs_enabled ? [1] : []
    content {
      aggregation_interval = "INTERVAL_5_SEC"
      flow_sampling        = 0.5
      metadata             = "INCLUDE_ALL_METADATA"
    }
  }
}

resource "google_compute_router" "router" {
  for_each = var.create_nat ? var.subnets : {}

  name    = "${each.key}-router"
  project = var.project_id
  network = google_compute_network.main.id
  region  = each.value.region
}

resource "google_compute_router_nat" "nat" {
  for_each = var.create_nat ? var.subnets : {}

  name                               = "${each.key}-nat"
  project                            = var.project_id
  router                             = google_compute_router.router[each.key].name
  region                             = each.value.region
  nat_ip_allocate_option            = "AUTO_ONLY"
  source_subnetwork_ip_ranges_to_nat = "ALL_SUBNETWORKS_ALL_IP_RANGES"

  log_config {
    enable = true
    filter = "ERRORS_ONLY"
  }
}
```

**modules/vpc/variables.tf**
```hcl
# modules/vpc/variables.tf

variable "project_id" {
  description = "ID du projet GCP"
  type        = string
}

variable "name" {
  description = "Nom du VPC"
  type        = string

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{0,62}$", var.name))
    error_message = "Le nom du VPC doit respecter les conventions GCP."
  }
}

variable "description" {
  description = "Description du VPC"
  type        = string
  default     = "VPC créé par Terraform"
}

variable "routing_mode" {
  description = "Mode de routage (REGIONAL ou GLOBAL)"
  type        = string
  default     = "GLOBAL"

  validation {
    condition     = contains(["REGIONAL", "GLOBAL"], var.routing_mode)
    error_message = "routing_mode doit être REGIONAL ou GLOBAL."
  }
}

variable "subnets" {
  description = "Map des subnets à créer"
  type = map(object({
    cidr             = string
    region           = string
    secondary_ranges = optional(map(string), {})
  }))
}

variable "flow_logs_enabled" {
  description = "Activer les Flow Logs VPC"
  type        = bool
  default     = false
}

variable "create_nat" {
  description = "Créer un Cloud NAT pour chaque subnet"
  type        = bool
  default     = true
}
```

**modules/vpc/outputs.tf**
```hcl
# modules/vpc/outputs.tf

output "network_id" {
  description = "ID du VPC"
  value       = google_compute_network.main.id
}

output "network_name" {
  description = "Nom du VPC"
  value       = google_compute_network.main.name
}

output "network_self_link" {
  description = "Self-link du VPC"
  value       = google_compute_network.main.self_link
}

output "subnet_ids" {
  description = "Map des IDs de subnets"
  value = {
    for k, v in google_compute_subnetwork.subnets :
    k => v.id
  }
}

output "subnet_self_links" {
  description = "Map des self-links de subnets"
  value = {
    for k, v in google_compute_subnetwork.subnets :
    k => v.self_link
  }
}

output "nat_ips" {
  description = "IPs NAT allouées"
  value = {
    for k, v in google_compute_router_nat.nat :
    k => v.nat_ips
  }
}
```

### 3.3 Appel du Module depuis un Environnement

**environments/prod/main.tf**
```hcl
# environments/prod/main.tf

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

# Appel du module VPC
module "vpc" {
  source = "../../modules/vpc"

  project_id  = var.project_id
  name        = "worldline-prod-vpc"
  description = "VPC Production Worldline"

  subnets = {
    "prod-gke-subnet" = {
      cidr   = "10.0.0.0/20"
      region = "europe-west1"
      secondary_ranges = {
        "pods"     = "10.1.0.0/16"
        "services" = "10.2.0.0/20"
      }
    }
    "prod-db-subnet" = {
      cidr   = "10.0.16.0/24"
      region = "europe-west1"
    }
  }

  flow_logs_enabled = true
  create_nat        = true
}

# Appel du module GKE (utilise les outputs du VPC)
module "gke" {
  source = "../../modules/gke"

  project_id     = var.project_id
  cluster_name   = "worldline-prod-gke"
  region         = "europe-west1"
  network_id     = module.vpc.network_id
  subnet_id      = module.vpc.subnet_ids["prod-gke-subnet"]
  pods_range     = "pods"
  services_range = "services"

  # Configuration spécifique prod
  node_count     = 3
  machine_type   = "e2-standard-4"
  enable_autopilot = false
}

# Appel du module Cloud SQL
module "cloudsql" {
  source = "../../modules/cloudsql"

  project_id    = var.project_id
  instance_name = "worldline-prod-db"
  region        = "europe-west1"
  network_id    = module.vpc.network_id

  database_version = "POSTGRES_15"
  tier             = "db-custom-4-16384"  # 4 vCPU, 16GB RAM

  # HA pour la prod
  availability_type = "REGIONAL"

  databases = ["app_db", "analytics_db"]
}
```

---

## 4. Gestion des Versions

### 4.1 Pourquoi Verrouiller les Versions ?

```
┌─────────────────────────────────────────────────────────────────┐
│              LE DANGER DES VERSIONS FLOTTANTES                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Lundi :  Alice déploie avec google provider 5.0.0             │
│            → terraform apply ✅                                 │
│                                                                 │
│   Mardi :  HashiCorp publie google provider 5.1.0               │
│            → Nouveau comportement sur les firewalls             │
│                                                                 │
│   Mercredi : Bob fait terraform init (télécharge 5.1.0)         │
│              terraform plan                                     │
│              → "15 resources will be DESTROYED"                 │
│              → 😱                                               │
│                                                                 │
│   CAUSE : Pas de version pinning                                │
│   SOLUTION : Verrouiller les versions explicitement             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Version Constraints

**versions.tf**
```hcl
# versions.tf

terraform {
  # Version Terraform minimale requise
  required_version = ">= 1.5.0, < 2.0.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"  # >= 5.0.0, < 6.0.0
    }
    google-beta = {
      source  = "hashicorp/google-beta"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = ">= 3.0.0, < 4.0.0"
    }
  }
}
```

**Syntaxe des Contraintes :**

| Syntaxe | Signification | Exemple |
|---------|---------------|---------|
| `= 5.0.0` | Version exacte | Uniquement 5.0.0 |
| `>= 5.0.0` | Minimum | 5.0.0, 5.1.0, 6.0.0... |
| `~> 5.0` | Pessimistic (patch) | >= 5.0.0, < 6.0.0 |
| `~> 5.0.0` | Pessimistic (minor) | >= 5.0.0, < 5.1.0 |
| `>= 5.0, < 6.0` | Range | 5.x uniquement |

!!! success "Recommandation Worldline"
    Utilisez `~> X.0` pour les providers (permet les mises à jour mineures).

    Utilisez des versions exactes pour les modules internes.

### 4.3 Le Fichier .terraform.lock.hcl

Terraform génère automatiquement un fichier de lock :

```hcl
# .terraform.lock.hcl - DOIT ÊTRE COMMITÉ DANS GIT

provider "registry.terraform.io/hashicorp/google" {
  version     = "5.12.0"
  constraints = "~> 5.0"
  hashes = [
    "h1:ABC123...",
    "zh:DEF456...",
  ]
}
```

```bash
# Mettre à jour le lock file après changement de version
terraform init -upgrade

# Vérifier l'intégrité
terraform providers lock -platform=linux_amd64 -platform=darwin_amd64
```

!!! warning "Commit le Lock File"
    Le fichier `.terraform.lock.hcl` **DOIT** être versionné dans Git.

    Il garantit que toute l'équipe utilise exactement les mêmes versions de providers.

### 4.4 Versionnement des Modules

**Modules Locaux :**
```hcl
module "vpc" {
  source = "../../modules/vpc"  # Chemin relatif, pas de version
}
```

**Modules Git :**
```hcl
module "vpc" {
  source = "git::https://github.com/worldline/terraform-modules.git//vpc?ref=v1.2.0"
}
```

**Modules Registry (Terraform Cloud ou privé) :**
```hcl
module "vpc" {
  source  = "terraform-google-modules/network/google"
  version = "~> 7.0"
}
```

---

## 5. Bonnes Pratiques de Structure

### 5.1 Checklist Organisation

```
┌─────────────────────────────────────────────────────────────────┐
│              CHECKLIST STRUCTURE TERRAFORM                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   FICHIERS                                                      │
│   ────────                                                      │
│   ☑ Un fichier par type (main, variables, outputs, etc.)        │
│   ☑ Modules dans un dossier dédié /modules                      │
│   ☑ Environnements séparés /environments/{dev,staging,prod}     │
│   ☑ README.md à la racine et dans chaque module                 │
│                                                                 │
│   MODULES                                                       │
│   ───────                                                       │
│   ☑ Module = une responsabilité (VPC, GKE, SQL)                 │
│   ☑ Variables documentées (description obligatoire)             │
│   ☑ Validations sur les variables critiques                     │
│   ☑ Outputs pour tout ce qui peut être référencé                │
│                                                                 │
│   VERSIONS                                                      │
│   ────────                                                      │
│   ☑ required_version dans tous les environnements               │
│   ☑ required_providers avec contraintes explicites              │
│   ☑ .terraform.lock.hcl commité dans Git                        │
│   ☑ Modules Git avec tags (pas de ref=main)                     │
│                                                                 │
│   NOMMAGE                                                       │
│   ───────                                                       │
│   ☑ Ressources : {projet}-{env}-{type} (worldline-prod-vpc)     │
│   ☑ Variables : snake_case (project_id, cluster_name)           │
│   ☑ Modules : snake_case (module "gke_cluster")                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Documentation Automatique avec terraform-docs

```bash
# Installer terraform-docs
brew install terraform-docs

# Générer la documentation d'un module
cd modules/vpc
terraform-docs markdown . > README.md
```

**Exemple de README généré :**

```markdown
# VPC Module

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| project_id | ID du projet GCP | `string` | n/a | yes |
| name | Nom du VPC | `string` | n/a | yes |
| subnets | Map des subnets | `map(object(...))` | n/a | yes |
| flow_logs_enabled | Activer les Flow Logs | `bool` | `false` | no |

## Outputs

| Name | Description |
|------|-------------|
| network_id | ID du VPC |
| subnet_ids | Map des IDs de subnets |
```

---

## 6. Commandes de Référence

```bash
# === STRUCTURE ===
# Valider la syntaxe de tous les fichiers
terraform validate

# Formater récursivement
terraform fmt -recursive

# Vérifier le formatage (CI/CD)
terraform fmt -check -recursive

# === MODULES ===
# Télécharger les modules
terraform get

# Mettre à jour les modules
terraform get -update

# Voir le graphe de dépendances
terraform graph | dot -Tpng > graph.png

# === VERSIONS ===
# Mettre à jour les providers
terraform init -upgrade

# Verrouiller pour plusieurs plateformes
terraform providers lock \
    -platform=linux_amd64 \
    -platform=darwin_amd64 \
    -platform=windows_amd64

# === DOCUMENTATION ===
# Générer la doc d'un module
terraform-docs markdown table . > README.md

# Générer avec template custom
terraform-docs markdown table --output-file README.md .
```

---

## Quiz d'Auto-Évaluation

??? question "Question 1 : Pourquoi séparer les modules des environnements ?"
    **Réponse :**

    La séparation modules/environnements applique le principe **DRY** (Don't Repeat Yourself) :

    1. **Modules** : Code réutilisable, testé, versionné
    2. **Environnements** : Configuration spécifique (tailles, nombres, options)

    **Avantages** :
    - Un bug corrigé dans un module bénéficie à tous les environnements
    - Cohérence entre dev/staging/prod
    - Review de code centralisée sur les modules
    - Tests unitaires possibles sur les modules

??? question "Question 2 : Que signifie la contrainte `~> 5.0` ?"
    **Réponse :**

    `~> 5.0` est une contrainte **pessimistic** qui signifie :

    - Minimum : `>= 5.0.0`
    - Maximum : `< 6.0.0`

    Donc versions acceptées : 5.0.0, 5.0.1, 5.1.0, 5.99.99...

    **Différence avec `~> 5.0.0`** :
    - `~> 5.0.0` = `>= 5.0.0, < 5.1.0` (plus restrictif)

??? question "Question 3 : Le fichier .terraform.lock.hcl doit-il être commité ?"
    **Réponse :**

    **OUI, absolument.**

    Ce fichier contient :
    - Les versions exactes des providers téléchargés
    - Les checksums (hashes) pour vérification d'intégrité

    **Si non commité** :
    - Chaque développeur peut avoir des versions différentes
    - Les builds CI/CD ne sont pas reproductibles
    - Risque de régressions silencieuses

    **Règle** : Toujours commit `.terraform.lock.hcl`, jamais commit `.terraform/`.

---

## Prochaine Étape

Votre code est maintenant structuré. Apprenez à gérer les secrets de manière sécurisée.

[:octicons-arrow-right-24: Module 3 : Secrets & Variables](03-secrets-and-vars.md)

---

**Temps estimé :** 75 minutes
**Niveau :** Intermédiaire
