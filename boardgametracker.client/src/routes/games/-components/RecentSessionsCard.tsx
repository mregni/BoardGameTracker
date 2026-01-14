import { useTranslation } from 'react-i18next';
import { useCallback } from 'react';

import { SessionCardItem } from './SessionCardItem';

import { Session } from '@/models/Session/Session';
import { RecentActivityCard } from '@/components/BgtCard/RecentActivityCard';
import Calendar from '@/assets/icons/calendar.svg?react';

interface Props {
  sessions: Session[];
  dateFormat: string;
  uiLanguage: string;
  gameId: string;
}

export const RecentSessionsCard = (props: Props) => {
  const { sessions, dateFormat, uiLanguage, gameId } = props;
  const { t } = useTranslation();

  const renderSession = useCallback(
    (session: Session) => <SessionCardItem session={session} dateFormat={dateFormat} uiLanguage={uiLanguage} />,
    [dateFormat, uiLanguage]
  );

  const getSessionKey = useCallback((session: Session) => session.id, []);

  return (
    <RecentActivityCard
      items={sessions}
      renderItem={renderSession}
      title={t('game.titles.recent-sessions')}
      viewAllRoute={`/games/${gameId}/sessions`}
      viewAllText={t('game.sessions')}
      icon={Calendar}
      getKey={getSessionKey}
    />
  );
};
