import { useNavigate } from 'react-router-dom';

import { StringToHsl } from '../../utils/stringUtils';
import { PlayerSession } from '../../models';
import { usePlayerById } from '../../hooks/usePlayerById';

import { BgtAvatar } from './BgtAvatar';

interface Props {
  playerSession: PlayerSession;
}

export const BgtPlayerAvatar = (props: Props) => {
  const { playerSession } = props;
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  return (
    <BgtAvatar
      onClick={() => navigate(`/players/${playerSession.playerId}`)}
      title={playerById(playerSession.playerId)?.name + (playerSession.score ? `: ${playerSession.score}` : '')}
      image={playerById(playerSession.playerId)?.image}
      color={StringToHsl(playerById(playerSession.playerId)?.name)}
    />
  );
};
