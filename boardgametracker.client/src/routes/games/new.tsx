import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useNewGame } from './-hooks/useNewGame';
import { GameForm } from './-components/GameForm';

import { getSettings } from '@/services/queries/settings';
import { CreateGame } from '@/models/Games/CreateGame';
import { Game } from '@/models';
import { useToasts } from '../-hooks/useToasts';

export const Route = createFileRoute('/games/new')({
  component: RouteComponent,
  loader: async ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
  },
});

function RouteComponent() {
  const { errorToast, successToast } = useToasts();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const onSaveSuccess = (game: Game) => {
    successToast('game.notifications.created');
    navigate({ to: `/games/${game.id}` });
    window.scrollTo(0, 0);
  };

  const onSaveError = () => {
    errorToast('game.notifications.create-failed');
  };

  const { saveGame, isLoading } = useNewGame({ onSaveSuccess, onSaveError });
  const save = async (game: CreateGame) => {
    const result = await saveGame(game);
    navigate({ to: `/games/${result.id}` });
  };

  return (
    <GameForm buttonText={t('game.new.save')} title={t('game.new.manual.title')} onClick={save} disabled={isLoading} />
  );
}
