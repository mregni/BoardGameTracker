import { AxiosError } from 'axios';
import { keepPreviousData, QueryFunction, QueryKey, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { CreatePlay } from '../models/Plays/CreatePlay';
import { FailResult, ListResult, Play, QUERY_KEYS, Result } from '../models';

import { addPlay, deletePlay as deletePlayCall } from './services/playService';
import { getPlays } from './services/playerService';
import { getGamePlays } from './services/gameService';

interface ReturnProps {
  plays: Play[];
  totalCount: number;
  isFetching: boolean;
  deletePlay: (id: number) => Promise<void>;
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
  const { showInfoToast, showErrorToast } = useToast();

  const { data, isFetching, refetch } = useQuery<ListResult<T>, AxiosError<FailResult>>({
    queryKey: queryKey,
    queryFn: queryFn,
    enabled: enabled,
    placeholderData: keepPreviousData,
  });

  const deletePlay = async (id: number) => {
    try {
      await deletePlayCall(id);
      await refetch();
      showInfoToast('playplayer.delete.successfull');
    } catch {
      showErrorToast('playplayer.delete.failed');
    }
  };

  return {
    plays: data !== undefined ? data?.list : ([] as Play[]),
    totalCount: data !== undefined ? data?.count : 0,
    isFetching,
    deletePlay,
  };
};

interface PlayProps {
  save: (play: CreatePlay) => Promise<Result<Play>>;
  isPending: boolean;
}

export const usePlay = (): PlayProps => {
  const queryClient = useQueryClient();
  const { showInfoToast } = useToast();

  const { mutateAsync: save, isPending } = useMutation<Result<Play>, AxiosError<FailResult>, CreatePlay>({
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
    isPending,
  };
};
