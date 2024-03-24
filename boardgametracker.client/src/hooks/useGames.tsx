import {AxiosError} from 'axios';
import {useNavigate} from 'react-router-dom';

import {useMutation, useQuery, useQueryClient} from '@tanstack/react-query';

import {BggSearch, FailResult, Game, ListResult, QUERY_KEYS, Result, ResultState} from '../models';
import {useToast} from '../providers/BgtToastProvider';
import {addGameWithBgg, getGames} from './services/gameService';

export interface Props {
  games: Game[] | undefined;
  isPending: boolean;
  isError: boolean;
  save: (search: BggSearch) => Promise<Result<Game>>;
  saveIsPending: boolean;
}

export const useGames = (): Props => {
  const { showInfoToast, showWarningToast } = useToast();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const { data, isError, isPending } = useQuery<ListResult<Game>>({
    queryKey: [QUERY_KEYS.games],
    queryFn: ({ signal }) => getGames(signal)
  });

  const { mutateAsync: save, isPending: saveIsPending } = useMutation<Result<Game>, AxiosError<FailResult>, BggSearch>({
    mutationFn: addGameWithBgg,
    async onSuccess(data) {
      if (data.state === ResultState.Duplicate) {
        showWarningToast("game.notifications.duplicate");
      } else {
        showInfoToast("game.notifications.created");
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] });
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      }

      navigate(`/games/${data.model?.id}`);
    },
  });

  return {
    games: data?.list,
    isPending,
    isError,
    save,
    saveIsPending
  }
}

