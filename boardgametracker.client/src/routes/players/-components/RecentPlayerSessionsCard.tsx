import { useTranslation } from 'react-i18next';
import { useCallback } from 'react';

import { PlayerSessionCardItem } from './PlayerSessionCardItem';

import { useGameById } from '@/routes/-hooks/useGameById';
import { Session } from '@/models/Session/Session';
import { RecentActivityCard } from '@/components/BgtCard/RecentActivityCard';
import Calendar from '@/assets/icons/calendar.svg?react';

interface Props {
  sessions: Session[];
  dateFormat: string;
  uiLanguage: string;
  playerId: number;
}

export const RecentPlayerSessionsCard = (props: Props) => {
  const { sessions, dateFormat, uiLanguage, playerId } = props;
  const { t } = useTranslation();

  const { gameById } = useGameById();

  const renderSession = useCallback(
    (session: Session) => (
      <PlayerSessionCardItem
        session={session}
        game={gameById(session.gameId) ?? undefined}
        dateFormat={dateFormat}
        uiLanguage={uiLanguage}
        playerId={playerId}
      />
    ),
    [dateFormat, uiLanguage, playerId, gameById]
  );

  const getSessionKey = useCallback((session: Session) => session.id, []);

  return (
    <RecentActivityCard
      items={sessions}
      renderItem={renderSession}
      title={t('game.titles.recent-sessions')}
      viewAllRoute={`/players/${playerId}/sessions`}
      viewAllText={t('game.sessions')}
      icon={Calendar}
      getKey={getSessionKey}
    />
  );
};
