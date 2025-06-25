import { useTranslation } from 'react-i18next';
import { addMinutes } from 'date-fns';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';
import { useNewSessionData } from './-hooks/useNewSessionData';

import { SessionForm } from './-components/SessionForm';

import { CreateSession } from '@/models';

export const Route = createFileRoute('/sessions/new')({
  component: RouteComponent,
});

function RouteComponent() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { successToast, errorToast } = useToasts();

  const onSaveSuccess = () => {
    successToast('player-session.new.notifications.created');
  };

  const onSaveError = () => {
    errorToast('player-session.new.notifications.create-failed');
  };

  const { isPending, saveSession } = useNewSessionData({ onSaveSuccess, onSaveError });

  const save = async (data: CreateSession) => {
    const result = await saveSession(data);
    navigate({ to: `/games/${result.gameId}` });
  };

  return (
    <SessionForm
      start={addMinutes(new Date(), -30)}
      buttonText={t('player-session.save-new')}
      onClick={save}
      disabled={isPending}
      title={t('player-session.title-new')}
    />
  );
}
