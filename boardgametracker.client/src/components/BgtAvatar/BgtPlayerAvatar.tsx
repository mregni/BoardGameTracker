import { useNavigate } from 'react-router-dom';

import { StringToHsl } from '../../utils/stringUtils';
import { PlayPlayer } from '../../models';
import { usePlayers } from '../../hooks/usePlayers';

import { BgtAvatar } from './BgtAvatar';

interface Props {
  player: PlayPlayer;
}

export const BgtPlayerAvatar = (props: Props) => {
  const { player } = props;
  const { byId: playerById } = usePlayers();
  const navigate = useNavigate();

  return (
    <BgtAvatar
      onClick={() => navigate(`/players/${player.playerId}`)}
      title={playerById(player.playerId)?.name + (player.score ? `: ${player.score}` : '')}
      image={playerById(player.playerId)?.image}
      color={StringToHsl(playerById(player.playerId)?.name)}
    />
  );
};
