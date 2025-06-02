import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { useGames } from './hooks/useGames';
import { GameForm } from './components/GameForm';

import { CreateGame } from '@/models/Games/CreateGame';
import { Game } from '@/models';
import { useToasts } from '@/hooks/useToasts';

export const CreateGamePage = () => {
  const { errorToast, successToast } = useToasts();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const onSuccess = (game: Game) => {
    successToast('game.notifications.created');
    navigate(`/games/${game.id}`);
    window.scrollTo(0, 0);
  };

  const onError = () => {
    errorToast('game.notifications.failed');
  };

  const { saveGame, saveIsPending } = useGames({ onSuccess, onError });
  const save = async (game: CreateGame) => {
    const result = await saveGame(game);
    navigate(`/games/${result.id}`);
  };

  return (
    <GameForm
      buttonText={t('game.new.save')}
      title={t('game.new.manual.title')}
      onClick={save}
      disabled={saveIsPending}
    />
  );
};
