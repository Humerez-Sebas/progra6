.PHONY: help build up down restart logs clean backup restore test status

COMPOSE_FILE = docker-compose.yml
PROJECT_NAME = battletanks

help:
	@echo "BattleTanks Infrastructure - Comandos disponibles:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $1, $2}'

setup:
	@echo "Configurando infraestructura BattleTanks..."
	@mkdir -p logs data/postgres data/redis docs scripts
	@chmod +x scripts/*.sh
	@cp .env.example .env
	@echo "Configuración completada. Edita el archivo .env si es necesario."
	@echo "Ejecuta 'make setup-redis' después de 'make up' para configurar Redis."

setup-redis:
	@echo "Configurando Redis para BattleTanks..."
	@./scripts/setup-redis.sh

redis-info:
	@echo "Información de Redis:"
	@docker-compose exec redis redis-cli info memory | grep used_memory
	@echo ""
	@echo "Tamaño de cada base de datos:"
	@for i in 0 1 2 3 4 5 6 7; do \
		echo "  DB $i: $(docker-compose exec -T redis redis-cli -n $i dbsize) keys"; \
	done

redis-monitor:
	@echo "Monitoreando Redis (Ctrl+C para salir)..."
	@docker-compose exec redis redis-cli monitor

redis-stats:
	@echo "Estadísticas de Redis:"
	@docker-compose exec redis redis-cli info stats
	@echo ""
	@echo "Comandos más lentos:"
	@docker-compose exec redis redis-cli slowlog get 5

build:
	@echo "Construyendo imágenes..."
	@docker-compose -f $(COMPOSE_FILE) build

up:
	@echo "Iniciando servicios..."
	@docker-compose -f $(COMPOSE_FILE) up -d
	@echo "Esperando que los servicios estén listos..."
	@sleep 10
	@make status

down:
	@echo "Deteniendo servicios..."
	@docker-compose -f $(COMPOSE_FILE) down

restart:
	@echo "Reiniciando servicios..."
	@make down
	@make up

postgres-up:
	@docker-compose -f $(COMPOSE_FILE) up -d postgres

redis-up:
	@docker-compose -f $(COMPOSE_FILE) up -d redis

pgadmin-up:
	@docker-compose -f $(COMPOSE_FILE) up -d pgadmin

logs:
	@docker-compose -f $(COMPOSE_FILE) logs

logs-follow:
	@docker-compose -f $(COMPOSE_FILE) logs -f

logs-postgres:
	@docker-compose -f $(COMPOSE_FILE) logs postgres

logs-redis:
	@docker-compose -f $(COMPOSE_FILE) logs redis

status:
	@echo "Estado de servicios:"
	@docker-compose -f $(COMPOSE_FILE) ps
	@echo ""
	@echo "URLs disponibles:"
	@echo "  - pgAdmin: http://localhost:8080"
	@echo "  - Redis Commander: http://localhost:8081"
	@echo ""
	@echo "Credenciales:"
	@echo "  - PostgreSQL: battleuser/battlepass@localhost:5432/battle_tanks"
	@echo "  - pgAdmin: admin@battletanks.com/admin123"

health:
	@echo "Verificando salud de servicios..."
	@echo -n "PostgreSQL: "
	@docker-compose exec -T postgres pg_isready -U battleuser -d battle_tanks && echo "OK" || echo "Error"
	@echo -n "Redis: "
	@docker-compose exec -T redis redis-cli ping && echo "OK" || echo "Error"

postgres-shell:
	@docker-compose exec postgres psql -U battleuser -d battle_tanks

redis-cli:
	@docker-compose exec redis redis-cli

postgres-bash:
	@docker-compose exec postgres bash

redis-bash:
	@docker-compose exec redis sh

backup:
	@echo "Creando backup..."
	@mkdir -p backups
	@docker-compose exec postgres pg_dump -U battleuser battle_tanks > backups/backup_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "Backup creado en backups/"

restore:
	@echo "Restaurando backup..."
	@docker-compose exec -T postgres psql -U battleuser battle_tanks < $(FILE)
	@echo "Backup restaurado"

clean:
	@echo "Limpiando contenedores y volúmenes..."
	@docker-compose -f $(COMPOSE_FILE) down -v
	@docker system prune -f
	@echo "Limpieza completada"

clean-all:
	@echo "Limpieza completa..."
	@docker-compose -f $(COMPOSE_FILE) down -v --rmi all
	@docker system prune -af
	@echo "Limpieza completa terminada"

test:
	@echo "Ejecutando tests de conectividad..."
	@echo "Test PostgreSQL:"
	@docker-compose exec postgres psql -U battleuser -d battle_tanks -c "SELECT version();"
	@echo ""
	@echo "Test Redis:"
	@docker-compose exec redis redis-cli ping
	@echo ""
	@echo "Tests completados"

dev:
	@make up
	@make logs-follow

stats:
	@echo "Estadísticas de recursos:"
	@docker stats --no-stream

info:
	@echo "Información del proyecto:"
	@echo "  - Nombre: BattleTanks Infrastructure"
	@echo "  - Servicios: PostgreSQL, Redis, pgAdmin, Redis Commander"
	@echo "  - Docker Compose: $(COMPOSE_FILE)"
	@echo "  - Red: battletanks-network"
	@echo ""
	@make status

.DEFAULT_GOAL := help