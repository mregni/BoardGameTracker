import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { useUpdateSessionPage } from './hooks/useUpdateSessionPage';
import { SessionForm } from './components/SessionForm';

import { CreateSession } from '@/models/Session/CreateSession';
import { Session } from '@/models';
import { useToasts } from '@/hooks/useToasts';

export const UpdateSessionPage = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { successToast } = useToasts();

  const onSessionSaveSuccess = () => {
    successToast('player-session.new.notifications.created');
  };

  const { session, updateSession } = useUpdateSessionPage({ id, onSessionSaveSuccess });

  if (id === undefined) {
    throw Error('Session ID is undefined');
  }

  if (session === undefined) return null;

  const save = async (data: CreateSession) => {
    const updatedSession = { ...data } as Session;
    updatedSession.id = session.id;
    const result = await updateSession.mutateAsync(updatedSession);
    navigate(`/games/${result.gameId}/sessions`);
  };

  return (
    <SessionForm
      gameId={session.gameId}
      minutes={session.minutes}
      start={session.start}
      comment={session.comment}
      locationId={session.locationId}
      playerSessions={session.playerSessions}
      buttonText={t('player-session.save-update')}
      onClick={save}
      disabled={updateSession.isPending}
      title={t('player-session.title-update')}
    />
  );
};
