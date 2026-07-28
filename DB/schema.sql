-- Game-Server-CSharp Schema
-- PostgreSQL 16.x / UTF8
-- 이 스크립트는 docker-entrypoint-initdb.d에서 POSTGRES_DB로 이미 생성된 DB에 연결된 상태로 실행됨
-- (로컬에서 수동 적용 시에도 대상 DB에 접속한 뒤 실행할 것 — CREATE DATABASE 문 없음)

-- ──────────────────────────────────────────────────────────────
-- player
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS player (
    player_id  BIGINT GENERATED ALWAYS AS IDENTITY,
    pname      VARCHAR(30) NOT NULL,
    status     SMALLINT    NOT NULL DEFAULT 0, -- 0=정상 1=정지 2=영구밴
    created_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id),
    CONSTRAINT uq_player_pname UNIQUE (pname)
);

-- MySQL의 `ON UPDATE CURRENT_TIMESTAMP` 대응 — UPDATE 시 updated_at 자동 갱신 트리거
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_player_updated_at ON player;
CREATE TRIGGER trg_player_updated_at
    BEFORE UPDATE ON player
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ──────────────────────────────────────────────────────────────
-- character_stat
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS character_stat (
    player_id   BIGINT   NOT NULL,
    level       INT      NOT NULL DEFAULT 1,
    hp_max      INT      NOT NULL,
    hp          INT      NOT NULL,
    mp_max      INT      NOT NULL,
    mp          INT      NOT NULL,
    is_alive    SMALLINT NOT NULL DEFAULT 1,
    last_map_id INT      NOT NULL,
    PRIMARY KEY (player_id),
    CONSTRAINT fk_stat_player
        FOREIGN KEY (player_id) REFERENCES player (player_id) ON DELETE CASCADE
);

-- ──────────────────────────────────────────────────────────────
-- item_definition
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS item_definition (
    item_def_id INT GENERATED ALWAYS AS IDENTITY,
    item_name   VARCHAR(50) NOT NULL,
    item_desc   TEXT,
    item_type   SMALLINT    NOT NULL, -- 0=장비 1=소모품
    created_at  TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (item_def_id)
);

-- ──────────────────────────────────────────────────────────────
-- item_instance
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS item_instance (
    item_instance_id BIGINT GENERATED ALWAYS AS IDENTITY,
    item_def_id      INT       NOT NULL,
    item_status      SMALLINT  NOT NULL DEFAULT 0, -- 0=정상 1=밴 2=만료
    expired_at       TIMESTAMP DEFAULT NULL,        -- NULL=영구
    PRIMARY KEY (item_instance_id),
    CONSTRAINT fk_instance_def
        FOREIGN KEY (item_def_id) REFERENCES item_definition (item_def_id)
);

-- ──────────────────────────────────────────────────────────────
-- inventory
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS inventory (
    inventory_id     BIGINT GENERATED ALWAYS AS IDENTITY,
    player_id        BIGINT    NOT NULL,
    item_instance_id BIGINT    NOT NULL,
    item_count       INT       NOT NULL DEFAULT 1,
    acquired_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (inventory_id),
    CONSTRAINT fk_inventory_player
        FOREIGN KEY (player_id)        REFERENCES player        (player_id),
    CONSTRAINT fk_inventory_instance
        FOREIGN KEY (item_instance_id) REFERENCES item_instance (item_instance_id)
);

CREATE INDEX IF NOT EXISTS idx_inventory_player ON inventory (player_id);

-- item_definition 시드 데이터
-- GENERATED ALWAYS AS IDENTITY 컬럼에 명시적 값을 넣으려면 OVERRIDING SYSTEM VALUE 필요
INSERT INTO item_definition (item_def_id, item_name, item_desc, item_type)
OVERRIDING SYSTEM VALUE VALUES
    (1, '체력 물약', 'HP를 회복하는 소모품', 1),
    (2, '마나 물약', 'MP를 회복하는 소모품', 1),
    (3, '강철 검',   '기본 공격 장비',       0),
    (4, '가죽 갑옷', '기본 방어 장비',       0)
ON CONFLICT (item_def_id) DO NOTHING;

-- item_def_id를 명시적으로 넣었으므로 시퀀스를 다음 값으로 맞춰줌 (IDENTITY 컬럼에 명시적 삽입 시 필요)
SELECT setval(pg_get_serial_sequence('item_definition', 'item_def_id'), 4, true);

-- ──────────────────────────────────────────────────────────────
-- guild
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS guild (
    guild_id     BIGINT GENERATED ALWAYS AS IDENTITY,
    guild_name   VARCHAR(30) NOT NULL,
    guild_status SMALLINT    NOT NULL DEFAULT 0, -- 0=정상 1=해산
    created_at   TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (guild_id),
    CONSTRAINT uq_guild_name UNIQUE (guild_name)
);

-- ──────────────────────────────────────────────────────────────
-- guild_member
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS guild_member (
    guild_id  BIGINT    NOT NULL,
    player_id BIGINT    NOT NULL,
    role      SMALLINT  NOT NULL DEFAULT 0, -- 0=일반 1=부길드장 2=길드장
    joined_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (guild_id, player_id),
    CONSTRAINT fk_gm_guild
        FOREIGN KEY (guild_id)  REFERENCES guild  (guild_id) ON DELETE CASCADE,
    CONSTRAINT fk_gm_player
        FOREIGN KEY (player_id) REFERENCES player (player_id) ON DELETE CASCADE
);

-- ──────────────────────────────────────────────────────────────
-- channel
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS channel (
    channel_id   VARCHAR(50) NOT NULL,
    channel_type SMALLINT    NOT NULL DEFAULT 0, -- 0=일반 1=길드
    created_at   TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (channel_id)
);

-- ──────────────────────────────────────────────────────────────
-- match_history
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS match_history (
    match_id     BIGINT GENERATED ALWAYS AS IDENTITY,
    match_type   SMALLINT  NOT NULL,
    player_count INT       NOT NULL,
    started_at   TIMESTAMP NOT NULL,
    ended_at     TIMESTAMP DEFAULT NULL,
    PRIMARY KEY (match_id)
);

-- ──────────────────────────────────────────────────────────────
-- match_player
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS match_player (
    match_id  BIGINT   NOT NULL,
    player_id BIGINT   NOT NULL,
    result    SMALLINT NOT NULL DEFAULT 0, -- 0=패 1=승
    PRIMARY KEY (match_id, player_id),
    CONSTRAINT fk_mp_match
        FOREIGN KEY (match_id)  REFERENCES match_history (match_id) ON DELETE CASCADE,
    CONSTRAINT fk_mp_player
        FOREIGN KEY (player_id) REFERENCES player        (player_id) ON DELETE CASCADE
);
