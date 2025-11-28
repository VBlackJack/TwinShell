# Cheat Sheet : Commandes Certificats & OpenSSL

> Référence rapide des commandes les plus utilisées pour la gestion des certificats SSL/TLS.

---

## Génération de Clés

```bash
# Clé RSA 2048 bits (sans passphrase)
openssl genrsa -out server.key 2048

# Clé RSA 4096 bits avec chiffrement AES-256
openssl genrsa -aes256 -out ca.key 4096

# Clé ECDSA (courbe P-256)
openssl ecparam -genkey -name prime256v1 -out server-ec.key

# Clé ECDSA (courbe P-384, recommandé ANSSI)
openssl ecparam -genkey -name secp384r1 -out server-ec.key
```

---

## Génération de CSR (Certificate Signing Request)

```bash
# CSR simple (interactive)
openssl req -new -key server.key -out server.csr

# CSR avec subject en ligne de commande
openssl req -new -key server.key -out server.csr \
    -subj "/C=FR/ST=IDF/L=Paris/O=MyCorp/CN=www.example.com"

# CSR avec fichier de configuration (pour SAN)
openssl req -new -key server.key -out server.csr -config san.cnf
```

---

## Certificats Auto-Signés

```bash
# Certificat auto-signé valide 365 jours
openssl req -x509 -new -nodes -key ca.key -sha256 -days 365 \
    -out ca.crt -subj "/CN=My Root CA"

# Certificat auto-signé avec SAN
openssl req -x509 -new -nodes -key server.key -sha256 -days 365 \
    -out server.crt -config san.cnf -extensions req_ext
```

---

## Signature de Certificats

```bash
# Signer une CSR avec une CA
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key \
    -CAcreateserial -out server.crt -days 365 -sha256

# Signer avec extensions (SAN, Key Usage)
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key \
    -CAcreateserial -out server.crt -days 365 -sha256 \
    -extfile extensions.cnf
```

---

## Inspection de Certificats

```bash
# Afficher le contenu complet
openssl x509 -in cert.crt -text -noout

# Afficher uniquement le Subject
openssl x509 -in cert.crt -subject -noout

# Afficher uniquement l'Issuer
openssl x509 -in cert.crt -issuer -noout

# Afficher les dates de validité
openssl x509 -in cert.crt -dates -noout

# Afficher le fingerprint SHA256
openssl x509 -in cert.crt -fingerprint -sha256 -noout

# Afficher les SAN (Subject Alternative Names)
openssl x509 -in cert.crt -text -noout | grep -A1 "Subject Alternative Name"

# Vérifier si expire dans N secondes (2592000 = 30 jours)
openssl x509 -in cert.crt -checkend 2592000 -noout
```

---

## Inspection de CSR

```bash
# Afficher le contenu d'une CSR
openssl req -in server.csr -text -noout

# Vérifier la signature de la CSR
openssl req -in server.csr -verify -noout
```

---

## Inspection de Clés

```bash
# Vérifier une clé RSA
openssl rsa -in server.key -check -noout

# Afficher les détails d'une clé RSA
openssl rsa -in server.key -text -noout

# Extraire la clé publique
openssl rsa -in server.key -pubout -out server.pub

# Vérifier correspondance clé/certificat (les modulus doivent être identiques)
openssl x509 -in cert.crt -noout -modulus | openssl md5
openssl rsa -in server.key -noout -modulus | openssl md5
```

---

## Validation de Chaîne

```bash
# Vérifier un certificat contre une CA
openssl verify -CAfile ca.crt server.crt

# Vérifier avec chaîne complète (root + intermediate)
openssl verify -CAfile ca-chain.crt server.crt

# Afficher la chaîne de vérification
openssl verify -CAfile ca-chain.crt -show_chain server.crt
```

---

## Conversions de Format

### PEM ↔ DER

```bash
# PEM vers DER
openssl x509 -in cert.pem -outform DER -out cert.der

# DER vers PEM
openssl x509 -in cert.der -inform DER -outform PEM -out cert.pem
```

### PEM ↔ PKCS#12 (PFX)

```bash
# Créer un PFX (clé + cert + CA)
openssl pkcs12 -export -out bundle.pfx \
    -inkey server.key -in server.crt -certfile ca.crt

# Extraire le certificat d'un PFX
openssl pkcs12 -in bundle.pfx -clcerts -nokeys -out cert.pem

# Extraire la clé privée d'un PFX
openssl pkcs12 -in bundle.pfx -nocerts -nodes -out key.pem

# Extraire les CA d'un PFX
openssl pkcs12 -in bundle.pfx -cacerts -nokeys -out ca.pem
```

### PKCS#7 (P7B)

```bash
# PEM vers PKCS#7
openssl crl2pkcs7 -nocrl -certfile cert.pem -out cert.p7b

# PKCS#7 vers PEM
openssl pkcs7 -in cert.p7b -print_certs -out cert.pem
```

---

## Test de Connexion TLS

```bash
# Connexion basique
openssl s_client -connect example.com:443

# Avec SNI (Server Name Indication)
openssl s_client -connect example.com:443 -servername www.example.com

# Afficher la chaîne complète
openssl s_client -connect example.com:443 -showcerts

# Forcer TLS 1.3
openssl s_client -connect example.com:443 -tls1_3

# Forcer TLS 1.2
openssl s_client -connect example.com:443 -tls1_2

# Avec CA bundle personnalisé
openssl s_client -connect example.com:443 -CAfile ca-bundle.crt

# Extraire le certificat d'un serveur
echo | openssl s_client -connect example.com:443 2>/dev/null | \
    sed -ne '/-BEGIN CERTIFICATE-/,/-END CERTIFICATE-/p' > server.crt

# Test STARTTLS (SMTP)
openssl s_client -connect mail.example.com:25 -starttls smtp

# Test STARTTLS (IMAP)
openssl s_client -connect mail.example.com:143 -starttls imap
```

---

## Gestion des Passphrases

```bash
# Retirer la passphrase d'une clé
openssl rsa -in encrypted.key -out decrypted.key

# Ajouter une passphrase à une clé
openssl rsa -in decrypted.key -aes256 -out encrypted.key

# Changer la passphrase
openssl rsa -in oldpass.key -aes256 -out newpass.key
```

---

## Génération de Hashs

```bash
# Hash SHA256 d'un fichier
openssl dgst -sha256 file.txt

# Hash SHA256 avec signature
openssl dgst -sha256 -sign private.key -out signature.bin file.txt

# Vérifier une signature
openssl dgst -sha256 -verify public.key -signature signature.bin file.txt
```

---

## Configuration OpenSSL pour SAN

Fichier `san.cnf` exemple :

```ini
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
CN = www.example.com

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = www.example.com
DNS.2 = example.com
DNS.3 = api.example.com
IP.1  = 192.168.1.100
```

---

## Tableau des Formats de Fichiers

| Extension | Format | Contenu | Usage |
|-----------|--------|---------|-------|
| `.pem` | ASCII Base64 | Clé, Cert, ou les deux | Linux, Apache, Nginx |
| `.crt` | ASCII/DER | Certificat | Universel |
| `.cer` | ASCII/DER | Certificat | Windows |
| `.key` | ASCII Base64 | Clé privée | Configuration serveur |
| `.der` | Binaire | Certificat | Java |
| `.pfx` `.p12` | Binaire PKCS#12 | Clé + Cert + Chaîne | Windows/IIS |
| `.csr` | ASCII Base64 | Requête de signature | Soumission CA |
| `.p7b` | ASCII/Binaire PKCS#7 | Chaîne de certificats | Windows |

---

## Codes d'Erreur Courants

| Code | Message | Cause |
|------|---------|-------|
| `2` | `unable to get issuer certificate` | Chaîne incomplète |
| `10` | `certificate has expired` | Certificat périmé |
| `18` | `self-signed certificate` | CA non reconnue |
| `20` | `unable to get local issuer certificate` | Root CA manquante |
| `21` | `unable to verify first certificate` | Chaîne non présentée |

---

**Version :** 1.0
**Dernière mise à jour :** 2025-01-28
