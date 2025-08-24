CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

CREATE SCHEMA IF NOT EXISTS game;
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS stats;

CREATE TABLE IF NOT EXISTS auth.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS game.rooms (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    max_players INTEGER DEFAULT 4,
    current_players INTEGER DEFAULT 0,
    status VARCHAR(20) DEFAULT 'waiting',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMP,
    finished_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS game.player_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
    room_id UUID REFERENCES game.rooms(id) ON DELETE CASCADE,
    tank_color VARCHAR(20),
    position_x INTEGER DEFAULT 0,
    position_y INTEGER DEFAULT 0,
    health INTEGER DEFAULT 100,
    score INTEGER DEFAULT 0,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    left_at TIMESTAMP
);

CREATE TABLE IF NOT EXISTS stats.game_statistics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE,
    room_id UUID REFERENCES game.rooms(id) ON DELETE CASCADE,
    kills INTEGER DEFAULT 0,
    deaths INTEGER DEFAULT 0,
    shots_fired INTEGER DEFAULT 0,
    shots_hit INTEGER DEFAULT 0,
    damage_dealt INTEGER DEFAULT 0,
    damage_received INTEGER DEFAULT 0,
    game_duration INTEGER,
    final_score INTEGER DEFAULT 0,
    final_position INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_users_username ON auth.users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON auth.users(email);
CREATE INDEX IF NOT EXISTS idx_rooms_status ON game.rooms(status);
CREATE INDEX IF NOT EXISTS idx_player_sessions_user_id ON game.player_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_player_sessions_room_id ON game.player_sessions(room_id);
CREATE INDEX IF NOT EXISTS idx_game_statistics_user_id ON stats.game_statistics(user_id);

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON auth.users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

INSERT INTO auth.users (username, email, password_hash) VALUES
    ('admin', 'admin@battletanks.com', '$2a$10$example.hash.for.admin'),
    ('player1', 'player1@battletanks.com', '$2a$10$example.hash.for.player1'),
    ('player2', 'player2@battletanks.com', '$2a$10$example.hash.for.player2')
ON CONFLICT (username) DO NOTHING;

\echo 'Base de datos BattleTanks inicializada correctamente!'