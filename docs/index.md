# ShellBook

Bienvenue dans **ShellBook**, la base de connaissances Docs-as-Code pour les équipes SysOps/DevOps.

## Parcours de Formation

Les formations sont organisées par niveau de complexité et zone d'accès infrastructure :

| Niveau | Focus | Public cible |
|--------|-------|--------------|
| **Niveau 0** | Accès PADSEC, VPN, Active Directory | Nouveaux arrivants, tous profils |
| **Niveau 1** | Sécurité, PKI, Certificats | Ops confirmés, Security Engineers |
| **Niveau 2** | Linux, Windows, Scripting avancé | SysAdmins, Platform Engineers |
| **Niveau 3** | Cloud (GCP/GKE), IaC, GitOps | DevOps, SRE, Cloud Architects |

## Démarrage Rapide

<div class="grid cards" markdown>

-   :material-shield-lock:{ .lg .middle } **Niveau 0 - Fondations**

    ---

    Commencez par comprendre l'architecture PADSEC et les accès VPN.

    [:octicons-arrow-right-24: Architecture PADSEC](formations/ntlite/index.md)

-   :material-certificate:{ .lg .middle } **Niveau 1 - Sécurité**

    ---

    Maîtrisez la PKI et la gestion des certificats.

    [:octicons-arrow-right-24: Infrastructure PKI](formations/pki-security/index.md)

-   :material-linux:{ .lg .middle } **Niveau 2 - Plateformes**

    ---

    Approfondissez Linux, Windows et le scripting.

    [:octicons-arrow-right-24: Linux Performance](formations/linux-perf/index.md)

-   :material-cloud:{ .lg .middle } **Niveau 3 - Cloud & DevOps**

    ---

    Déployez sur GKE avec Terraform et ArgoCD.

    [:octicons-arrow-right-24: GCP & GKE](formations/gcp-gke/index.md)

</div>

## Architecture Documentaire

```
docs/
├── formations/           # Modules de formation par technologie
│   ├── ad-hardening/     # Hardening Active Directory
│   ├── gcp-gke/          # Google Kubernetes Engine
│   ├── gitops-argocd/    # GitOps avec ArgoCD
│   ├── linux/            # Services Linux (Edge, etc.)
│   ├── linux-perf/       # Performance Linux
│   ├── pki-security/     # Infrastructure PKI
│   ├── powershell-advanced/  # PowerShell avancé
│   ├── terraform-ops/    # Terraform industrialisé
│   └── windows/          # Active Directory, WSUS, etc.
├── network/              # Documentation réseau (VPN, etc.)
├── security/             # Guides sécurité transverses
└── developer/            # Documentation technique interne
```

## Conventions

!!! info "Anonymisation"
    Tous les exemples utilisent des noms de domaine et adresses IP fictifs :

    - Domaine : `*.shellbook.local`
    - Plages IP : `192.168.x.x`, `10.x.x.x`
    - Serveurs : `srv-*`, `fw-*`, `dc-*`

!!! warning "Classification"
    Cette documentation est destinée à un usage interne uniquement.
    Ne partagez pas les informations d'architecture en dehors de l'organisation.

## Contribution

ShellBook suit le paradigme **Docs-as-Code** :

1. Fork le repository
2. Créez une branche feature
3. Rédigez en Markdown avec les extensions MkDocs Material
4. Soumettez une Pull Request

```bash
# Preview local
mkdocs serve

# Build statique
mkdocs build
```
