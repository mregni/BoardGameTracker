/**
 * Centralized type definitions for player detail page components
 */

import { LegacyRef } from 'react';

import { Badge, Player } from '@/models';

// Player Header Types
export interface PlayerHeaderProps {
  playerName: string;
  onEdit: () => void;
  onDelete: () => void;
}

// Player Statistics Types
export interface PlayerStatistics {
  playCount: number;
  totalPlayedTime: number | null;
  winCount: number;
  distinctGameCount: number;
  mostWinsGame: MostWinsGame | null;
}

export interface MostWinsGame {
  id: string;
  title: string | null;
  image: string | null;
  totalWins: number | null;
}

export interface PlayerStatisticsGridProps {
  statistics: PlayerStatistics;
}

// Player Hero Section Types
export interface PlayerHeroSectionProps {
  player: Player;
  statistics: PlayerStatistics;
  playerId: string;
  achievementsRef: LegacyRef<HTMLDivElement> | undefined;
}

// Player Badge Container Types
export interface PlayerBadgeContainerProps {
  badges: Badge[];
  achievementsRef: LegacyRef<HTMLDivElement> | undefined;
}

// Player Achievements Section Types
export interface PlayerAchievementsSectionProps {
  badges: Badge[];
  achievementsRef: LegacyRef<HTMLDivElement> | undefined;
}

// Re-export Player and Badge from models
export type { Player, Badge } from '@/models';
