# Formation : Durcissement Active Directory

## Introduction

**Active Directory est la cible n°1 des attaquants.**

Dans 90% des attaques par ransomware, la compromission d'Active Directory est l'étape clé qui permet aux attaquants de prendre le contrôle total du système d'information. Une fois l'AD compromis, c'est **game over** : déploiement de ransomware sur tous les postes, exfiltration de données, destruction des sauvegardes.

```
┌─────────────────────────────────────────────────────────────────┐
│                  ANATOMIE D'UNE ATTAQUE AD                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   [Phishing]  ──►  [Poste Utilisateur]  ──►  [Mouvement        │
│                           │                   Latéral]          │
│                           │                      │              │
│                           ▼                      ▼              │
│                    [Élévation de        [Compromission AD]      │
│                     Privilèges]                │                │
│                                                ▼                │
│                                    ┌─────────────────────┐      │
│                                    │   DOMAIN ADMIN      │      │
│                                    │   = GAME OVER       │      │
│                                    └─────────────────────┘      │
│                                                │                │
│                                                ▼                │
│                              [Ransomware / Exfiltration]        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

!!! danger "Statistique Alarmante"
    Selon Microsoft, le temps moyen pour qu'un attaquant obtienne des privilèges Domain Admin après une compromission initiale est de **moins de 48 heures**. Dans certains cas, ce délai est réduit à quelques heures.

### Pourquoi cette formation ?

Cette formation vous donnera les connaissances et les outils pour :

- :material-shield-check: **Comprendre** l'architecture de sécurité recommandée (Tiering Model)
- :material-shield-check: **Identifier** les vecteurs d'attaque les plus courants
- :material-shield-check: **Implémenter** des mesures de durcissement concrètes
- :material-shield-check: **Auditer** votre environnement AD existant

---

## Syllabus de la Formation

Cette formation est organisée en **3 modules** :

### Module 1 : Le Modèle de Tiering
:material-layers-triple: **Architecture** | :material-clock-outline: ~60 min

- Le modèle d'administration en couches (Microsoft Enterprise Access Model)
- Séparation Tier 0 / Tier 1 / Tier 2
- La règle d'or : isolation des credentials
- Implémentation des PAW (Privileged Access Workstations)

[:octicons-arrow-right-24: Accéder au Module 1](01-tiering-model.md)

---

### Module 2 : Vecteurs d'Attaque AD
:material-skull-crossbones: **Offensive Security** | :material-clock-outline: ~75 min

- ASREPRoast : exploitation de la pré-authentification Kerberos
- Kerberoasting : attaque des comptes de service
- Pass-the-Hash et Pass-the-Ticket
- Détection et remédiation

[:octicons-arrow-right-24: Accéder au Module 2](02-attack-vectors.md)

---

### Module 3 : Audit et Politiques de Mots de Passe
:material-shield-lock: **Défense** | :material-clock-outline: ~60 min

- Audit avec PingCastle
- Fine-Grained Password Policies (PSO/FGPP)
- Group Managed Service Accounts (gMSA)
- Stratégie de durcissement progressive

[:octicons-arrow-right-24: Accéder au Module 3](03-audit-and-pso.md)

---

## Prérequis

!!! warning "Connaissances requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **Active Directory** : Concepts de base (Forêt, Domaine, OU)
    - **Utilisateurs et Groupes** : Création, gestion, groupes de sécurité
    - **GPO** : Création et liaison de stratégies de groupe
    - **PowerShell** : Commandes de base, module ActiveDirectory
    - **Kerberos** : Notions de base (TGT, TGS, SPN)

### Environnement de Lab

Pour les exercices pratiques, vous aurez besoin de :

=== "Lab Recommandé"

    ```
    ┌─────────────────────────────────────────────────┐
    │              ENVIRONNEMENT DE LAB               │
    ├─────────────────────────────────────────────────┤
    │                                                 │
    │   [DC01]  - Windows Server 2019/2022           │
    │            - Contrôleur de domaine             │
    │            - DNS intégré                       │
    │            - 4 GB RAM minimum                  │
    │                                                 │
    │   [SRV01] - Windows Server 2019/2022           │
    │            - Serveur membre                    │
    │            - Simulation Tier 1                 │
    │            - 2 GB RAM minimum                  │
    │                                                 │
    │   [PC01]  - Windows 10/11 Pro                  │
    │            - Poste joint au domaine            │
    │            - Simulation Tier 2                 │
    │            - 4 GB RAM minimum                  │
    │                                                 │
    └─────────────────────────────────────────────────┘
    ```

=== "PowerShell - Vérification des prérequis"

    ```powershell
    # Vérifier le module ActiveDirectory
    Get-Module -ListAvailable -Name ActiveDirectory

    # Si absent, installer les RSAT
    Add-WindowsCapability -Online -Name Rsat.ActiveDirectory.DS-LDS.Tools~~~~0.0.1.0

    # Vérifier la connectivité au domaine
    Test-ComputerSecureChannel -Verbose

    # Vérifier le niveau fonctionnel du domaine
    (Get-ADDomain).DomainMode
    ```

---

## Ressources et Références

### Standards et Bonnes Pratiques

- :material-file-document: [ANSSI - Recommandations de sécurité relatives à Active Directory](https://www.ssi.gouv.fr/guide/recommandations-de-securite-relatives-a-active-directory/)
- :material-file-document: [Microsoft - Enterprise Access Model](https://docs.microsoft.com/en-us/security/compass/privileged-access-access-model)
- :material-file-document: [Microsoft - Securing Privileged Access](https://docs.microsoft.com/en-us/security/compass/overview)

### Outils d'Audit

- :material-tools: [PingCastle](https://www.pingcastle.com/) — Audit automatisé d'Active Directory
- :material-tools: [BloodHound](https://github.com/BloodHoundAD/BloodHound) — Analyse des chemins d'attaque
- :material-tools: [Purple Knight](https://www.purple-knight.com/) — Évaluation de la sécurité AD

### Ressources Complémentaires

- :material-book: [IT-Connect - Sécurité Active Directory](https://www.it-connect.fr/cours/active-directory/)
- :material-book: [HackTricks - Active Directory Methodology](https://book.hacktricks.xyz/windows-hardening/active-directory-methodology)

---

## Avertissement

!!! danger "Usage Éthique Uniquement"
    Les techniques offensives présentées dans cette formation sont à des fins **éducatives et défensives uniquement**.

    Toute utilisation de ces connaissances sur des systèmes sans autorisation explicite est **illégale** et passible de poursuites pénales.

    Dans un contexte professionnel (Worldline/SecNumCloud), assurez-vous d'avoir les autorisations nécessaires avant tout test de sécurité.

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Security Team
