import { AxiosError } from 'axios';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { PlayerScoring } from '../models/Games/XValue';
import { TopPlayer } from '../models/Games/TopPlayer';
import { ScoreRank } from '../models/Games/ScoringRank';
import {
  FailResult,
  Game,
  GAME_CHARTS,
  GameStatistics,
  PlayerCountChart,
  SessionsByDayChart,
  QUERY_KEYS,
  Result,
} from '../models';

import {
  deleteGame as deleteGameCall,
  getChart,
  getGame,
  getGameStatistics,
  getTopPlayers,
} from './services/gameService';

interface ReturnProps {
  game: Game | undefined;
  isPending: boolean;
  isError: boolean;
  statistics: GameStatistics | undefined;
  topPlayers: TopPlayer[] | undefined;
  deleteGame: () => Promise<void>;
  sessionsByDayChart: SessionsByDayChart[] | undefined;
  playerCountChart: PlayerCountChart[] | undefined;
  playerScoringChart: PlayerScoring[] | undefined;
  scoreRankChart: ScoreRank[] | undefined;
}

export const useGame = (id: string | undefined): ReturnProps => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const { data, isPending, isError } = useQuery<Result<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined,
  });

  const { data: statistics } = useQuery<Result<GameStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({ signal }) => getGameStatistics(id!, signal),
    enabled: id !== undefined,
  });

  const { data: topPlayers } = useQuery<Result<TopPlayer[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameTopPlayers],
    queryFn: ({ signal }) => getTopPlayers(id!, signal),
    enabled: id !== undefined,
  });

  const { data: chartByDay } = useQuery<Result<SessionsByDayChart[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameSessionsByDayChart],
    queryFn: ({ signal }) => getChart<SessionsByDayChart>(id!, GAME_CHARTS.sessionsByDay, signal),
    enabled: id !== undefined,
  });

  const { data: chartPlayerCount } = useQuery<Result<PlayerCountChart[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlayerCountChart],
    queryFn: ({ signal }) => getChart<PlayerCountChart>(id!, GAME_CHARTS.playerCount, signal),
    enabled: id !== undefined,
  });

  const { data: playerScoringChart } = useQuery<Result<PlayerScoring[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlayerScoringChart],
    queryFn: ({ signal }) => getChart<PlayerScoring>(id!, GAME_CHARTS.playerScoring, signal),
    enabled: id !== undefined && (data?.model.hasScoring ?? false),
  });

  const { data: scoreRankChart } = useQuery<Result<ScoreRank[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameScoreRankChart],
    queryFn: ({ signal }) => getChart<ScoreRank>(id!, GAME_CHARTS.scoreRank, signal),
    enabled: id !== undefined && (data?.model.hasScoring ?? false),
  });

  const deleteGame = async () => {
    if (data?.model !== undefined) {
      try {
        await deleteGameCall(data?.model.id);
        showInfoToast('game.delete.successfull');
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      } catch {
        showErrorToast('game.delete.failed');
      }
    }
  };

  return {
    game: data?.model,
    isPending,
    isError,
    statistics: statistics?.model,
    topPlayers: topPlayers?.model,
    deleteGame,
    sessionsByDayChart: chartByDay?.model,
    playerCountChart: chartPlayerCount?.model,
    playerScoringChart: playerScoringChart?.model,
    scoreRankChart: scoreRankChart?.model,
  };
};
