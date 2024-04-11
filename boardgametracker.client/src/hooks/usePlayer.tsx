import {AxiosError} from 'axios';

import {useQuery, useQueryClient} from '@tanstack/react-query';

import {FailResult, Player, QUERY_KEYS, Result} from '../models';
import {PlayerStatistics} from '../models/Player/PlayerStatistics';
import {useToast} from '../providers/BgtToastProvider';
import {
  deletePlayer as deletePlayerCall, getPlayer, getPlayerStatistics,
} from './services/playerService';

interface ReturnProps {
  player: Player | undefined;
  statistics: PlayerStatistics | undefined;
  deletePlayer: () => Promise<void>;
}

export const usePlayer = (id: string | undefined): ReturnProps => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();
  
  const { data } = useQuery<Result<Player>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.player, id],
    queryFn: ({ signal }) => getPlayer(id!, signal),
    enabled: id !== undefined
  });

  const { data: statistics } = useQuery<Result<PlayerStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.game, id, QUERY_KEYS.gameStatistics],
    queryFn: ({ signal }) => getPlayerStatistics(id!, signal),
    enabled: id !== undefined
  });

  const deletePlayer = async () => {
    if (data?.model !== undefined) {
      try {
        await deletePlayerCall(data?.model.id);
        showInfoToast("player.delete.successfull");
        await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      }
      catch {
        showErrorToast("player.delete.failed");
      }
    }
  }

  return {
    player: data?.model,
    statistics: statistics?.model,
    deletePlayer
  }
}
