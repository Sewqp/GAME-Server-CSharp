-- Game-Server-CSharp Schema
-- MySQL 8.x / utf8mb4

CREATE DATABASE IF NOT EXISTS game_server_cs
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_bin;

USE game_server_cs;

-- ──────────────────────────────────────────────────────────────
-- player
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `player` (
    `player_id`  BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `pname`      VARCHAR(30)     NOT NULL,
    `status`     TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=정상 1=정지 2=영구밴',
    `created_at` DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`player_id`),
    UNIQUE KEY `uq_player_pname` (`pname`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- character_stat
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `character_stat` (
    `player_id`   BIGINT UNSIGNED NOT NULL,
    `level`       INT UNSIGNED    NOT NULL DEFAULT 1,
    `hp_max`      INT UNSIGNED    NOT NULL,
    `hp`          INT UNSIGNED    NOT NULL,
    `mp_max`      INT UNSIGNED    NOT NULL,
    `mp`          INT UNSIGNED    NOT NULL,
    `is_alive`    TINYINT UNSIGNED NOT NULL DEFAULT 1,
    `last_map_id` INT UNSIGNED    NOT NULL,
    PRIMARY KEY (`player_id`),
    CONSTRAINT `fk_stat_player`
        FOREIGN KEY (`player_id`) REFERENCES `player` (`player_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- item_definition
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `item_definition` (
    `item_def_id` INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `item_name`   VARCHAR(50)     NOT NULL,
    `item_desc`   TEXT,
    `item_type`   TINYINT UNSIGNED NOT NULL COMMENT '0=장비 1=소모품',
    `created_at`  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`item_def_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- item_instance
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `item_instance` (
    `item_instance_id` BIGINT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `item_def_id`      INT UNSIGNED     NOT NULL,
    `item_status`      TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=정상 1=밴 2=만료',
    `expired_at`       DATETIME         DEFAULT NULL COMMENT 'NULL=영구',
    PRIMARY KEY (`item_instance_id`),
    CONSTRAINT `fk_instance_def`
        FOREIGN KEY (`item_def_id`) REFERENCES `item_definition` (`item_def_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- inventory
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `inventory` (
    `inventory_id`     BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `player_id`        BIGINT UNSIGNED NOT NULL,
    `item_instance_id` BIGINT UNSIGNED NOT NULL,
    `item_count`       INT UNSIGNED    NOT NULL DEFAULT 1,
    `acquired_at`      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`inventory_id`),
    KEY `idx_inventory_player` (`player_id`),
    CONSTRAINT `fk_inventory_player`
        FOREIGN KEY (`player_id`)        REFERENCES `player`        (`player_id`),
    CONSTRAINT `fk_inventory_instance`
        FOREIGN KEY (`item_instance_id`) REFERENCES `item_instance` (`item_instance_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- item_definition 시드 데이터
INSERT IGNORE INTO `item_definition` (`item_def_id`, `item_name`, `item_desc`, `item_type`) VALUES
    (1, '체력 물약',   'HP를 회복하는 소모품', 1),
    (2, '마나 물약',   'MP를 회복하는 소모품', 1),
    (3, '강철 검',     '기본 공격 장비',       0),
    (4, '가죽 갑옷',   '기본 방어 장비',       0);

-- ──────────────────────────────────────────────────────────────
-- guild
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `guild` (
    `guild_id`     BIGINT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `guild_name`   VARCHAR(30)      NOT NULL,
    `guild_status` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=정상 1=해산',
    `created_at`   DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`guild_id`),
    UNIQUE KEY `uq_guild_name` (`guild_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- guild_member
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `guild_member` (
    `guild_id`  BIGINT UNSIGNED  NOT NULL,
    `player_id` BIGINT UNSIGNED  NOT NULL,
    `role`      TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=일반 1=부길드장 2=길드장',
    `joined_at` DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`guild_id`, `player_id`),
    CONSTRAINT `fk_gm_guild`
        FOREIGN KEY (`guild_id`)  REFERENCES `guild`  (`guild_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_gm_player`
        FOREIGN KEY (`player_id`) REFERENCES `player` (`player_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- channel
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `channel` (
    `channel_id`   VARCHAR(50)      NOT NULL,
    `channel_type` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=일반 1=길드',
    `created_at`   DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`channel_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- match_history
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `match_history` (
    `match_id`     BIGINT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `match_type`   TINYINT UNSIGNED NOT NULL,
    `player_count` INT UNSIGNED     NOT NULL,
    `started_at`   DATETIME         NOT NULL,
    `ended_at`     DATETIME         DEFAULT NULL,
    PRIMARY KEY (`match_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- ──────────────────────────────────────────────────────────────
-- match_player
-- ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `match_player` (
    `match_id`  BIGINT UNSIGNED  NOT NULL,
    `player_id` BIGINT UNSIGNED  NOT NULL,
    `result`    TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0=패 1=승',
    PRIMARY KEY (`match_id`, `player_id`),
    CONSTRAINT `fk_mp_match`
        FOREIGN KEY (`match_id`)  REFERENCES `match_history` (`match_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_mp_player`
        FOREIGN KEY (`player_id`) REFERENCES `player`        (`player_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;
