# Module 2 : Lab OpenSSL - Construire une PKI Interne

## Objectifs du Lab

À l'issue de ce lab, vous serez capable de :

- :material-check: Générer une Autorité de Certification (Root CA)
- :material-check: Créer des requêtes de signature (CSR)
- :material-check: Signer des certificats avec votre CA
- :material-check: Comprendre et manipuler les différents formats de fichiers

!!! info "Scénario du Lab"
    Vous êtes l'administrateur sécurité d'une entreprise. Votre mission est de créer une **PKI interne** pour sécuriser les communications entre vos serveurs internes. Vous allez créer :

    1. Une Root CA auto-signée
    2. Un certificat pour votre serveur web interne `intranet.corp.local`

---

## Préparation de l'Environnement

### Structure de Répertoires

=== "Linux"

    ```bash
    # Créer la structure de répertoires
    mkdir -p ~/pki-lab/{root-ca,intermediate-ca,server-certs}
    mkdir -p ~/pki-lab/root-ca/{certs,private,newcerts,crl}

    # Initialiser les fichiers de suivi
    touch ~/pki-lab/root-ca/index.txt
    echo 1000 > ~/pki-lab/root-ca/serial

    # Définir les permissions restrictives pour les clés privées
    chmod 700 ~/pki-lab/root-ca/private

    # Se positionner dans le répertoire de travail
    cd ~/pki-lab
    ```

=== "Windows (PowerShell)"

    ```powershell
    # Créer la structure de répertoires
    $labPath = "$env:USERPROFILE\pki-lab"
    New-Item -ItemType Directory -Path "$labPath\root-ca\certs" -Force
    New-Item -ItemType Directory -Path "$labPath\root-ca\private" -Force
    New-Item -ItemType Directory -Path "$labPath\root-ca\newcerts" -Force
    New-Item -ItemType Directory -Path "$labPath\server-certs" -Force

    # Initialiser les fichiers de suivi
    New-Item -ItemType File -Path "$labPath\root-ca\index.txt" -Force
    Set-Content -Path "$labPath\root-ca\serial" -Value "1000"

    # Se positionner dans le répertoire de travail
    Set-Location $labPath
    ```

---

## Étape 1 : Générer la Root CA

### 1.1 Générer la Clé Privée de la Root CA

!!! danger "ATTENTION : Clé Critique"
    Cette clé privée est **l'élément le plus sensible** de votre PKI. En production :

    - Générez-la sur une machine air-gapped (déconnectée du réseau)
    - Stockez-la dans un HSM (Hardware Security Module)
    - Chiffrez-la avec une passphrase forte (min. 20 caractères)
    - Faites des sauvegardes sécurisées (coffre-fort physique)

=== "Linux"

    ```bash
    # Générer une clé RSA 4096 bits avec chiffrement AES-256
    openssl genrsa -aes256 -out root-ca/private/root-ca.key 4096

    # Vous serez invité à entrer une passphrase
    # Exemple: "MySecureRootCA_P@ssphrase_2025!"

    # Vérifier les permissions
    chmod 400 root-ca/private/root-ca.key
    ls -la root-ca/private/
    ```

=== "Windows (PowerShell)"

    ```powershell
    # Générer une clé RSA 4096 bits avec chiffrement AES-256
    openssl genrsa -aes256 -out root-ca\private\root-ca.key 4096

    # Vérifier le fichier généré
    Get-Item root-ca\private\root-ca.key
    ```

**Sortie attendue :**

```
Generating RSA private key, 4096 bit long modulus
.....++
.......................................++
e is 65537 (0x10001)
Enter pass phrase for root-ca/private/root-ca.key: ********
Verifying - Enter pass phrase for root-ca/private/root-ca.key: ********
```

### 1.2 Générer le Certificat Auto-Signé de la Root CA

=== "Linux"

    ```bash
    openssl req -x509 -new -nodes \
        -key root-ca/private/root-ca.key \
        -sha256 \
        -days 7300 \
        -out root-ca/certs/root-ca.crt \
        -subj "/C=FR/ST=Ile-de-France/L=Paris/O=MyCorp/OU=Security Team/CN=MyCorp Root CA"
    ```

=== "Windows (PowerShell)"

    ```powershell
    openssl req -x509 -new -nodes `
        -key root-ca\private\root-ca.key `
        -sha256 `
        -days 7300 `
        -out root-ca\certs\root-ca.crt `
        -subj "/C=FR/ST=Ile-de-France/L=Paris/O=MyCorp/OU=Security Team/CN=MyCorp Root CA"
    ```

**Explication des paramètres :**

| Paramètre | Description |
|-----------|-------------|
| `-x509` | Génère un certificat auto-signé (pas une CSR) |
| `-new` | Crée une nouvelle requête |
| `-nodes` | Ne pas chiffrer la sortie (la clé est déjà chiffrée) |
| `-key` | Clé privée à utiliser |
| `-sha256` | Algorithme de hash pour la signature |
| `-days 7300` | Validité : 20 ans (365 × 20) |
| `-subj` | Subject du certificat (DN - Distinguished Name) |

### 1.3 Vérifier le Certificat Root CA

```bash
# Afficher les informations du certificat
openssl x509 -in root-ca/certs/root-ca.crt -text -noout
```

**Vérifiez ces éléments :**

- `Issuer` et `Subject` sont **identiques** (auto-signé)
- `CA:TRUE` dans les extensions
- Validité de 20 ans

---

## Étape 2 : Générer un Certificat Serveur

### 2.1 Créer la Clé Privée du Serveur

=== "Linux"

    ```bash
    # Générer une clé RSA 2048 bits (sans passphrase pour un serveur web)
    openssl genrsa -out server-certs/intranet.corp.local.key 2048

    # Permissions restrictives
    chmod 400 server-certs/intranet.corp.local.key
    ```

=== "Windows (PowerShell)"

    ```powershell
    # Générer une clé RSA 2048 bits
    openssl genrsa -out server-certs\intranet.corp.local.key 2048
    ```

!!! info "Pourquoi pas de passphrase ?"
    Pour les serveurs web (Apache, Nginx), une clé sans passphrase permet un redémarrage automatique du service. En production, utilisez des solutions comme :

    - Variables d'environnement pour la passphrase
    - Vault (HashiCorp) pour la gestion des secrets
    - HSM avec déverrouillage automatisé

### 2.2 Créer le Fichier de Configuration des Extensions

Pour inclure les **SAN (Subject Alternative Names)**, créez un fichier de configuration :

=== "Linux"

    ```bash
    cat > server-certs/intranet.corp.local.cnf << 'EOF'
    [req]
    default_bits       = 2048
    prompt             = no
    default_md         = sha256
    distinguished_name = dn
    req_extensions     = req_ext

    [dn]
    C  = FR
    ST = Ile-de-France
    L  = Paris
    O  = MyCorp
    OU = IT Department
    CN = intranet.corp.local

    [req_ext]
    subjectAltName = @alt_names

    [alt_names]
    DNS.1 = intranet.corp.local
    DNS.2 = intranet
    DNS.3 = www.intranet.corp.local
    IP.1  = 192.168.1.100
    EOF
    ```

=== "Windows (PowerShell)"

    ```powershell
    @"
    [req]
    default_bits       = 2048
    prompt             = no
    default_md         = sha256
    distinguished_name = dn
    req_extensions     = req_ext

    [dn]
    C  = FR
    ST = Ile-de-France
    L  = Paris
    O  = MyCorp
    OU = IT Department
    CN = intranet.corp.local

    [req_ext]
    subjectAltName = @alt_names

    [alt_names]
    DNS.1 = intranet.corp.local
    DNS.2 = intranet
    DNS.3 = www.intranet.corp.local
    IP.1  = 192.168.1.100
    "@ | Out-File -FilePath server-certs\intranet.corp.local.cnf -Encoding ASCII
    ```

### 2.3 Générer la CSR (Certificate Signing Request)

=== "Linux"

    ```bash
    openssl req -new \
        -key server-certs/intranet.corp.local.key \
        -out server-certs/intranet.corp.local.csr \
        -config server-certs/intranet.corp.local.cnf
    ```

=== "Windows (PowerShell)"

    ```powershell
    openssl req -new `
        -key server-certs\intranet.corp.local.key `
        -out server-certs\intranet.corp.local.csr `
        -config server-certs\intranet.corp.local.cnf
    ```

**Vérifier la CSR :**

```bash
openssl req -in server-certs/intranet.corp.local.csr -text -noout
```

Confirmez que les SAN sont présents dans la section `Requested Extensions`.

---

## Étape 3 : Signer le Certificat avec la Root CA

### 3.1 Créer le Fichier d'Extensions pour la Signature

=== "Linux"

    ```bash
    cat > server-certs/server-ext.cnf << 'EOF'
    authorityKeyIdentifier = keyid,issuer
    basicConstraints       = CA:FALSE
    keyUsage               = digitalSignature, keyEncipherment
    extendedKeyUsage       = serverAuth
    subjectAltName         = @alt_names

    [alt_names]
    DNS.1 = intranet.corp.local
    DNS.2 = intranet
    DNS.3 = www.intranet.corp.local
    IP.1  = 192.168.1.100
    EOF
    ```

=== "Windows (PowerShell)"

    ```powershell
    @"
    authorityKeyIdentifier = keyid,issuer
    basicConstraints       = CA:FALSE
    keyUsage               = digitalSignature, keyEncipherment
    extendedKeyUsage       = serverAuth
    subjectAltName         = @alt_names

    [alt_names]
    DNS.1 = intranet.corp.local
    DNS.2 = intranet
    DNS.3 = www.intranet.corp.local
    IP.1  = 192.168.1.100
    "@ | Out-File -FilePath server-certs\server-ext.cnf -Encoding ASCII
    ```

### 3.2 Signer la CSR

=== "Linux"

    ```bash
    openssl x509 -req \
        -in server-certs/intranet.corp.local.csr \
        -CA root-ca/certs/root-ca.crt \
        -CAkey root-ca/private/root-ca.key \
        -CAcreateserial \
        -out server-certs/intranet.corp.local.crt \
        -days 365 \
        -sha256 \
        -extfile server-certs/server-ext.cnf
    ```

=== "Windows (PowerShell)"

    ```powershell
    openssl x509 -req `
        -in server-certs\intranet.corp.local.csr `
        -CA root-ca\certs\root-ca.crt `
        -CAkey root-ca\private\root-ca.key `
        -CAcreateserial `
        -out server-certs\intranet.corp.local.crt `
        -days 365 `
        -sha256 `
        -extfile server-certs\server-ext.cnf
    ```

**Explication des paramètres :**

| Paramètre | Description |
|-----------|-------------|
| `-req` | Indique que l'entrée est une CSR |
| `-CA` | Certificat de la CA signataire |
| `-CAkey` | Clé privée de la CA |
| `-CAcreateserial` | Crée un fichier de numéro de série |
| `-days 365` | Validité : 1 an |
| `-extfile` | Fichier d'extensions à inclure |

### 3.3 Vérifier le Certificat Signé

```bash
# Afficher le certificat
openssl x509 -in server-certs/intranet.corp.local.crt -text -noout

# Vérifier la chaîne
openssl verify -CAfile root-ca/certs/root-ca.crt server-certs/intranet.corp.local.crt
```

**Sortie attendue :**

```
server-certs/intranet.corp.local.crt: OK
```

---

## Étape 4 : Créer un Bundle pour Déploiement

### 4.1 Créer la Chaîne Complète (Full Chain)

```bash
# Concaténer le certificat serveur et la CA (ordre important !)
cat server-certs/intranet.corp.local.crt root-ca/certs/root-ca.crt > server-certs/intranet.corp.local.fullchain.pem
```

### 4.2 Créer un Bundle PKCS#12 (pour Windows/IIS)

=== "Linux"

    ```bash
    openssl pkcs12 -export \
        -out server-certs/intranet.corp.local.pfx \
        -inkey server-certs/intranet.corp.local.key \
        -in server-certs/intranet.corp.local.crt \
        -certfile root-ca/certs/root-ca.crt \
        -name "Intranet Server Certificate"
    ```

=== "Windows (PowerShell)"

    ```powershell
    openssl pkcs12 -export `
        -out server-certs\intranet.corp.local.pfx `
        -inkey server-certs\intranet.corp.local.key `
        -in server-certs\intranet.corp.local.crt `
        -certfile root-ca\certs\root-ca.crt `
        -name "Intranet Server Certificate"
    ```

Vous serez invité à définir un mot de passe d'export.

---

## Référence : Formats de Fichiers

| Extension | Format | Contenu | Usage Typique |
|-----------|--------|---------|---------------|
| `.pem` | ASCII (Base64) | Certificat, clé, ou les deux | Linux/Unix, Apache, Nginx |
| `.crt` | ASCII ou DER | Certificat uniquement (partie publique) | Universel |
| `.cer` | ASCII ou DER | Alias de .crt | Windows préférence |
| `.key` | ASCII (Base64) | Clé privée uniquement | Configuration serveur |
| `.der` | Binaire | Certificat en format binaire | Java, certains appliances |
| `.pfx` / `.p12` | Binaire (PKCS#12) | Bundle : clé privée + certificat + chaîne | Windows/IIS, import/export |
| `.csr` | ASCII (Base64) | Certificate Signing Request | Soumission à une CA |

### Conversions Courantes

```bash
# PEM vers DER
openssl x509 -in cert.pem -outform DER -out cert.der

# DER vers PEM
openssl x509 -in cert.der -inform DER -outform PEM -out cert.pem

# Extraire le certificat d'un PFX
openssl pkcs12 -in bundle.pfx -clcerts -nokeys -out cert.pem

# Extraire la clé privée d'un PFX
openssl pkcs12 -in bundle.pfx -nocerts -out key.pem
```

---

## Récapitulatif des Fichiers Générés

À la fin de ce lab, vous devriez avoir :

```
~/pki-lab/
├── root-ca/
│   ├── certs/
│   │   └── root-ca.crt          # Certificat public de la Root CA
│   ├── private/
│   │   └── root-ca.key          # 🔒 Clé privée de la Root CA (PROTÉGER!)
│   ├── root-ca.srl              # Fichier de numéro de série
│   └── index.txt
└── server-certs/
    ├── intranet.corp.local.key      # 🔒 Clé privée du serveur
    ├── intranet.corp.local.csr      # Requête de signature (archivage)
    ├── intranet.corp.local.crt      # Certificat signé
    ├── intranet.corp.local.cnf      # Configuration OpenSSL
    ├── intranet.corp.local.fullchain.pem  # Chaîne complète
    └── intranet.corp.local.pfx      # Bundle PKCS#12
```

---

## Prochaine Étape

Votre PKI est opérationnelle ! Apprenez maintenant à diagnostiquer les problèmes courants.

[:octicons-arrow-right-24: Module 3 : Debugging & Troubleshooting](03-debugging.md)

---

**Temps estimé :** 90 minutes
**Niveau :** Intermédiaire à Avancé
