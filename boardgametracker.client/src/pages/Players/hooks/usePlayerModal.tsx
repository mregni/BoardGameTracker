import { AxiosError } from 'axios';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { Player, FailResult, QUERY_KEYS } from '@/models';
import { addPlayer } from '@/hooks/services/playerService';

interface Props {
  onSuccess: () => void;
}

export const usePlayerModal = ({ onSuccess }: Props) => {
  const queryClient = useQueryClient();

  const { mutateAsync, isPending } = useMutation<Player, AxiosError<FailResult>, Player>({
    mutationFn: addPlayer,
    async onSuccess() {
      onSuccess();
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.players],
      });
      await queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.counts],
      });
    },
  });

  return {
    save: mutateAsync,
    isPending,
  };
};
