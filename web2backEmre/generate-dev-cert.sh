#!/bin/bash

# https dizinini oluştur
mkdir -p https

# Development sertifikası oluştur
dotnet dev-certs https -ep https/dev-cert.pfx -p "YourStrong!Passw0rd"

# Sertifika izinlerini ayarla
chmod 644 https/dev-cert.pfx 