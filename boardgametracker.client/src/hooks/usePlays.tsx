import { AxiosError } from 'axios';
import { keepPreviousData, QueryFunction, QueryKey, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { CreatePlay } from '../models/Plays/CreatePlay';
import { FailResult, ListResult, Play, QUERY_KEYS, Result } from '../models';

import { addPlay } from './services/playService';
import { getPlays } from './services/playerService';
import { getGamePlays } from './services/gameService';

interface ReturnProps {
  plays: Play[];
  totalCount: number;
  isFetching: boolean;
}

export const usePlayerPlays = (playerId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return usePlays({
    enabled: playerId !== undefined,
    queryFn: ({ signal }) => getPlays(playerId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.playerPlays, page],
  });
};

export const useGamePlays = (gameId: string | undefined, page: number, pageCount: number): ReturnProps => {
  return usePlays({
    enabled: gameId !== undefined,
    queryFn: ({ signal }) => getGamePlays(gameId!, page, pageCount, signal),
    queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.gamePlays, page],
  });
};

interface Props<T> {
  queryKey: QueryKey;
  queryFn: QueryFunction<ListResult<T>, QueryKey, never>;
  enabled: boolean;
}

const usePlays = <T,>(props: Props<T>) => {
  const { queryKey, queryFn, enabled } = props;

  const { data, isFetching } = useQuery<ListResult<T>, AxiosError<FailResult>>({
    queryKey: queryKey,
    queryFn: queryFn,
    enabled: enabled,
    placeholderData: keepPreviousData,
  });

  return {
    plays: data !== undefined ? data?.list : ([] as Play[]),
    totalCount: data !== undefined ? data?.count : 0,
    isFetching,
  };
};

interface CreateProps {
  save: (play: CreatePlay) => Promise<Result<Play>>;
}

export const useCreatePlays = (): CreateProps => {
  const queryClient = useQueryClient();
  const { showInfoToast } = useToast();

  const { mutateAsync: save } = useMutation<Result<Play>, AxiosError<FailResult>, CreatePlay>({
    mutationFn: addPlay,
    async onSuccess(playResult) {
      showInfoToast('playplayer.new.notifications.created');
      const maps = playResult.model.players.map(async (x) => {
        return await queryClient.invalidateQueries({
          queryKey: [QUERY_KEYS.players, x.id, QUERY_KEYS.playerPlays],
        });
      });

      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, playResult.model.gameId],
      });

      await Promise.all(maps);
    },
  });

  return {
    save,
  };
};
