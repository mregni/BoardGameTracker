import { useNavigate, useParams } from 'react-router-dom';
import { Bars } from 'react-loading-icons';
import { useFieldArray, useForm } from 'react-hook-form';
import { useEffect, useState } from 'react';
import { t } from 'i18next';
import { addMinutes } from 'date-fns';
import { zodResolver } from '@hookform/resolvers/zod';

import { UpdatePlayerModal } from '../Players/modals/UpdatePlayerModal';
import { CreatePlayerModal } from '../Players/modals/CreatePlayerModal';

import { useCreateSessionPage } from './hooks/useCreateSessionPage';

import { useToast } from '@/providers/BgtToastProvider';
import {
  CreateSession,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreateSessionSchema,
} from '@/models/Session/CreateSession';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtTextArea } from '@/components/BgtForm/BgtTextArea';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtPlayerSelector } from '@/components/BgtForm/BgtPlayerSelector';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtComboBox } from '@/components/BgtForm/BgtComboBox';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

export const CreateSessionPage = () => {
  const { gameId } = useParams();
  const navigate = useNavigate();
  const { showInfoToast, showErrorToast } = useToast();

  const onSessionSaveSuccess = () => {
    showInfoToast('player-session.new.notifications.created');
  };

  const onLocationSaveSuccess = () => {
    showInfoToast('location.notifications.created');
  };

  const onLocationSaveError = () => {
    showErrorToast('location.notifications.failed');
  };

  const { locations, saveLocation, saveSession, games, game } = useCreateSessionPage({
    gameId,
    onSessionSaveSuccess,
    onLocationSaveSuccess,
    onLocationSaveError,
  });

  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);
  const [openUpdateNewPlayerModal, setOpenUpdateNewPlayerModal] = useState(false);
  const [playerIdToEdit, setPlayerIdToEdit] = useState<string | null>(null);

  const { register, handleSubmit, control, setValue, trigger, watch } = useForm<CreateSession>({
    resolver: zodResolver(CreateSessionSchema),
    defaultValues: {
      gameId: gameId,
      locationId: undefined,
      minutes: game.data?.maxPlayTime ?? 30,
      comment: '',
      start: addMinutes(new Date(), -(game.data?.maxPlayTime ?? 30)),
      playerSessions: [],
    },
  });

  const selectedGameId = watch('gameId');

  useEffect(() => {
    if (selectedGameId !== undefined) {
      const selectedBoardGame = games.data?.find((game) => game.id.toString() === selectedGameId);
      if (selectedBoardGame) {
        setValue('minutes', selectedBoardGame.maxPlayTime ?? 30, { shouldValidate: true });
        setValue('start', addMinutes(new Date(), -(selectedBoardGame?.maxPlayTime ?? 30)));
      }
    }
  }, [games, selectedGameId, setValue]);

  const {
    fields: players,
    append,
    remove,
    update,
  } = useFieldArray<CreateSession>({
    name: 'playerSessions',
    control,
  });

  const closeNewPlayPlayer = (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    setPlayerIdToEdit(null);
    append(player);
    setOpenCreateNewPlayerModal(false);
  };

  const closeUpdatePlayPlayer = (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => {
    const index = players.findIndex((x) => x.playerId === player.playerId);
    if (index !== -1) {
      update(index, player);
    }
  };

  if (locations.data === undefined || games.data === undefined) return null;

  const onSubmit = async (data: CreateSession) => {
    const result = await saveSession.mutateAsync(data);
    navigate(`/games/${result.gameId}`);
  };

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard>
          <BgtHeading size="6">{t('player-session.title')}</BgtHeading>
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <BgtSelect
                label={t('player-session.new.game.label')}
                hasAvatars
                items={
                  games.data.map((x) => ({
                    value: x.id.toString(),
                    label: x.title,
                    image: x.image,
                  })) ?? []
                }
                name="gameId"
                control={control}
                disabled={saveSession.isPending}
                placeholder={t('player-session.new.game.placeholder')}
              />

              <BgtComboBox
                control={control}
                name="locationId"
                options={
                  locations.data.map((x) => ({
                    value: x.id.toString(),
                    label: x.name,
                  })) ?? []
                }
                label={t('player-session.new.location.label')}
                disabled={saveSession.isPending}
                placeholder={t('player-session.new.location.placeholder')}
                addOptionText={(value) => t('player-session.new.location.create-new', { name: value })}
                onChange={(value) => {
                  setValue('locationId', value?.value ?? '');
                  void trigger('locationId');
                }}
                onCreate={async (value) => await saveLocation.mutateAsync({ name: value })}
                isSaving={saveLocation.isPending}
                getSelectedItem={(x) => ({ value: x.id.toString(), label: x.name })}
              />

              <BgtInputField
                valueAsNumber
                name="minutes"
                type="number"
                control={control}
                disabled={saveSession.isPending}
                label={t('player-session.new.duration.label')}
                placeholder={t('player-session.new.duration.placeholder')}
              />

              <BgtInputField
                name="start"
                type="datetime-local"
                control={control}
                disabled={saveSession.isPending}
                label={t('player-session.new.start.label')}
              />

              <BgtPlayerSelector
                name="playerSessions"
                control={control}
                setCreateModalOpen={setOpenCreateNewPlayerModal}
                setUpdateModalOpen={setOpenUpdateNewPlayerModal}
                remove={remove}
                players={players}
                setPlayerIdToEdit={setPlayerIdToEdit}
                disabled={saveSession.isPending}
              />

              <BgtTextArea
                name="comment"
                register={register}
                disabled={saveSession.isPending}
                label={t('player-session.new.comment.label')}
              />

              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="outline"
                  type="button"
                  disabled={saveSession.isPending}
                  className="flex-none"
                  onClick={() => navigate(-1)}
                >
                  {saveSession.isPending && <Bars className="size-4" />}
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" disabled={saveSession.isPending} className="flex-1" variant="soft">
                  {saveSession.isPending && <Bars className="size-4" />}
                  {t('player-session.save')}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>

        <CreatePlayerModal
          open={openCreateNewPlayerModal}
          hasScoring={game.data.hasScoring ?? true}
          onClose={closeNewPlayPlayer}
          onCancel={() => setOpenCreateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
        />
        <UpdatePlayerModal
          open={openUpdateNewPlayerModal}
          hasScoring={game.data.hasScoring ?? true}
          onClose={closeUpdatePlayPlayer}
          onCancel={() => setOpenUpdateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
          playerToEdit={players.find((x) => x.id === playerIdToEdit)}
        />
      </BgtPageContent>
    </BgtPage>
  );
};
