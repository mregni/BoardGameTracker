import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '../../utils/stringUtils';
import { Game, Player, PlayerSession } from '../../models';

import { BgtAvatar } from './BgtAvatar';

interface AvatarProps {
  playerSession: PlayerSession;
  game: Game | undefined;
  player: Player | undefined;
}

export const BgtPlayerAvatar = (props: AvatarProps) => {
  const { playerSession, game, player } = props;
  const navigate = useNavigate();

  if (player === undefined || game === undefined) return null;

  return (
    <BgtAvatar
      key={`${playerSession.playerId}_${playerSession.sessionId}`}
      title={`${player.name}${game.hasScoring ? ` (${playerSession.score})` : ''}`}
      image={player.image}
      onClick={() => navigate({ to: `/players/${player.id}` })}
      color={StringToHsl(player.name)}
    />
  );
};
