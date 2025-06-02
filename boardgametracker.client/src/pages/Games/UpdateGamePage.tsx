import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { useUpdateGame } from './hooks/useUpdateGame';
import { GameForm } from './components/GameForm';

import { CreateGame } from '@/models/Games/CreateGame';
import { Game } from '@/models';
import { useToasts } from '@/hooks/useToasts';

export const UpdateGamePage = () => {
  const { id } = useParams();
  const { successToast } = useToasts();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const onSuccess = (game: Game) => {
    successToast('game.notifications.updated');
    navigate(`/games/${game.id}`);
    window.scrollTo(0, 0);
  };

  if (id === undefined) {
    throw Error('Session ID is undefined');
  }
  const { game, updateGame, updateIsPending } = useUpdateGame({ id, onGameUpdateSuccess: onSuccess });

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

    navigate(`/games/${result.id}`);
  };

  return (
    <GameForm
      game={game}
      buttonText={t('game.update.save')}
      title={t('game.update.title')}
      onClick={save}
      disabled={updateIsPending}
    />
  );
};
