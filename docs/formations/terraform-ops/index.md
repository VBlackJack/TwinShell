# Formation : Terraform & Industrialisation

## Introduction

**Infrastructure is Code.**

Cette phrase n'est pas un slogan marketing. C'est un changement de paradigme fondamental dans la façon dont nous concevons, déployons et maintenons l'infrastructure. En environnement régulé (Worldline, SecNumCloud), l'Infrastructure as Code (IaC) n'est pas optionnelle : elle est **obligatoire**.

!!! danger "Ce que cette formation N'EST PAS"
    Cette formation n'est pas un tutoriel "Hello World" pour déployer une VM en 5 minutes.

    Elle traite de **Terraform en production** : gestion du state, collaboration d'équipe, sécurité des secrets, et structure de code maintenable à l'échelle de l'entreprise.

---

## Pourquoi Terraform ?

### Le Problème du ClickOps

```
┌─────────────────────────────────────────────────────────────────┐
│              CLICKOPS vs INFRASTRUCTURE AS CODE                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   CLICKOPS (Console Web)                                        │
│   ──────────────────────                                        │
│   • Ingénieur A crée une VM via la console                      │
│   • Ingénieur B modifie le firewall manuellement                │
│   • 6 mois plus tard : "Qui a créé ça ? Pourquoi ?"            │
│   • Recréer l'environnement ? Impossible.                       │
│   • Audit de conformité ? Cauchemar.                            │
│                                                                 │
│   ❌ Non reproductible                                          │
│   ❌ Non auditable                                              │
│   ❌ Non versionné                                              │
│   ❌ Drift incontrôlé                                           │
│                                                                 │
│   INFRASTRUCTURE AS CODE (Terraform)                            │
│   ────────────────────────────────────                          │
│   • Tout est décrit dans des fichiers .tf                       │
│   • Versionné dans Git (historique complet)                     │
│   • Review de code = Review d'infrastructure                    │
│   • Recréer l'environnement ? terraform apply                   │
│   • Audit de conformité ? git log + terraform state             │
│                                                                 │
│   ✅ Reproductible                                              │
│   ✅ Auditable                                                  │
│   ✅ Versionné                                                  │
│   ✅ Drift détectable                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Terraform vs Alternatives

| Outil | Type | Cloud Support | Maturité |
|-------|------|---------------|----------|
| **Terraform** | Déclaratif, Multi-cloud | AWS, GCP, Azure, +3000 providers | Production |
| Pulumi | Impératif (Python, TS) | Multi-cloud | Production |
| CloudFormation | Déclaratif | AWS uniquement | Production |
| Deployment Manager | Déclaratif | GCP uniquement | Legacy |
| OpenTofu | Fork Terraform | Multi-cloud | Émergent |

!!! success "Choix Worldline : Terraform"
    Terraform est le standard de facto pour l'IaC multi-cloud. Sa syntaxe déclarative (HCL) et son écosystème de providers en font l'outil de référence.

---

## Le Cycle de Vie Terraform

### Write → Plan → Apply

```
┌─────────────────────────────────────────────────────────────────┐
│              CYCLE DE VIE TERRAFORM                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. WRITE (Écrire)                                             │
│   ─────────────────                                             │
│   Décrire l'infrastructure désirée en HCL                       │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │  resource "google_compute_instance" "web" {             │   │
│   │    name         = "web-server"                          │   │
│   │    machine_type = "e2-medium"                           │   │
│   │    zone         = "europe-west1-b"                      │   │
│   │  }                                                      │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   2. PLAN (Planifier)                                           │
│   ───────────────────                                           │
│   Terraform compare l'état désiré vs l'état actuel              │
│                                                                 │
│   $ terraform plan                                              │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │  + google_compute_instance.web                          │   │
│   │      + name         = "web-server"                      │   │
│   │      + machine_type = "e2-medium"                       │   │
│   │                                                         │   │
│   │  Plan: 1 to add, 0 to change, 0 to destroy.            │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   3. APPLY (Appliquer)                                          │
│   ────────────────────                                          │
│   Exécuter les changements après validation                     │
│                                                                 │
│   $ terraform apply                                             │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │  google_compute_instance.web: Creating...               │   │
│   │  google_compute_instance.web: Creation complete [32s]   │   │
│   │                                                         │   │
│   │  Apply complete! Resources: 1 added, 0 changed.         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    STATE FILE                           │   │
│   │   terraform.tfstate                                     │   │
│   │   (Représentation de l'infrastructure réelle)           │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Les Commandes Essentielles

```bash
# Initialiser le projet (télécharge les providers)
terraform init

# Valider la syntaxe
terraform validate

# Formater le code (style canonique)
terraform fmt -recursive

# Planifier les changements (LECTURE SEULE)
terraform plan -out=tfplan

# Appliquer les changements
terraform apply tfplan

# Voir l'état actuel
terraform state list
terraform state show google_compute_instance.web

# Détruire l'infrastructure
terraform destroy
```

---

## Infrastructure Immutable

### Le Concept

L'**Infrastructure Immutable** est un paradigme où les serveurs ne sont jamais modifiés après leur création. Au lieu de patcher un serveur, on le remplace par un nouveau.

```
┌─────────────────────────────────────────────────────────────────┐
│              MUTABLE vs IMMUTABLE INFRASTRUCTURE                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   INFRASTRUCTURE MUTABLE (Pets)                                 │
│   ─────────────────────────────                                 │
│                                                                 │
│   Serveur "web-prod-01" créé en 2020                           │
│        │                                                        │
│        ├── Patch sécurité (2020-06)                            │
│        ├── Upgrade Apache (2021-01)                            │
│        ├── Fix hotfix (2021-03)                                │
│        ├── Nouveau certificat (2021-12)                        │
│        ├── Migration PHP (2022-06)                             │
│        └── ???                                                  │
│                                                                 │
│   ⚠️  Configuration Drift : l'état réel != l'état documenté     │
│   ⚠️  "Snowflake Server" : impossible à recréer                 │
│                                                                 │
│   ─────────────────────────────────────────────────────────────│
│                                                                 │
│   INFRASTRUCTURE IMMUTABLE (Cattle)                             │
│   ──────────────────────────────────                            │
│                                                                 │
│   Image v1.0 ──► Serveur A (détruit)                           │
│   Image v1.1 ──► Serveur B (détruit)                           │
│   Image v1.2 ──► Serveur C (actif)                             │
│                                                                 │
│   ✅ Chaque serveur est identique à sa définition              │
│   ✅ Rollback = Redéployer l'image précédente                  │
│   ✅ Pas de configuration drift                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Application avec Terraform

```hcl
# Au lieu de modifier une VM existante...
resource "google_compute_instance" "web" {
  name         = "web-${var.version}"  # Nouvelle VM à chaque version
  machine_type = "e2-medium"

  boot_disk {
    initialize_params {
      image = "projects/my-project/global/images/web-${var.image_version}"
    }
  }

  # Forcer le remplacement si l'image change
  lifecycle {
    create_before_destroy = true
  }
}
```

!!! tip "Golden Images avec Packer"
    Pour l'infrastructure immutable, combinez Terraform avec **Packer** :

    1. **Packer** : Construit des images VM standardisées (Golden Images)
    2. **Terraform** : Déploie ces images de manière déclarative

---

## Syllabus de la Formation

Cette formation est organisée en **3 modules** :

### Module 1 : Gestion du State
:material-database-lock: **Le Risque N°1** | :material-clock-outline: ~60 min

- Comprendre le `terraform.tfstate`
- Remote Backend (GCS)
- State Locking pour la collaboration

[:octicons-arrow-right-24: Accéder au Module 1](01-state-management.md)

---

### Module 2 : Structure & Modules
:material-folder-multiple: **Principe DRY** | :material-clock-outline: ~75 min

- Organisation d'un projet Terraform
- Modules réutilisables
- Gestion des versions de providers

[:octicons-arrow-right-24: Accéder au Module 2](02-modules-structure.md)

---

### Module 3 : Secrets & Variables
:material-shield-key: **Sécurité** | :material-clock-outline: ~60 min

- Variables sensibles et non-sensibles
- Intégration avec GCP Secret Manager
- Variables d'environnement (`TF_VAR_*`)

[:octicons-arrow-right-24: Accéder au Module 3](03-secrets-and-vars.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **GCP Fundamentals** : Projets, IAM, VPC, Service Accounts
    - **CLI** : `gcloud` et `terraform` installés
    - **Git** : Branches, commits, pull requests
    - **Concepts Cloud** : VMs, réseaux, stockage

### Installation Terraform

=== "macOS"

    ```bash
    brew tap hashicorp/tap
    brew install hashicorp/tap/terraform

    # Vérifier
    terraform version
    ```

=== "Linux (Debian/Ubuntu)"

    ```bash
    wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
    echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
    sudo apt update && sudo apt install terraform

    # Vérifier
    terraform version
    ```

=== "Windows"

    ```powershell
    # Via Chocolatey
    choco install terraform

    # Ou télécharger depuis https://www.terraform.io/downloads

    # Vérifier
    terraform version
    ```

---

## Architecture de Référence Worldline

```
┌─────────────────────────────────────────────────────────────────┐
│              PIPELINE TERRAFORM WORLDLINE                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    DÉVELOPPEUR                          │   │
│   │   1. Modifie les fichiers .tf                           │   │
│   │   2. terraform fmt && terraform validate                │   │
│   │   3. Commit + Push + Pull Request                       │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    CI/CD (GitLab/GitHub)                │   │
│   │                                                         │   │
│   │   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │   │
│   │   │  Validate   │─►│    Plan     │─►│   Review    │    │   │
│   │   │  fmt check  │  │  (output)   │  │  (humain)   │    │   │
│   │   └─────────────┘  └─────────────┘  └─────────────┘    │   │
│   │                                           │             │   │
│   │                                           ▼             │   │
│   │                               ┌─────────────────────┐   │   │
│   │                               │  Apply (protected)  │   │   │
│   │                               │  main branch only   │   │   │
│   │                               └─────────────────────┘   │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    GCP INFRASTRUCTURE                   │   │
│   │                                                         │   │
│   │   ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │   │
│   │   │   VPC   │  │   GKE   │  │Cloud SQL│  │   IAM   │   │   │
│   │   └─────────┘  └─────────┘  └─────────┘  └─────────┘   │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                         │                                       │
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    REMOTE STATE (GCS)                   │   │
│   │   gs://worldline-terraform-state/env/terraform.tfstate  │   │
│   │   + State Locking via GCS                               │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Règles d'Or Terraform

!!! danger "Les 5 Règles Inviolables"

    1. **JAMAIS de `terraform apply` en local sur la production**
       → Toujours passer par la CI/CD

    2. **JAMAIS de secrets dans le code**
       → Utiliser Secret Manager ou variables d'environnement

    3. **TOUJOURS un remote backend**
       → Le state local est une bombe à retardement

    4. **TOUJOURS verrouiller les versions**
       → Providers et modules avec versions explicites

    5. **TOUJOURS un `terraform plan` avant `apply`**
       → Lire et comprendre les changements

---

## Ressources Complémentaires

### Documentation Officielle

- :material-link: [Terraform Documentation](https://developer.hashicorp.com/terraform/docs)
- :material-link: [Google Cloud Provider](https://registry.terraform.io/providers/hashicorp/google/latest/docs)
- :material-link: [Terraform Best Practices](https://developer.hashicorp.com/terraform/cloud-docs/recommended-practices)

### Outils Essentiels

| Outil | Usage | Installation |
|-------|-------|--------------|
| `terraform` | CLI Terraform | `brew install terraform` |
| `tflint` | Linter Terraform | `brew install tflint` |
| `terraform-docs` | Génère la doc automatiquement | `brew install terraform-docs` |
| `tfsec` | Analyse sécurité statique | `brew install tfsec` |
| `infracost` | Estimation des coûts | `brew install infracost` |

---

!!! quote "Philosophie DevOps"
    *"Infrastructure as Code means treating your infrastructure with the same rigor as application code: version control, code review, testing, and automation."*

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Cloud Team
