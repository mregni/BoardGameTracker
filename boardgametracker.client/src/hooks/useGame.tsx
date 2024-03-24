import {AxiosError} from 'axios';

import {useQuery} from '@tanstack/react-query';

import {FailResult, Game, GameStatistics, Play, QUERY_KEYS, Result} from '../models';
import {TopPlayer} from '../models/Games/TopPlayer';
import {getGame, getGamePlays, getGameStatistics, getTopPlayers} from './services/gameService';

interface ReturnProps {
  game: Game | undefined;
  isPending: boolean;
  isError: boolean;
  statistics: GameStatistics | undefined;
  plays: Play[] | undefined;
  topPlayers: TopPlayer[] | undefined;
}

export const useGame = (id: string | undefined): ReturnProps => {
  const { data, isPending, isError } = useQuery<Result<Game>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id],
    queryFn: ({ signal }) => getGame(id!, signal),
    enabled: id !== undefined
  });

  const { data: statistics } = useQuery<Result<GameStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({signal}) => getGameStatistics(id!, signal),
    enabled: id !== undefined
  });

  const { data: plays } = useQuery<Result<Play[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gamePlays],
    queryFn: ({signal}) => getGamePlays(id!, signal),
    enabled: id !== undefined
  });

  const { data: topPlayers } = useQuery<Result<TopPlayer[]>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameTopPlayers],
    queryFn: ({signal}) => getTopPlayers(id!, signal),
    enabled: id !== undefined
  });

  return {
    game: data?.model,
    isPending,
    isError,
    statistics: statistics?.model,
    plays: plays?.model,
    topPlayers: topPlayers?.model
  }
}
