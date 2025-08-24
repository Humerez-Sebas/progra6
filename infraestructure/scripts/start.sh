#!/bin/bash

# Script para iniciar la infraestructura de BattleTanks

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_message() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

if ! command -v docker &> /dev/null; then
    print_error "Docker no está instalado. Por favor, instala Docker primero."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    print_error "Docker Compose no está instalado. Por favor, instala Docker Compose primero."
    exit 1
fi

print_message "Creando directorios necesarios..."
mkdir -p logs/nginx
mkdir -p data/postgres
mkdir -p data/redis

chmod 755 logs/nginx
chmod 755 data/postgres
chmod 755 data/redis

print_message "Iniciando servicios de infraestructura..."
docker-compose up -d

print_message "Esperando a que los servicios estén listos..."
sleep 10

print_message "Verificando estado de servicios..."

if docker-compose exec postgres pg_isready -U battleuser -d battle_tanks > /dev/null 2>&1; then
    print_success "PostgreSQL está funcionando correctamente"
else
    print_warning "PostgreSQL podría no estar completamente listo"
fi

if docker-compose exec redis redis-cli ping > /dev/null 2>&1; then
    print_success "Redis está funcionando correctamente"
else
    print_warning "Redis podría no estar completamente listo"
fi

print_message "Servicios disponibles:"
echo "  - PostgreSQL: localhost:5432"
echo "  - Redis: localhost:6379"
echo "  - pgAdmin: http://localhost:8080"
echo "  - Redis Commander: http://localhost:8081"
echo "  - Nginx: http://localhost:80"

print_message "Credenciales:"
echo "  PostgreSQL:"
echo "    - Base de datos: battle_tanks"
echo "    - Usuario: battleuser"
echo "    - Contraseña: battlepass"
echo "  pgAdmin:"
echo "    - Email: admin@battletanks.com"
echo "    - Contraseña: admin123"

print_success "Infraestructura iniciada correctamente!"
print_message "Para detener los servicios, ejecuta: ./scripts/stop.sh"