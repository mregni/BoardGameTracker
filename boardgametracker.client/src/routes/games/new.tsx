import { useTranslation } from 'react-i18next';
import { createFileRoute, useNavigate } from '@tanstack/react-router';

import { useNewGame } from './-hooks/useNewGame';
import { GameForm } from './-components/GameForm';

import { getSettings } from '@/services/queries/settings';
import { CreateGame } from '@/models/Games/CreateGame';
import { Game } from '@/models';

export const Route = createFileRoute('/games/new')({
  component: RouteComponent,
  loader: async ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getSettings());
  },
});

function RouteComponent() {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const onSuccess = (game: Game) => {
    navigate({ to: `/games/${game.id}` });
    window.scrollTo(0, 0);
  };

  const { saveGame, isLoading } = useNewGame({ onSuccess });
  const save = async (game: CreateGame) => {
    const result = await saveGame(game);
    navigate({ to: `/games/${result.id}` });
  };

  return (
    <GameForm buttonText={t('game.new.save')} title={t('game.new.manual.title')} onClick={save} disabled={isLoading} />
  );
}
