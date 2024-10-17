import { AxiosError } from 'axios';
import { useQuery, useQueryClient, UseQueryResult } from '@tanstack/react-query';

import { useToast } from '@/providers/BgtToastProvider';
import { TopPlayer } from '@/models/Games/TopPlayer';
import { FailResult, Game, GameStatistics, QUERY_KEYS, Result } from '@/models';
import { getGame, getGameStatistics, getTopPlayers, deleteGame as deleteGameCall } from '@/hooks/services/gameService';

interface Props {
  topPlayers: UseQueryResult<Result<TopPlayer[]>, AxiosError<FailResult>>;
  statistics: UseQueryResult<Result<GameStatistics>, AxiosError<FailResult>>;
  deleteGame: () => Promise<void>;
  game: UseQueryResult<Result<Game>, AxiosError<FailResult>>;
}

export const useGameDetailPage = (id: string | undefined): Props => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const game = useQuery<Result<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined,
  });

  const statistics = useQuery<Result<GameStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({ signal }) => getGameStatistics(id!, signal),
    enabled: id !== undefined,
  });

  const topPlayers = useQuery<Result<TopPlayer[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameTopPlayers],
    queryFn: ({ signal }) => getTopPlayers(id!, signal),
    enabled: id !== undefined,
  });

  const deleteGame = async () => {
    if (id !== undefined) {
      try {
        await deleteGameCall(id);
        showInfoToast('game.delete.successfull');
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      } catch {
        showErrorToast('game.delete.failed');
      }
    }
  };

  return {
    game,
    topPlayers,
    statistics,
    deleteGame,
  };
};
