import { AxiosError } from 'axios';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { FailResult, ListResult, Player, QUERY_KEYS, Result } from '../models';
import { useToast } from '../providers/BgtToastProvider';
import { addPlayer, getPlayers } from './services/playerService';

export interface Props {
  players: Player[] | undefined;
  isPending: boolean;
  isError: boolean;
  save: (player: Player) => Promise<Result<Player>>;
  byId: (id: number | string | undefined) => Player | null;
}

export const usePlayers = (): Props => {
  const queryClient = useQueryClient();
  const { showInfoToast } = useToast();

  const { mutateAsync: save } = useMutation<Result<Player>, AxiosError<FailResult>, Player>({
    mutationFn: addPlayer,
    async onSuccess() {
      showInfoToast('player.notifications.created');
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.players],
      });
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.counts],
      });
    },
  });

  const { data, isError, isPending } = useQuery<ListResult<Player>>({
    queryKey: [QUERY_KEYS.players],
    queryFn: ({ signal }) => getPlayers(signal),
  });

  const byId = (id: number | string | undefined): Player | null => {
    if (data === undefined || id === undefined) return null;

    const index = data.list.findIndex((x) => x.id === +id);
    if (index !== -1) {
      return data.list[index];
    }

    return null;
  };

  return {
    players: data?.list,
    isPending,
    isError,
    save,
    byId,
  };
};
