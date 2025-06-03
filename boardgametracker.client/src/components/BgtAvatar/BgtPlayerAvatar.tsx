import { useNavigate } from 'react-router-dom';

import { StringToHsl } from '../../utils/stringUtils';
import { Game, PlayerSession } from '../../models';
import { usePlayerById } from '../../hooks/usePlayerById';

import { BgtAvatar } from './BgtAvatar';

interface AvatarProps {
  playerSession: PlayerSession;
  game: Game | undefined;
}

export const BgtPlayerAvatar = (props: AvatarProps) => {
  const { playerSession, game } = props;
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  const player = playerById(playerSession.playerId);

  if (player === null || game === undefined) return null;

  return (
    <BgtAvatar
      key={`${playerSession.playerId}_${playerSession.sessionId}`}
      title={`${player.name}${game.hasScoring ? ` (${playerSession.score})` : ''}`}
      image={player.image}
      onClick={() => navigate(`/players/${player.id}`)}
      color={StringToHsl(player.name)}
    />
  );
};
