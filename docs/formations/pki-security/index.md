# Formation PKI & Gestion des Certificats

## Introduction

Dans un environnement **SecNumCloud** et plus largement dans toute infrastructure moderne, la **PKI** (Public Key Infrastructure) constitue le socle fondamental de la sécurité des communications. Cette formation vous donnera les compétences nécessaires pour comprendre, déployer et dépanner une infrastructure à clé publique.

### Pourquoi la PKI est-elle essentielle ?

La PKI répond à trois besoins fondamentaux de la sécurité informatique :

| Besoin | Description | Mécanisme PKI |
|--------|-------------|---------------|
| **Identité** | Garantir l'authenticité d'un serveur ou d'un utilisateur | Certificats X.509 signés par une CA de confiance |
| **Chiffrement** | Protéger la confidentialité des données en transit | Échange de clés via TLS/SSL |
| **Intégrité** | Assurer que les données n'ont pas été altérées | Signatures numériques et hashs cryptographiques |

!!! info "Contexte SecNumCloud"
    Dans le cadre de la qualification SecNumCloud de l'ANSSI, la maîtrise de la PKI est une **exigence réglementaire**. Les opérateurs doivent démontrer leur capacité à gérer le cycle de vie complet des certificats.

---

## Syllabus de la Formation

Cette formation est organisée en **4 modules progressifs** :

### Module 1 : Concepts Fondamentaux
:material-book-open-variant: **Théorie** | :material-clock-outline: ~45 min

- Cryptographie asymétrique (clé publique/privée)
- Chaîne de confiance (Root CA → Intermediate → End-Entity)
- Anatomie d'un certificat X.509
- Handshake TLS 1.3 détaillé

[:octicons-arrow-right-24: Accéder au Module 1](01-concepts.md)

---

### Module 2 : Lab OpenSSL - Construire une PKI
:material-flask: **Pratique** | :material-clock-outline: ~90 min

- Générer une Autorité de Certification (Root CA)
- Créer et signer des certificats serveur
- Comprendre les formats de fichiers (.pem, .der, .pfx)
- Déployer un certificat sur un serveur web

[:octicons-arrow-right-24: Accéder au Module 2](02-openssl-lab.md)

---

### Module 3 : Debugging & Troubleshooting
:material-bug: **Dépannage** | :material-clock-outline: ~60 min

- Inspecter un certificat en ligne de commande
- Valider une chaîne de certification
- Diagnostiquer les erreurs TLS courantes
- Outils de test réseau (`openssl s_client`)

[:octicons-arrow-right-24: Accéder au Module 3](03-debugging.md)

---

### Module 4 : Automatisation avec ACME & Certbot
:material-robot: **Automatisation** | :material-clock-outline: ~75 min

- Protocole ACME et challenges (HTTP-01, DNS-01)
- Obtention et renouvellement automatique avec Certbot
- Hooks de déploiement (Nginx, Apache, HAProxy)
- Intégration enterprise (Smallstep, Vault)

[:octicons-arrow-right-24: Accéder au Module 4](04-automation.md)

---

## Prérequis

!!! warning "Compétences requises"
    Avant de commencer cette formation, assurez-vous de maîtriser :

    - **Linux CLI** : Navigation, édition de fichiers, pipes et redirections
    - **Réseau TCP/IP** : Notions de ports, DNS, connexions client/serveur
    - **Éditeur de texte** : vim, nano ou équivalent

### Environnement de Lab

Pour les exercices pratiques, vous aurez besoin de :

=== "Linux (Recommandé)"

    ```bash
    # Vérifier la version d'OpenSSL
    openssl version

    # Version minimale recommandée : 1.1.1+
    # Pour TLS 1.3 : OpenSSL 1.1.1 ou supérieur
    ```

=== "Windows (PowerShell)"

    ```powershell
    # Option 1 : OpenSSL via Chocolatey
    choco install openssl

    # Option 2 : Utiliser WSL2 avec une distribution Linux
    wsl --install -d Ubuntu
    ```

---

## Ressources Complémentaires

- :material-file-document: [Cheat Sheet Certificats](../../security/certificates.md) — Commandes rapides
- :material-link: [RFC 5280 - X.509 PKI](https://datatracker.ietf.org/doc/html/rfc5280) — Spécification officielle
- :material-link: [Mozilla SSL Configuration Generator](https://ssl-config.mozilla.org/) — Configurations TLS recommandées

---

**Dernière mise à jour :** 2025-01-28
**Version :** 1.0
**Auteur :** ShellBook Security Team
