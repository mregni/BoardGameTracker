import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { addMinutes } from 'date-fns';

import { useGame } from '../Games/hooks/useGame';

import { useCreateSessionPage } from './hooks/useCreateSessionPage';
import { SessionForm } from './components/SessionForm';

import { CreateSession } from '@/models/Session/CreateSession';
import { useToasts } from '@/hooks/useToasts';

export const CreateSessionPage = () => {
  const { gameId } = useParams();
  const { game } = useGame({ id: gameId });
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { successToast } = useToasts();

  const onSessionSaveSuccess = () => {
    successToast('player-session.new.notifications.created');
  };

  const { saveSession } = useCreateSessionPage({
    onSessionSaveSuccess,
  });

  const save = async (data: CreateSession) => {
    const result = await saveSession.mutateAsync(data);
    navigate(`/games/${result.gameId}`);
  };

  return (
    <SessionForm
      gameId={gameId}
      minutes={game.data?.maxPlayTime ?? 30}
      start={addMinutes(new Date(), -(game.data?.maxPlayTime ?? 30))}
      buttonText={t('player-session.save')}
      onClick={save}
      disabled={saveSession.isPending}
      title={t('player-session.title-new')}
    />
  );
};
