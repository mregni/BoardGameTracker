import { useTranslation } from 'react-i18next';
import { addMinutes } from 'date-fns';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';

import { useNewSessionWithGameData } from './-hooks/useNewSessionWithGameData';
import { SessionForm } from './-components/SessionForm';

import { gameIdParamSchema } from '@/utils/routeSchemas';
import { getPlayers } from '@/services/queries/players';
import { getLocations } from '@/services/queries/locations';
import { getGame, getGames } from '@/services/queries/games';
import { CreateSession } from '@/models';

export const Route = createFileRoute('/sessions/new_/$gameId')({
  component: RouteComponent,
  params: gameIdParamSchema,
  loader: async ({ params, context: { queryClient } }) => {
    queryClient.prefetchQuery(getGame(params.gameId));
    queryClient.prefetchQuery(getGames());
    queryClient.prefetchQuery(getLocations());
    queryClient.prefetchQuery(getPlayers());
  },
});

function RouteComponent() {
  const { gameId } = Route.useParams();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { successToast, errorToast } = useToasts();

  const onSaveSuccess = () => {
    successToast('player-session.new.notifications.created');
  };

  const onSaveError = () => {
    errorToast('player-session.new.notifications.create-failed');
  };

  const { game, isLoading, isPending, saveSession } = useNewSessionWithGameData({ gameId, onSaveSuccess, onSaveError });

  const save = async (data: CreateSession) => {
    const result = await saveSession(data);
    navigate({ to: `/games/${result.gameId}` });
  };

  if (isLoading || game === undefined) return null;

  return (
    <SessionForm
      game={game}
      minutes={game.maxPlayTime ?? 30}
      start={addMinutes(new Date(), -(game?.maxPlayTime ?? 30))}
      buttonText={t('player-session.save-new')}
      onClick={save}
      disabled={isLoading || isPending}
      title={t('player-session.title-new')}
    />
  );
}
