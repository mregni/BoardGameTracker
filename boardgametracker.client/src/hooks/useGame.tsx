import {AxiosError} from 'axios';

import {useQuery, useQueryClient} from '@tanstack/react-query';

import {FailResult, Game, GameStatistics, Play, QUERY_KEYS, Result} from '../models';
import {TopPlayer} from '../models/Games/TopPlayer';
import {useToast} from '../providers/BgtToastProvider';
import {
  deleteGame as deleteGameCall, getGame, getGamePlays, getGameStatistics, getTopPlayers,
} from './services/gameService';

interface ReturnProps {
  game: Game | undefined;
  isPending: boolean;
  isError: boolean;
  statistics: GameStatistics | undefined;
  plays: Play[];
  topPlayers: TopPlayer[] | undefined;
  deleteGame: () => Promise<void>;
}

export const useGame = (id: string | undefined): ReturnProps => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const { data, isPending, isError } = useQuery<Result<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined
  });

  const { data: statistics } = useQuery<Result<GameStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({ signal }) => getGameStatistics(id!, signal),
    enabled: id !== undefined
  });

  const { data: plays } = useQuery<Result<Play[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlays],
    queryFn: ({ signal }) => getGamePlays(id!, signal),
    enabled: id !== undefined
  });

  const { data: topPlayers } = useQuery<Result<TopPlayer[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameTopPlayers],
    queryFn: ({ signal }) => getTopPlayers(id!, signal),
    enabled: id !== undefined
  });

  const deleteGame = async () => {
    if (data?.model !== undefined) {
      try {
        await deleteGameCall(data?.model.id);
        showInfoToast("game.delete.successfull");
        //await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, id] });
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      }
      catch {
        showErrorToast("game.delete.failed");
      }
    }
  }

  return {
    game: data?.model,
    isPending,
    isError,
    statistics: statistics?.model,
    plays: plays !== undefined ? plays?.model : [] as Play[],
    topPlayers: topPlayers?.model,
    deleteGame
  }
}
