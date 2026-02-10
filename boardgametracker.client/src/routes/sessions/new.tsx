import { useTranslation } from 'react-i18next';
import { addMinutes } from 'date-fns';
import { createFileRoute, useNavigate } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';

import { useToasts } from '../-hooks/useToasts';

import { useNewSessionData } from './-hooks/useNewSessionData';
import { SessionForm } from './-components/SessionForm';

import { getGames } from '@/services/queries/games';
import { CreateSession } from '@/models';
import { BgtEmptyPage } from '@/components/BgtLayout/BgtEmptyPage';
import Game from '@/assets/icons/gamepad.svg?react';

export const Route = createFileRoute('/sessions/new')({
  component: RouteComponent,
});

function RouteComponent() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { successToast, errorToast } = useToasts();

  const { data: games } = useQuery(getGames());

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

  if (games !== undefined && games.length === 0) {
    return (
      <BgtEmptyPage
        header={t('player-session.title-new')}
        icon={Game}
        title={t('dashboard.empty.title')}
        description={t('dashboard.empty.description')}
        action={{
          label: t('dashboard.empty.button'),
          onClick: () => navigate({ to: '/games' }),
        }}
      />
    );
  }

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
