# Module 3 : Debugging & Troubleshooting des Certificats

## Objectifs du Module

À l'issue de ce module, vous serez capable de :

- :material-check: Inspecter et analyser un certificat en ligne de commande
- :material-check: Valider une chaîne de certification complète
- :material-check: Diagnostiquer les erreurs TLS les plus courantes
- :material-check: Utiliser `openssl s_client` pour tester les connexions sécurisées

---

## 1. Inspecter un Certificat

### 1.1 Lire un Certificat en Texte Clair

La commande fondamentale pour analyser un certificat :

=== "Linux"

    ```bash
    # Afficher toutes les informations du certificat
    openssl x509 -in certificate.crt -text -noout
    ```

=== "Windows (PowerShell)"

    ```powershell
    openssl x509 -in certificate.crt -text -noout
    ```

**Explication des paramètres :**

| Paramètre | Description |
|-----------|-------------|
| `-in` | Fichier certificat en entrée |
| `-text` | Affiche le contenu en texte lisible |
| `-noout` | N'affiche pas le certificat encodé en Base64 |

### 1.2 Informations Spécifiques

```bash
# Afficher uniquement le Subject
openssl x509 -in certificate.crt -subject -noout

# Afficher uniquement l'Issuer (émetteur)
openssl x509 -in certificate.crt -issuer -noout

# Afficher les dates de validité
openssl x509 -in certificate.crt -dates -noout

# Afficher le numéro de série
openssl x509 -in certificate.crt -serial -noout

# Afficher le fingerprint SHA256
openssl x509 -in certificate.crt -fingerprint -sha256 -noout

# Afficher les Subject Alternative Names (SAN)
openssl x509 -in certificate.crt -text -noout | grep -A1 "Subject Alternative Name"
```

### 1.3 Vérifier l'Expiration

```bash
# Vérifier si le certificat expire dans les 30 prochains jours
openssl x509 -in certificate.crt -checkend 2592000 -noout

# Retourne 0 si valide, 1 si expiration imminente
echo $?
```

!!! tip "Automatisation"
    Intégrez cette commande dans vos scripts de monitoring pour alerter avant l'expiration :

    ```bash
    #!/bin/bash
    CERT="/path/to/certificate.crt"
    DAYS=30
    SECONDS=$((DAYS * 86400))

    if ! openssl x509 -in "$CERT" -checkend "$SECONDS" -noout; then
        echo "ALERTE: Le certificat expire dans moins de $DAYS jours!"
        # Envoyer une notification (mail, Slack, etc.)
    fi
    ```

---

## 2. Valider une Chaîne de Certification

### 2.1 Vérification Basique

```bash
# Vérifier un certificat contre une CA
openssl verify -CAfile root-ca.crt server-certificate.crt
```

**Résultats possibles :**

| Sortie | Signification |
|--------|---------------|
| `certificate.crt: OK` | Chaîne valide |
| `unable to get local issuer certificate` | CA intermédiaire manquante |
| `certificate signature failure` | Signature invalide |
| `certificate has expired` | Certificat expiré |

### 2.2 Vérification avec Chaîne Complète

Si vous avez une CA intermédiaire :

```bash
# Créer un bundle de CA (root + intermediate)
cat intermediate-ca.crt root-ca.crt > ca-chain.crt

# Vérifier avec la chaîne complète
openssl verify -CAfile ca-chain.crt server-certificate.crt
```

### 2.3 Vérification avec Affichage de la Chaîne

```bash
# Afficher la chaîne de vérification complète
openssl verify -CAfile ca-chain.crt -show_chain server-certificate.crt
```

**Sortie attendue :**

```
server-certificate.crt: OK
Chain:
depth=0: CN = www.example.com (untrusted)
depth=1: CN = Example Intermediate CA
depth=2: CN = Example Root CA
```

---

## 3. Test de Connexion avec `openssl s_client`

### 3.1 Connexion Basique

L'outil `s_client` permet de tester une connexion TLS comme le ferait un navigateur :

=== "Linux"

    ```bash
    # Test de connexion à un serveur HTTPS
    openssl s_client -connect google.com:443
    ```

=== "Windows (PowerShell)"

    ```powershell
    # Test de connexion à un serveur HTTPS
    openssl s_client -connect google.com:443
    ```

!!! info "Interprétation de la sortie"
    La sortie contient plusieurs sections importantes :

    - **Certificate chain** : La chaîne de certificats présentée par le serveur
    - **Server certificate** : Le certificat du serveur (PEM)
    - **SSL handshake** : Détails de la négociation TLS
    - **Verify return code** : Code de validation (0 = OK)

### 3.2 Options Avancées

```bash
# Afficher la chaîne complète avec détails
openssl s_client -connect example.com:443 -showcerts

# Forcer une version TLS spécifique
openssl s_client -connect example.com:443 -tls1_3

# Spécifier un SNI (Server Name Indication)
openssl s_client -connect example.com:443 -servername www.example.com

# Utiliser un CA bundle personnalisé
openssl s_client -connect example.com:443 -CAfile /path/to/ca-bundle.crt

# Timeout de connexion (en secondes)
timeout 5 openssl s_client -connect example.com:443

# Vérifier le certificat et quitter immédiatement
echo | openssl s_client -connect example.com:443 2>/dev/null | openssl x509 -text -noout
```

### 3.3 Analyser le Résultat

**Exemple de sortie :**

```
CONNECTED(00000003)
depth=2 C = US, O = DigiCert Inc, CN = DigiCert Global Root CA
verify return:1
depth=1 C = US, O = DigiCert Inc, CN = DigiCert TLS RSA SHA256 2020 CA1
verify return:1
depth=0 C = US, ST = California, L = Mountain View, O = Google LLC, CN = *.google.com
verify return:1
---
Certificate chain
 0 s:C = US, ST = California, L = Mountain View, O = Google LLC, CN = *.google.com
   i:C = US, O = DigiCert Inc, CN = DigiCert TLS RSA SHA256 2020 CA1
 1 s:C = US, O = DigiCert Inc, CN = DigiCert TLS RSA SHA256 2020 CA1
   i:C = US, O = DigiCert Inc, CN = DigiCert Global Root CA
---
...
SSL-Session:
    Protocol  : TLSv1.3
    Cipher    : TLS_AES_256_GCM_SHA384
...
    Verify return code: 0 (ok)
```

**Points à vérifier :**

| Élément | Attendu | Problème si... |
|---------|---------|----------------|
| `Verify return code` | `0 (ok)` | Tout autre code indique un problème |
| `Protocol` | `TLSv1.2` ou `TLSv1.3` | Versions antérieures = obsolètes |
| `Certificate chain` | Complète | Chaîne incomplète = erreurs navigateur |

---

## 4. Erreurs Courantes et Solutions

### 4.1 Tableau de Diagnostic

| Code d'erreur | Message | Cause probable | Solution |
|---------------|---------|----------------|----------|
| `2` | `unable to get issuer certificate` | Chaîne incomplète | Ajouter les certificats intermédiaires |
| `10` | `certificate has expired` | Certificat périmé | Renouveler le certificat |
| `18` | `self-signed certificate` | CA non reconnue | Ajouter la Root CA au trust store |
| `19` | `self-signed certificate in chain` | CA intermédiaire auto-signée | Vérifier la chaîne de confiance |
| `20` | `unable to get local issuer certificate` | Root CA manquante | Installer la Root CA |
| `21` | `unable to verify first certificate` | Pas de chaîne présentée | Configurer le serveur avec fullchain |
| `26` | `unsupported certificate purpose` | Usage incorrect | Vérifier Key Usage / Extended Key Usage |

### 4.2 Problèmes de Chaîne Incomplète

!!! danger "Erreur la Plus Courante"
    L'erreur `unable to verify the first certificate` est la plus fréquente. Elle survient quand le serveur ne présente pas la chaîne intermédiaire.

**Diagnostic :**

```bash
# Vérifier ce que le serveur envoie
openssl s_client -connect problematic-server.com:443 -showcerts 2>/dev/null | \
    grep -E "^(Certificate chain| [0-9]+ s:|   i:)"
```

**Solution (Apache) :**

```apache
SSLCertificateFile /etc/ssl/certs/server.crt
SSLCertificateKeyFile /etc/ssl/private/server.key
SSLCertificateChainFile /etc/ssl/certs/intermediate.crt
```

**Solution (Nginx) :**

```nginx
ssl_certificate /etc/ssl/certs/server-fullchain.pem;  # cert + intermediates
ssl_certificate_key /etc/ssl/private/server.key;
```

### 4.3 Problèmes de SAN/CN

!!! warning "NET::ERR_CERT_COMMON_NAME_INVALID"
    Cette erreur Chrome/Firefox indique que le domaine demandé ne correspond pas au certificat.

**Diagnostic :**

```bash
# Extraire les SAN du certificat
echo | openssl s_client -connect server.com:443 2>/dev/null | \
    openssl x509 -noout -text | grep -A1 "Subject Alternative Name"
```

**Vérifiez que :**

- Le domaine exact est listé dans les SAN
- Les wildcards sont correctement utilisés (`*.example.com` couvre `www.example.com` mais PAS `example.com`)

### 4.4 Problèmes de Date/Heure

```bash
# Vérifier les dates du certificat
openssl x509 -in certificate.crt -noout -dates

# Vérifier l'heure système
date
timedatectl status  # Linux avec systemd
```

!!! info "Synchronisation NTP"
    Une différence de quelques minutes entre le client et le serveur peut causer des erreurs de validation. Assurez-vous que NTP est configuré sur tous vos systèmes.

---

## 5. Outils de Diagnostic Avancés

### 5.1 SSL Labs (En ligne)

Pour un diagnostic complet d'un serveur public :

:material-link: [https://www.ssllabs.com/ssltest/](https://www.ssllabs.com/ssltest/)

### 5.2 testssl.sh (Linux)

Script complet pour tester la configuration TLS :

```bash
# Installation
git clone https://github.com/drwetter/testssl.sh.git
cd testssl.sh

# Exécution
./testssl.sh https://example.com
```

### 5.3 crt.sh (Certificate Transparency)

Rechercher tous les certificats émis pour un domaine :

:material-link: [https://crt.sh/?q=example.com](https://crt.sh/?q=example.com)

---

## 6. Script de Diagnostic Automatisé

Voici un script utilitaire pour diagnostiquer rapidement un certificat :

=== "Linux (Bash)"

    ```bash
    #!/bin/bash
    # cert-check.sh - Diagnostic rapide d'un certificat

    if [ -z "$1" ]; then
        echo "Usage: $0 <host:port> OR $0 <certificate.crt>"
        exit 1
    fi

    echo "========================================"
    echo "  Diagnostic de Certificat"
    echo "========================================"

    if [[ "$1" == *":"* ]]; then
        # Connexion réseau
        HOST="$1"
        echo -e "\n[*] Connexion à $HOST..."

        CERT=$(echo | openssl s_client -connect "$HOST" -servername "${HOST%%:*}" 2>/dev/null)

        echo -e "\n[*] Chaîne de certificats:"
        echo "$CERT" | grep -E "^(Certificate chain| [0-9]+ s:|   i:)"

        echo -e "\n[*] Protocole et Cipher:"
        echo "$CERT" | grep -E "(Protocol|Cipher)"

        echo -e "\n[*] Code de vérification:"
        echo "$CERT" | grep "Verify return code"

        echo -e "\n[*] Dates de validité:"
        echo "$CERT" | openssl x509 -noout -dates 2>/dev/null

        echo -e "\n[*] Subject Alternative Names:"
        echo "$CERT" | openssl x509 -noout -text 2>/dev/null | \
            grep -A1 "Subject Alternative Name" | tail -1
    else
        # Fichier local
        FILE="$1"
        echo -e "\n[*] Analyse de $FILE..."

        echo -e "\n[*] Subject:"
        openssl x509 -in "$FILE" -subject -noout

        echo -e "\n[*] Issuer:"
        openssl x509 -in "$FILE" -issuer -noout

        echo -e "\n[*] Dates de validité:"
        openssl x509 -in "$FILE" -dates -noout

        echo -e "\n[*] Fingerprint SHA256:"
        openssl x509 -in "$FILE" -fingerprint -sha256 -noout

        echo -e "\n[*] Subject Alternative Names:"
        openssl x509 -in "$FILE" -text -noout | \
            grep -A1 "Subject Alternative Name" | tail -1
    fi

    echo -e "\n========================================"
    ```

=== "Windows (PowerShell)"

    ```powershell
    # cert-check.ps1 - Diagnostic rapide d'un certificat
    param(
        [Parameter(Mandatory=$true)]
        [string]$Target
    )

    Write-Host "========================================"
    Write-Host "  Diagnostic de Certificat"
    Write-Host "========================================"

    if ($Target -match ":") {
        # Connexion réseau
        Write-Host "`n[*] Connexion à $Target..."
        $result = echo "" | openssl s_client -connect $Target 2>$null

        Write-Host "`n[*] Protocole et Cipher:"
        $result | Select-String -Pattern "Protocol|Cipher"

        Write-Host "`n[*] Code de vérification:"
        $result | Select-String -Pattern "Verify return code"
    }
    else {
        # Fichier local
        Write-Host "`n[*] Analyse de $Target..."

        Write-Host "`n[*] Subject:"
        openssl x509 -in $Target -subject -noout

        Write-Host "`n[*] Issuer:"
        openssl x509 -in $Target -issuer -noout

        Write-Host "`n[*] Dates de validité:"
        openssl x509 -in $Target -dates -noout
    }

    Write-Host "`n========================================"
    ```

**Usage :**

```bash
# Test d'un serveur distant
./cert-check.sh google.com:443

# Analyse d'un fichier local
./cert-check.sh /path/to/certificate.crt
```

---

## Ressources Complémentaires

!!! success "Cheat Sheet"
    Pour une référence rapide de toutes les commandes OpenSSL, consultez notre aide-mémoire :

    [:material-file-document: Cheat Sheet Certificats](../../security/certificates.md)

---

## Quiz Final

??? question "Question 1 : Quelle commande permet de vérifier qu'un certificat n'expire pas dans les 7 jours ?"
    **Réponse :**
    ```bash
    openssl x509 -in cert.crt -checkend 604800 -noout
    # 604800 = 7 * 24 * 60 * 60 secondes
    ```

??? question "Question 2 : Comment diagnostiquer une erreur 'unable to verify the first certificate' ?"
    **Réponse :** Cette erreur indique que le serveur ne présente pas la chaîne intermédiaire. Vérifiez avec :
    ```bash
    openssl s_client -connect server:443 -showcerts
    ```
    Puis configurez le serveur pour envoyer le fullchain (certificat + intermédiaires).

??? question "Question 3 : Comment extraire le certificat d'un serveur distant et le sauvegarder ?"
    **Réponse :**
    ```bash
    echo | openssl s_client -connect server.com:443 2>/dev/null | \
        sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > server.crt
    ```

---

## Félicitations !

Vous avez terminé la formation **PKI & Gestion des Certificats**. Vous maîtrisez maintenant :

- :material-check-circle: Les fondamentaux de la cryptographie asymétrique
- :material-check-circle: La construction d'une PKI interne avec OpenSSL
- :material-check-circle: Le diagnostic et la résolution des problèmes TLS

[:octicons-arrow-left-24: Retour à l'index de la formation](index.md)

---

**Temps estimé :** 60 minutes
**Niveau :** Intermédiaire à Avancé
