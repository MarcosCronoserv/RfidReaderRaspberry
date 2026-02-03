#!/bin/bash
# =====================================================
#  Script de Execucao para Raspberry Pi
#  .NET 8 Self-Contained (nao precisa de Mono)
# =====================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Valores padrao
DEFAULT_IP="192.168.0.242"
DEFAULT_DURATION="30"

# Verifica se o executavel existe
if [ ! -f "$SCRIPT_DIR/RfidReaderRaspberry" ]; then
    echo "ERRO: Executavel nao encontrado em $SCRIPT_DIR"
    echo "Certifique-se de que copiou todos os arquivos da pasta publish."
    exit 1
fi

# Pega IP do leitor (argumento ou padrao)
READER_IP="${1:-$DEFAULT_IP}"
DURATION="${2:-$DEFAULT_DURATION}"

echo "============================================="
echo "  RFID Reader - Raspberry Pi"
echo "============================================="
echo "IP do Leitor: $READER_IP"
echo "Duracao: $DURATION segundos"
echo ""

# Executa diretamente (nao precisa de Mono)
cd "$SCRIPT_DIR"
./RfidReaderRaspberry "$READER_IP" "$DURATION"
