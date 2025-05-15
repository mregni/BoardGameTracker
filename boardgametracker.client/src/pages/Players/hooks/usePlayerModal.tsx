import { AxiosError } from 'axios';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Player, FailResult, QUERY_KEYS } from '@/models';
import { addPlayer, updatePlayer } from '@/hooks/services/playerService';

interface Props {
  onSuccess?: () => void;
  onUpdateSuccess?: () => void;
}

export const usePlayerModal = ({ onSuccess, onUpdateSuccess }: Props) => {
  const queryClient = useQueryClient();

  const { mutateAsync: save, isPending } = useMutation<Player, AxiosError<FailResult>, Player>({
    mutationFn: addPlayer,
    async onSuccess() {
      onSuccess?.();
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.players],
      });
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.counts],
      });
    },
  });

  const { mutateAsync: update, isPending: updateIsPending } = useMutation<Player, AxiosError<FailResult>, Player>({
    mutationFn: updatePlayer,
    async onSuccess() {
      onUpdateSuccess?.();
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.players],
      });
    },
  });

  return {
    save,
    isPending,
    update,
    updateIsPending,
  };
};
