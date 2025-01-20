import { AxiosError } from 'axios';
import { useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '@/providers/BgtToastProvider';
import { TopPlayer } from '@/models/Games/TopPlayer';
import { ScoreRank } from '@/models/Games/ScoringRank';
import { FailResult, Game, GAME_CHARTS, GameStatistics, PlayerCountChart, QUERY_KEYS } from '@/models';
import { getGame, getGameStatistics, getTopPlayers, deleteGameCall, getChart } from '@/hooks/services/gameService';

interface Props {
  id: string | undefined;
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const useGame = ({ id, onDeleteError, onDeleteSuccess }: Props) => {
  const queryClient = useQueryClient();
  const { showErrorToast } = useToast();

  const game = useQuery<Game, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined,
  });

  const statistics = useQuery<GameStatistics, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({ signal }) => getGameStatistics(id!, signal),
    enabled: id !== undefined,
  });

  const topPlayers = useQuery<TopPlayer[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameTopPlayers],
    queryFn: ({ signal }) => getTopPlayers(id!, signal),
    enabled: id !== undefined,
  });

  const scoreRankChart = useQuery<ScoreRank[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameScoreRankChart],
    queryFn: ({ signal }) => getChart<ScoreRank>(id!, GAME_CHARTS.scoreRank, signal),
    enabled: id !== undefined && (game.data?.hasScoring ?? false),
  });

  const chartPlayerCount = useQuery<PlayerCountChart[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlayerCountChart],
    queryFn: ({ signal }) => getChart<PlayerCountChart>(id!, GAME_CHARTS.playerCount, signal),
    enabled: id !== undefined,
  });

  // const playerScoringChart = useQuery<PlayerScoring[], AxiosError<FailResult>>({
  //   queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlayerScoringChart],
  //   queryFn: ({ signal }) => getChart<PlayerScoring>(id!, GAME_CHARTS.playerScoring, signal),
  //   enabled: id !== undefined && (data?.hasScoring ?? false),
  // });

  const deleteGame = async () => {
    if (id !== undefined) {
      try {
        await deleteGameCall(id);
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
        onDeleteSuccess?.();
      } catch {
        onDeleteError?.();
      }
    }
  };

  if (game.isError) {
    showErrorToast('game.notifications.not-found');
  }

  return {
    game,
    topPlayers,
    statistics,
    deleteGame,
    scoreRankChart,
    chartPlayerCount,
  };
};
