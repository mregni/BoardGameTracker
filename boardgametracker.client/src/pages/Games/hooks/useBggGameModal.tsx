import { AxiosError } from 'axios';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { BggSearch, FailResult, Game, QUERY_KEYS } from '@/models';
import { addGameWithBgg } from '@/hooks/services/gameService';

interface Props {
  onSuccess?: (game: Game) => void;
}

export const useBggGameModal = ({ onSuccess }: Props) => {
  const queryClient = useQueryClient();

  const { mutateAsync: save, isPending: isPending } = useMutation<Game, AxiosError<FailResult>, BggSearch>({
    mutationFn: addGameWithBgg,
    async onSuccess(data) {
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });

      onSuccess?.(data);
    },
  });

  return {
    save,
    isPending,
  };
};
