-- --------------------------------------------------------
-- Host:                         uk.nervaware.co.uk
-- Server version:               8.0.36-28 - Percona Server (GPL), Release '28', Revision '47601f19'$
-- Server OS:                    Linux
-- HeidiSQL Version:             12.10.0.7000
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for hlstatsx
CREATE DATABASE IF NOT EXISTS `hlstatsx` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `hlstatsx`;

-- Dumping structure for table hlstatsx.geoLiteCity_Blocks
CREATE TABLE IF NOT EXISTS `geoLiteCity_Blocks` (
  `startIpNum` bigint unsigned NOT NULL DEFAULT '0',
  `endIpNum` bigint unsigned NOT NULL DEFAULT '0',
  `locId` bigint unsigned NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.geoLiteCity_Location
CREATE TABLE IF NOT EXISTS `geoLiteCity_Location` (
  `locId` bigint unsigned NOT NULL DEFAULT '0',
  `country` varchar(2) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `region` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `city` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `postalCode` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `latitude` decimal(14,4) DEFAULT NULL,
  `longitude` decimal(14,4) DEFAULT NULL,
  PRIMARY KEY (`locId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Actions
CREATE TABLE IF NOT EXISTS `hlstats_Actions` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `reward_player` int NOT NULL DEFAULT '10',
  `reward_team` int NOT NULL DEFAULT '0',
  `team` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `description` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `for_PlayerActions` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `for_PlayerPlayerActions` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `for_TeamActions` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `for_WorldActions` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `count` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  UNIQUE KEY `gamecode` (`code`,`game`,`team`),
  KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=722 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Awards
CREATE TABLE IF NOT EXISTS `hlstats_Awards` (
  `awardId` int unsigned NOT NULL AUTO_INCREMENT,
  `awardType` char(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'W',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `code` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `verb` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `d_winner_id` int unsigned DEFAULT NULL,
  `d_winner_count` int unsigned DEFAULT NULL,
  `g_winner_id` int unsigned DEFAULT NULL,
  `g_winner_count` int unsigned DEFAULT NULL,
  PRIMARY KEY (`awardId`),
  UNIQUE KEY `code` (`game`,`awardType`,`code`)
) ENGINE=InnoDB AUTO_INCREMENT=958 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Clans
CREATE TABLE IF NOT EXISTS `hlstats_Clans` (
  `clanId` int unsigned NOT NULL AUTO_INCREMENT,
  `tag` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `homepage` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hidden` tinyint unsigned NOT NULL DEFAULT '0',
  `mapregion` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`clanId`),
  UNIQUE KEY `tag` (`game`,`tag`),
  KEY `game` (`game`)
) ENGINE=InnoDB AUTO_INCREMENT=434 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_ClanTags
CREATE TABLE IF NOT EXISTS `hlstats_ClanTags` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `pattern` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `position` enum('EITHER','START','END') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'EITHER',
  PRIMARY KEY (`id`),
  UNIQUE KEY `pattern` (`pattern`)
) ENGINE=InnoDB AUTO_INCREMENT=30 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Countries
CREATE TABLE IF NOT EXISTS `hlstats_Countries` (
  `flag` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`flag`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Admin
CREATE TABLE IF NOT EXISTS `hlstats_Events_Admin` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `type` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Unknown',
  `message` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerName` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_ChangeName
CREATE TABLE IF NOT EXISTS `hlstats_Events_ChangeName` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `oldName` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `newName` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB AUTO_INCREMENT=63 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_ChangeRole
CREATE TABLE IF NOT EXISTS `hlstats_Events_ChangeRole` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `role` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=9575838 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_ChangeTeam
CREATE TABLE IF NOT EXISTS `hlstats_Events_ChangeTeam` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `team` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=3440632 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Chat
CREATE TABLE IF NOT EXISTS `hlstats_Events_Chat` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `message_mode` tinyint NOT NULL DEFAULT '0',
  `message` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `serverId` (`serverId`),
  KEY `eventTime` (`eventTime`),
  FULLTEXT KEY `message` (`message`)
) ENGINE=InnoDB AUTO_INCREMENT=25385 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Connects
CREATE TABLE IF NOT EXISTS `hlstats_Events_Connects` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `ipAddress` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hostname` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hostgroup` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `eventTime_Disconnect` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=1714650 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Disconnects
CREATE TABLE IF NOT EXISTS `hlstats_Events_Disconnects` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=474789 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Entries
CREATE TABLE IF NOT EXISTS `hlstats_Events_Entries` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=85439 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Frags
CREATE TABLE IF NOT EXISTS `hlstats_Events_Frags` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `killerId` int unsigned NOT NULL DEFAULT '0',
  `victimId` int unsigned NOT NULL DEFAULT '0',
  `weapon` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `headshot` tinyint(1) NOT NULL DEFAULT '0',
  `killerRole` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `victimRole` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `pos_x` mediumint DEFAULT NULL,
  `pos_y` mediumint DEFAULT NULL,
  `pos_z` mediumint DEFAULT NULL,
  `pos_victim_x` mediumint DEFAULT NULL,
  `pos_victim_y` mediumint DEFAULT NULL,
  `pos_victim_z` mediumint DEFAULT NULL,
  `mapId` int DEFAULT NULL,
  `roleId` int DEFAULT NULL,
  `weaponId` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `serverId` (`serverId`),
  KEY `headshot` (`headshot`),
  KEY `map` (`map`(5)),
  KEY `weapon16` (`weapon`(16)),
  KEY `killerRole` (`killerRole`(8)),
  KEY `killerId` (`killerId`,`eventTime`),
  KEY `victimId` (`victimId`,`eventTime`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=24264679 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Latency
CREATE TABLE IF NOT EXISTS `hlstats_Events_Latency` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `ping` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_PlayerActions
CREATE TABLE IF NOT EXISTS `hlstats_Events_PlayerActions` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `actionId` int unsigned NOT NULL DEFAULT '0',
  `bonus` int NOT NULL DEFAULT '0',
  `pos_x` mediumint DEFAULT NULL,
  `pos_y` mediumint DEFAULT NULL,
  `pos_z` mediumint DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `actionId` (`actionId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=9223743 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_PlayerPlayerActions
CREATE TABLE IF NOT EXISTS `hlstats_Events_PlayerPlayerActions` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `victimId` int unsigned NOT NULL DEFAULT '0',
  `actionId` int unsigned NOT NULL DEFAULT '0',
  `bonus` int NOT NULL DEFAULT '0',
  `pos_x` mediumint DEFAULT NULL,
  `pos_y` mediumint DEFAULT NULL,
  `pos_z` mediumint DEFAULT NULL,
  `pos_victim_x` mediumint DEFAULT NULL,
  `pos_victim_y` mediumint DEFAULT NULL,
  `pos_victim_z` mediumint DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `actionId` (`actionId`),
  KEY `victimId` (`victimId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=1277466 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Rcon
CREATE TABLE IF NOT EXISTS `hlstats_Events_Rcon` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `type` varchar(6) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'UNK',
  `remoteIp` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `password` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `command` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Statsme
CREATE TABLE IF NOT EXISTS `hlstats_Events_Statsme` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `weapon` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `shots` int unsigned NOT NULL DEFAULT '0',
  `hits` int unsigned NOT NULL DEFAULT '0',
  `headshots` int unsigned NOT NULL DEFAULT '0',
  `damage` int unsigned NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `weapon` (`weapon`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=7652487 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Statsme2
CREATE TABLE IF NOT EXISTS `hlstats_Events_Statsme2` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `weapon` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `head` int unsigned NOT NULL DEFAULT '0',
  `chest` int unsigned NOT NULL DEFAULT '0',
  `stomach` int unsigned NOT NULL DEFAULT '0',
  `leftarm` int unsigned NOT NULL DEFAULT '0',
  `rightarm` int unsigned NOT NULL DEFAULT '0',
  `leftleg` int unsigned NOT NULL DEFAULT '0',
  `rightleg` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `weapon` (`weapon`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=7652441 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_StatsmeLatency
CREATE TABLE IF NOT EXISTS `hlstats_Events_StatsmeLatency` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `ping` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_StatsmeTime
CREATE TABLE IF NOT EXISTS `hlstats_Events_StatsmeTime` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `time` time NOT NULL DEFAULT '00:00:00',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Suicides
CREATE TABLE IF NOT EXISTS `hlstats_Events_Suicides` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `weapon` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `pos_x` mediumint DEFAULT NULL,
  `pos_y` mediumint DEFAULT NULL,
  `pos_z` mediumint DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_TeamBonuses
CREATE TABLE IF NOT EXISTS `hlstats_Events_TeamBonuses` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `actionId` int unsigned NOT NULL DEFAULT '0',
  `bonus` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `playerId` (`playerId`),
  KEY `actionId` (`actionId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=34961198 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Events_Teamkills
CREATE TABLE IF NOT EXISTS `hlstats_Events_Teamkills` (
  `id` int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime` datetime DEFAULT NULL,
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `killerId` int unsigned NOT NULL DEFAULT '0',
  `victimId` int unsigned NOT NULL DEFAULT '0',
  `weapon` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `pos_x` mediumint DEFAULT NULL,
  `pos_y` mediumint DEFAULT NULL,
  `pos_z` mediumint DEFAULT NULL,
  `pos_victim_x` mediumint DEFAULT NULL,
  `pos_victim_y` mediumint DEFAULT NULL,
  `pos_victim_z` mediumint DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `killerId` (`killerId`),
  KEY `eventTime` (`eventTime`)
) ENGINE=InnoDB AUTO_INCREMENT=78 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Games
CREATE TABLE IF NOT EXISTS `hlstats_Games` (
  `code` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hidden` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `realgame` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'hl2mp',
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Games_Defaults
CREATE TABLE IF NOT EXISTS `hlstats_Games_Defaults` (
  `code` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `parameter` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`code`,`parameter`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Games_Supported
CREATE TABLE IF NOT EXISTS `hlstats_Games_Supported` (
  `code` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Heatmap_Config
CREATE TABLE IF NOT EXISTS `hlstats_Heatmap_Config` (
  `id` int NOT NULL AUTO_INCREMENT,
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `xoffset` float NOT NULL,
  `yoffset` float NOT NULL,
  `flipx` tinyint(1) NOT NULL DEFAULT '0',
  `flipy` tinyint(1) NOT NULL DEFAULT '1',
  `rotate` tinyint(1) NOT NULL DEFAULT '0',
  `days` tinyint NOT NULL DEFAULT '30',
  `brush` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'small',
  `scale` float NOT NULL,
  `font` tinyint NOT NULL DEFAULT '10',
  `thumbw` float NOT NULL DEFAULT '0.170312',
  `thumbh` float NOT NULL DEFAULT '0.170312',
  `cropx1` int NOT NULL DEFAULT '0',
  `cropy1` int NOT NULL DEFAULT '0',
  `cropx2` int NOT NULL DEFAULT '0',
  `cropy2` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  UNIQUE KEY `gamemap` (`map`,`game`)
) ENGINE=InnoDB AUTO_INCREMENT=443 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_HostGroups
CREATE TABLE IF NOT EXISTS `hlstats_HostGroups` (
  `id` int NOT NULL AUTO_INCREMENT,
  `pattern` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Livestats
CREATE TABLE IF NOT EXISTS `hlstats_Livestats` (
  `player_id` int NOT NULL DEFAULT '0',
  `server_id` int NOT NULL DEFAULT '0',
  `cli_address` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `cli_city` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `cli_country` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `cli_flag` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `cli_state` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `cli_lat` float(7,4) DEFAULT NULL,
  `cli_lng` float(7,4) DEFAULT NULL,
  `steam_id` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `team` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `kills` int NOT NULL DEFAULT '0',
  `deaths` int NOT NULL DEFAULT '0',
  `suicides` int NOT NULL DEFAULT '0',
  `headshots` int NOT NULL DEFAULT '0',
  `shots` int NOT NULL DEFAULT '0',
  `hits` int NOT NULL DEFAULT '0',
  `is_dead` tinyint(1) NOT NULL DEFAULT '0',
  `has_bomb` int NOT NULL DEFAULT '0',
  `ping` int NOT NULL DEFAULT '0',
  `connected` int NOT NULL DEFAULT '0',
  `skill_change` int NOT NULL DEFAULT '0',
  `skill` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`player_id`)
) ENGINE=MEMORY DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Maps_Counts
CREATE TABLE IF NOT EXISTS `hlstats_Maps_Counts` (
  `rowId` int NOT NULL AUTO_INCREMENT,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `kills` int NOT NULL,
  `headshots` int NOT NULL,
  PRIMARY KEY (`game`,`map`),
  KEY `rowId` (`rowId`)
) ENGINE=InnoDB AUTO_INCREMENT=22865361 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Maps_Name
CREATE TABLE IF NOT EXISTS `hlstats_Maps_Name` (
  `id` int unsigned NOT NULL,
  `name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `id_UNIQUE` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Mods_Defaults
CREATE TABLE IF NOT EXISTS `hlstats_Mods_Defaults` (
  `code` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `parameter` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`code`,`parameter`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Mods_Supported
CREATE TABLE IF NOT EXISTS `hlstats_Mods_Supported` (
  `code` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Options
CREATE TABLE IF NOT EXISTS `hlstats_Options` (
  `keyname` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `opttype` tinyint NOT NULL DEFAULT '1',
  PRIMARY KEY (`keyname`),
  KEY `opttype` (`opttype`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Options_Choices
CREATE TABLE IF NOT EXISTS `hlstats_Options_Choices` (
  `keyname` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `text` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `isDefault` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`keyname`,`value`),
  KEY `keyname` (`keyname`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_PlayerNames
CREATE TABLE IF NOT EXISTS `hlstats_PlayerNames` (
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `lastuse` datetime DEFAULT NULL,
  `connection_time` int unsigned NOT NULL DEFAULT '0',
  `numuses` int unsigned NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  `suicides` int unsigned NOT NULL DEFAULT '0',
  `headshots` int unsigned NOT NULL DEFAULT '0',
  `shots` int unsigned NOT NULL DEFAULT '0',
  `hits` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`playerId`,`name`),
  KEY `name16` (`name`(16))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Players
CREATE TABLE IF NOT EXISTS `hlstats_Players` (
  `playerId` int unsigned NOT NULL AUTO_INCREMENT,
  `last_event` int NOT NULL DEFAULT '0',
  `connection_time` int unsigned NOT NULL DEFAULT '0',
  `lastName` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `lastAddress` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `city` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `state` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `country` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `flag` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `lat` float(7,4) DEFAULT NULL,
  `lng` float(7,4) DEFAULT NULL,
  `clan` int unsigned NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  `suicides` int unsigned NOT NULL DEFAULT '0',
  `skill` int unsigned NOT NULL DEFAULT '1000',
  `shots` int unsigned NOT NULL DEFAULT '0',
  `hits` int unsigned NOT NULL DEFAULT '0',
  `teamkills` int unsigned NOT NULL DEFAULT '0',
  `fullName` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `email` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `homepage` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `icq` int unsigned DEFAULT NULL,
  `mmrank` tinyint DEFAULT NULL,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `hideranking` int unsigned NOT NULL DEFAULT '0',
  `headshots` int unsigned NOT NULL DEFAULT '0',
  `last_skill_change` int NOT NULL DEFAULT '0',
  `displayEvents` int unsigned NOT NULL DEFAULT '1',
  `kill_streak` int NOT NULL DEFAULT '0',
  `death_streak` int NOT NULL DEFAULT '0',
  `blockavatar` int unsigned NOT NULL DEFAULT '0',
  `activity` int NOT NULL DEFAULT '100',
  `createdate` int NOT NULL DEFAULT '0',
  `game_rank` int unsigned DEFAULT NULL,
  PRIMARY KEY (`playerId`),
  KEY `playerclan` (`clan`,`playerId`),
  KEY `skill` (`skill`),
  KEY `game` (`game`),
  KEY `kills` (`kills`),
  KEY `hideranking` (`hideranking`) /*!80000 INVISIBLE */,
  KEY `createdate` (`createdate`)
) ENGINE=InnoDB AUTO_INCREMENT=10336 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Players_Awards
CREATE TABLE IF NOT EXISTS `hlstats_Players_Awards` (
  `awardTime` date NOT NULL,
  `awardId` int unsigned NOT NULL DEFAULT '0',
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `count` int unsigned NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`awardTime`,`awardId`,`playerId`,`game`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Players_History
CREATE TABLE IF NOT EXISTS `hlstats_Players_History` (
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `eventTime` date DEFAULT NULL,
  `connection_time` int unsigned NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  `suicides` int unsigned NOT NULL DEFAULT '0',
  `skill` int unsigned NOT NULL DEFAULT '1000',
  `shots` int unsigned NOT NULL DEFAULT '0',
  `hits` int unsigned NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `headshots` int unsigned NOT NULL DEFAULT '0',
  `teamkills` int unsigned NOT NULL DEFAULT '0',
  `kill_streak` int NOT NULL DEFAULT '0',
  `death_streak` int NOT NULL DEFAULT '0',
  `skill_change` int NOT NULL DEFAULT '0',
  UNIQUE KEY `eventTime` (`eventTime`,`playerId`,`game`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Players_Ribbons
CREATE TABLE IF NOT EXISTS `hlstats_Players_Ribbons` (
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `ribbonId` int unsigned NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_PlayerUniqueIds
CREATE TABLE IF NOT EXISTS `hlstats_PlayerUniqueIds` (
  `playerId` int unsigned NOT NULL DEFAULT '0',
  `uniqueId` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `merge` int unsigned DEFAULT NULL,
  PRIMARY KEY (`uniqueId`,`game`),
  KEY `playerId` (`playerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Ranks
CREATE TABLE IF NOT EXISTS `hlstats_Ranks` (
  `rankId` int unsigned NOT NULL AUTO_INCREMENT,
  `image` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `minKills` int unsigned NOT NULL DEFAULT '0',
  `maxKills` int NOT NULL DEFAULT '0',
  `rankName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`rankId`),
  UNIQUE KEY `rankgame` (`image`,`game`),
  KEY `game` (`game`(8))
) ENGINE=InnoDB AUTO_INCREMENT=1181 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Ribbons
CREATE TABLE IF NOT EXISTS `hlstats_Ribbons` (
  `ribbonId` int unsigned NOT NULL AUTO_INCREMENT,
  `awardCode` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `awardCount` int NOT NULL DEFAULT '0',
  `special` tinyint NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `image` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `ribbonName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`ribbonId`),
  UNIQUE KEY `award` (`awardCode`,`awardCount`,`game`,`special`)
) ENGINE=InnoDB AUTO_INCREMENT=1798 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Roles
CREATE TABLE IF NOT EXISTS `hlstats_Roles` (
  `roleId` int unsigned NOT NULL AUTO_INCREMENT,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hidden` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `picked` int unsigned NOT NULL DEFAULT '0',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `deaths` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`roleId`),
  UNIQUE KEY `gamecode` (`game`,`code`)
) ENGINE=InnoDB AUTO_INCREMENT=161 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Roles_Names
CREATE TABLE IF NOT EXISTS `hlstats_Roles_Names` (
  `id` int NOT NULL,
  `name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Servers
CREATE TABLE IF NOT EXISTS `hlstats_Servers` (
  `serverId` int unsigned NOT NULL AUTO_INCREMENT,
  `address` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `port` int unsigned NOT NULL DEFAULT '0',
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `sortorder` tinyint NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `publicaddress` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `statusurl` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `rcon_password` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `kills` int NOT NULL DEFAULT '0',
  `players` int NOT NULL DEFAULT '0',
  `rounds` int NOT NULL DEFAULT '0',
  `suicides` int NOT NULL DEFAULT '0',
  `headshots` int NOT NULL DEFAULT '0',
  `bombs_planted` int NOT NULL DEFAULT '0',
  `bombs_defused` int NOT NULL DEFAULT '0',
  `ct_wins` int NOT NULL DEFAULT '0',
  `ts_wins` int NOT NULL DEFAULT '0',
  `act_players` int NOT NULL DEFAULT '0',
  `max_players` int NOT NULL DEFAULT '0',
  `act_map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `map_rounds` int NOT NULL DEFAULT '0',
  `map_ct_wins` int NOT NULL DEFAULT '0',
  `map_ts_wins` int NOT NULL DEFAULT '0',
  `map_started` int NOT NULL DEFAULT '0',
  `map_changes` int NOT NULL DEFAULT '0',
  `ct_shots` int NOT NULL DEFAULT '0',
  `ct_hits` int NOT NULL DEFAULT '0',
  `ts_shots` int NOT NULL DEFAULT '0',
  `ts_hits` int NOT NULL DEFAULT '0',
  `map_ct_shots` int NOT NULL DEFAULT '0',
  `map_ct_hits` int NOT NULL DEFAULT '0',
  `map_ts_shots` int NOT NULL DEFAULT '0',
  `map_ts_hits` int NOT NULL DEFAULT '0',
  `lat` float(7,4) DEFAULT NULL,
  `lng` float(7,4) DEFAULT NULL,
  `city` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `country` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `last_event` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`serverId`),
  UNIQUE KEY `addressport` (`address`,`port`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci PACK_KEYS=0;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Servers_Config
CREATE TABLE IF NOT EXISTS `hlstats_Servers_Config` (
  `serverId` int unsigned NOT NULL DEFAULT '0',
  `parameter` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `serverConfigId` int unsigned NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`serverId`,`parameter`),
  KEY `serverConfigId` (`serverConfigId`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Servers_Config_Default
CREATE TABLE IF NOT EXISTS `hlstats_Servers_Config_Default` (
  `parameter` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`parameter`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Servers_VoiceComm
CREATE TABLE IF NOT EXISTS `hlstats_Servers_VoiceComm` (
  `serverId` int unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `addr` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `password` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `descr` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `queryPort` int unsigned NOT NULL DEFAULT '51234',
  `UDPPort` int unsigned NOT NULL DEFAULT '8767',
  `serverType` tinyint NOT NULL DEFAULT '0',
  PRIMARY KEY (`serverId`),
  UNIQUE KEY `address` (`addr`,`UDPPort`,`queryPort`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_server_load
CREATE TABLE IF NOT EXISTS `hlstats_server_load` (
  `server_id` int NOT NULL DEFAULT '0',
  `timestamp` int NOT NULL DEFAULT '0',
  `act_players` tinyint NOT NULL DEFAULT '0',
  `min_players` tinyint NOT NULL DEFAULT '0',
  `max_players` tinyint NOT NULL DEFAULT '0',
  `map` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `uptime` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `fps` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  KEY `timestamp` (`timestamp`),
  KEY `server_id` (`server_id`,`timestamp` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Teams
CREATE TABLE IF NOT EXISTS `hlstats_Teams` (
  `teamId` int unsigned NOT NULL AUTO_INCREMENT,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `hidden` enum('0','1') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  `playerlist_bgcolor` varchar(7) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `playerlist_color` varchar(7) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `playerlist_index` tinyint unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`teamId`),
  UNIQUE KEY `gamecode` (`game`,`code`)
) ENGINE=InnoDB AUTO_INCREMENT=67 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Trend
CREATE TABLE IF NOT EXISTS `hlstats_Trend` (
  `timestamp` int NOT NULL DEFAULT '0',
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `players` int NOT NULL DEFAULT '0',
  `kills` int NOT NULL DEFAULT '0',
  `headshots` int NOT NULL DEFAULT '0',
  `servers` int NOT NULL DEFAULT '0',
  `act_slots` int NOT NULL DEFAULT '0',
  `max_slots` int NOT NULL DEFAULT '0',
  KEY `game` (`game`),
  KEY `timestamp` (`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Users
CREATE TABLE IF NOT EXISTS `hlstats_Users` (
  `username` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `password` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `acclevel` int NOT NULL DEFAULT '0',
  `playerId` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Weapons
CREATE TABLE IF NOT EXISTS `hlstats_Weapons` (
  `weaponId` int unsigned NOT NULL AUTO_INCREMENT,
  `game` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'valve',
  `code` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '',
  `modifier` float(10,2) NOT NULL DEFAULT '1.00',
  `kills` int unsigned NOT NULL DEFAULT '0',
  `headshots` int unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`weaponId`),
  UNIQUE KEY `gamecode` (`game`,`code`),
  KEY `code` (`code`),
  KEY `modifier` (`modifier`)
) ENGINE=InnoDB AUTO_INCREMENT=939 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table hlstatsx.hlstats_Weapons_Name
CREATE TABLE IF NOT EXISTS `hlstats_Weapons_Name` (
  `id` int NOT NULL,
  `name` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for procedure hlstatsx.spGlobalStats
DELIMITER //
CREATE PROCEDURE `spGlobalStats`()
BEGIN
  DECLARE v_total INT;
  DECLARE v_last24hrs INT;
  DECLARE v_countries INT;
  DECLARE v_frags INT;
  
SET v_total = (SELECT TABLE_ROWS FROM information_schema.tables WHERE TABLE_NAME = 'hlstats_Players');  
SET v_last24hrs = (SELECT count(*) FROM  hlstats_Players WHERE createdate >= UNIX_TIMESTAMP() - 86400);
SET v_countries = (SELECT count(DISTINCT country) FROM hlstatsx.hlstats_Players);
SET v_frags = (SELECT TABLE_ROWS FROM information_schema.tables WHERE TABLE_NAME = 'hlstats_Events_Frags');

SELECT
  v_total Total,
  v_last24hrs Last24Hrs,
  v_countries Countries,
  v_frags Frags;
  
  
END//
DELIMITER ;

-- Dumping structure for procedure hlstatsx.spPlayerStats
DELIMITER //
CREATE PROCEDURE `spPlayerStats`(p_steamId VARCHAR(64))
BEGIN
  DECLARE v_total_frags INT;
  DECLARE v_total_deaths INT;
  DECLARE v_total_deaths_last24hrs INT;
  DECLARE v_total_frags_last24hrs INT;
  DECLARE v_position INT;
  DECLARE v_points INT;

  DECLARE v_playerId INT;
  
  DECLARE v_uniqueId VARCHAR(64) CHARACTER SET 'utf8mb4' COLLATE 'utf8mb4_unicode_ci';
  IF LEFT(p_steamId, 8) = 'STEAM_0:' THEN
    SET v_uniqueId = SUBSTRING(p_steamId, 9);
    SET v_playerId = (SELECT playerId FROM hlstats_PlayerUniqueIds WHERE uniqueId = v_uniqueId);
  
    IF v_playerId IS NOT NULL THEN  
      SELECT
        kills, deaths, skill
  	  INTO
        v_total_frags,
        v_total_deaths,
        v_points
	  FROM	
        hlstats_Players
	  WHERE
        playerId = v_playerId;
	
      SET v_total_frags_last24hrs = (SELECT count(*) FROM hlstats_Events_Frags WHERE KillerId = v_playerId AND eventTime > DATE_ADD(NOW(), INTERVAL -24 HOUR));
      SET v_total_deaths_last24hrs = (SELECT count(*) FROM hlstats_Events_Frags WHERE VictimId = v_playerId AND eventTime > DATE_ADD(NOW(), INTERVAL -24 HOUR));
	  SET v_position = (SELECT count(*) FROM hlstats_Players WHERE hideranking = 0 AND skill > v_points) + 1;

      SELECT
        v_total_frags frags,
        v_total_deaths deaths,
        v_points points,
        v_total_frags_last24hrs frags_last24hrs,
        v_total_deaths_last24hrs deaths_last24hrs,	
        v_position position;
    END IF;  
  END IF;
END//
DELIMITER ;

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
