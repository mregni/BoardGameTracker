import { useTranslation } from 'react-i18next';
import { memo, useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { toDisplay } from '@/utils/dateUtils';
import { Session, Game } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props {
  session: Session;
  game?: Game;
  dateFormat: string;
  uiLanguage: string;
  playerId: number;
}

const PlayerSessionCardItemComponent = (props: Props) => {
  const { session, game, dateFormat, uiLanguage, playerId } = props;
  const navigate = useNavigate();
  const { t } = useTranslation();

  const playerSession = useMemo(() => {
    const playerSessions = session.playerSessions.filter((ps) => ps.playerId === playerId);
    if (playerSessions.length === 0) return null;
    return playerSessions.length === 0 ? null : playerSessions[0];
  }, [playerId, session.playerSessions]);

  return (
    <BgtCard>
      <div className="flex items-center gap-4">
        <div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
          {game && (
            <BgtAvatar
              onClick={() => navigate({ to: `/games/${game.id}` })}
              image={game.image}
              title={game.title}
              color={StringToHsl(game.title)}
              size="large"
            />
          )}
        </div>
        <div className="flex-1">
          <BgtText color="white">{game?.title}</BgtText>
          <div className="text-white/50 text-sm">{toDisplay(session.start, dateFormat, uiLanguage)}</div>
        </div>
        <div className="text-right">
          <BgtText color={playerSession?.won ? 'green' : 'red'} weight="bold">
            {playerSession?.won ? t('common.won') : t('common.lost')}
          </BgtText>

          {playerSession?.score && (
            <BgtText color="cyan" weight="bold">
              {t('common.points', { count: playerSession?.score })}
            </BgtText>
          )}
        </div>
      </div>
    </BgtCard>
  );
};

PlayerSessionCardItemComponent.displayName = 'PlayerSessionCardItem';

export const PlayerSessionCardItem = memo(PlayerSessionCardItemComponent);
