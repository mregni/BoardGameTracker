import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';

import { useUpdateGame } from './-hooks/useUpdateGame';
import { GameForm } from './-components/GameForm';

import { getSettings } from '@/services/queries/settings';
import { Game, CreateGame } from '@/models';

export const Route = createFileRoute('/games/$gameId_/update')({
  component: RouteComponent,
  loader: async ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
  },
});

function RouteComponent() {
  const { gameId } = Route.useParams();
  const { successToast, errorToast } = useToasts();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const onUpdateSuccess = () => {
    successToast('game.notifications.updated');
    navigate({ to: `/games/${gameId}` });
    window.scrollTo(0, 0);
  };

  const onUpdateError = () => {
    errorToast('game.notifications.update-failed');
  };

  const { game, updateGame, isLoading } = useUpdateGame({ gameId, onUpdateSuccess, onUpdateError });

  if (game === undefined) return null;

  const save = async (data: CreateGame) => {
    const updatedGame = {
      ...data,
      id: game.id,
      baseGameId: game.baseGameId,
      categories: game.categories,
      expansions: game.expansions,
      mechanics: game.mechanics,
      people: game.people,
      rating: game.rating,
      weight: game.weight,
      type: game.type,
    } as Game;
    const result = await updateGame(updatedGame);

    navigate({ to: `/games/${result.id}` });
  };

  return (
    <GameForm
      game={game}
      buttonText={t('game.update.save')}
      title={t('game.update.title')}
      onClick={save}
      disabled={isLoading}
    />
  );
}
