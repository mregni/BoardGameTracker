import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';

import { useUpdateGame } from './-hooks/useUpdateGame';
import { GameForm } from './-components/GameForm';

import { gameIdParamSchema } from '@/utils/routeSchemas';
import { getSettings } from '@/services/queries/settings';
import { Game, CreateGame } from '@/models';

export const Route = createFileRoute('/games/$gameId_/update')({
  component: RouteComponent,
  params: gameIdParamSchema,
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
    const updatedGame: Game = {
      ...game,
      ...data,
      image: data.image ?? game.image,
      additionDate: data.additionDate ? data.additionDate.toISOString() : null,
    };
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
