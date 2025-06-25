import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';

import { useUpdateSessionData } from './-hooks/useUpdateSessionData';
import { SessionForm } from './-components/SessionForm';

import { getSession } from '@/services/queries/sessions';
import { getGame } from '@/services/queries/games';
import { Session, CreateSession } from '@/models';

export const Route = createFileRoute('/sessions/update_/$sessionId')({
  component: RouteComponent,
  loader: async ({ params, context: { queryClient } }) => {
    const data = await queryClient.fetchQuery(getSession(params.sessionId));
    queryClient.prefetchQuery(getGame(data.gameId));
  },
});

function RouteComponent() {
  const { sessionId } = Route.useParams();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { successToast, errorToast } = useToasts();

  const onSaveSuccess = () => {
    successToast('player-session.update.notifications.updated');
  };

  const onSaveError = () => {
    errorToast('player-session.update.notifications.update-failed');
  };

  const { game, session, updateSession, isPending } = useUpdateSessionData({ sessionId, onSaveSuccess, onSaveError });

  if (session === undefined) return null;

  const save = async (data: CreateSession) => {
    const updatedSession = { ...data } as Session;
    updatedSession.id = session.id;
    const result = await updateSession(updatedSession);
    navigate({ to: `/games/${result.gameId}/sessions` });
  };

  if (game === undefined || session === undefined) return null;

  return (
    <SessionForm
      game={game}
      minutes={session.minutes}
      start={session.start}
      comment={session.comment}
      locationId={session.locationId}
      playerSessions={session.playerSessions}
      expansions={session.expansions}
      buttonText={t('player-session.save-update')}
      onClick={save}
      disabled={isPending}
      title={t('player-session.title-update')}
    />
  );
}
