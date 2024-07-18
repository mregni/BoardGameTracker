import { useNavigate, useParams } from 'react-router-dom';
import { Bars } from 'react-loading-icons';
import { useFieldArray, useForm } from 'react-hook-form';
import { useState } from 'react';
import { t } from 'i18next';
import { addMinutes } from 'date-fns';
import { Button } from '@radix-ui/themes';
import { zodResolver } from '@hookform/resolvers/zod';

import {
  CreateSession,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  CreateSessionSchema,
} from '../../models/Session/CreateSession';
import { ResultState } from '../../models';
import { useSession } from '../../hooks/useSessions';
import { useLocations } from '../../hooks/useLocations';
import { useGames } from '../../hooks/useGames';
import { useGame } from '../../hooks/useGame';
import { BgtUpdatePlayerModal } from '../../components/Modals/BgtUpdatePlayerModal';
import { BgtCreatePlayerModal } from '../../components/Modals/BgtCreatePlayerModal';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtHeading } from '../../components/BgtHeading/BgtHeading';
import { BgtTextArea } from '../../components/BgtForm/BgtTextArea';
import { BgtSelectNoLabel } from '../../components/BgtForm/BgtSelectNoLabel';
import { BgtSelect } from '../../components/BgtForm/BgtSelect';
import { BgtPlayerSelector } from '../../components/BgtForm/BgtPlayerSelector';
import { BgtInputField } from '../../components/BgtForm/BgtInputField';
import { BgtFormRow } from '../../components/BgtForm/BgtFormRow';
import { BgtEmptyFormRow } from '../../components/BgtForm/BgtEmptyFormRow';
import { BgtComboBox } from '../../components/BgtForm/BgtComboBox';
import { BgtCenteredCard } from '../../components/BgtCard/BgtCenteredCard';
import { BgtCard } from '../../components/BgtCard/BgtCard';
import BgtButton from '../../components/BgtButton/BgtButton';

export const CreateSessionPage = () => {
  const { gameId } = useParams();
  const { game } = useGame(gameId);
  const { games } = useGames();
  const { locations, save: saveLocation, isSaving } = useLocations();
  const navigate = useNavigate();
  const { save, isPending } = useSession();

  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);
  const [openUpdateNewPlayerModal, setOpenUpdateNewPlayerModal] = useState(false);
  const [playerIdToEdit, setPlayerIdToEdit] = useState<string | null>(null);

  const { register, handleSubmit, control, setValue, trigger } = useForm<CreateSession>({
    resolver: zodResolver(CreateSessionSchema),
    defaultValues: {
      gameId: gameId,
      locationId: undefined,
      minutes: game?.maxPlayTime ?? 30,
      comment: '',
      start: addMinutes(new Date(), -(game?.maxPlayTime ?? 30)),
      playerSessions: [],
    },
  });

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

  if (locations === undefined || games === undefined) return null;

  const onSubmit = async (data: CreateSession) => {
    const result = await save(data);
    if (result.state === ResultState.Success) {
      navigate(`/games/${result.model.gameId}`);
    }
  };

  return (
    <BgtPage>
      <BgtPageContent className="flex flex-col h-full">
        <BgtCenteredCard>
          <BgtHeading size="6" className="uppercase">
            {t('player-session.title')}
          </BgtHeading>
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <BgtSelect
                label={t('player-session.new.game.label')}
                hasAvatars
                items={
                  games?.map((x) => ({
                    value: x.id.toString(),
                    label: x.title,
                    image: x.image,
                  })) ?? []
                }
                name="gameId"
                control={control}
                disabled={isPending}
                placeholder={t('player-session.new.game.placeholder')}
              />

              <BgtComboBox
                control={control}
                name="locationId"
                options={
                  locations?.map((x) => ({
                    value: x.id.toString(),
                    label: x.name,
                  })) ?? []
                }
                label={t('player-session.new.location.label')}
                disabled={isPending}
                placeholder={t('player-session.new.location.placeholder')}
                addOptionText={(value) => t('player-session.new.location.create-new', { name: value })}
                onChange={(value) => {
                  setValue('locationId', value?.value ?? '');
                  void trigger('locationId');
                }}
                onCreate={async (value) => {
                  await saveLocation({ name: value });
                }}
                isSaving={isSaving}
                getSelectedItem={(x) => ({ value: x.model.id.toString(), label: x.model.name })}
              />

              <BgtInputField
                valueAsNumber
                name="minutes"
                type="number"
                control={control}
                disabled={isPending}
                label={t('player-session.new.duration.label')}
                placeholder={t('player-session.new.duration.placeholder')}
              />

              <BgtInputField
                name="start"
                type="datetime-local"
                control={control}
                disabled={isPending}
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
                disabled={isPending}
              />

              <BgtTextArea
                name="comment"
                register={register}
                disabled={isPending}
                label={t('player-session.new.comment.label')}
              />

              <BgtButton type="submit" disabled={isPending} className="flex flex-row gap-2">
                {isPending && <Bars className="size-4" />}
                {t('player-session.save')}
              </BgtButton>
            </div>
          </form>
        </BgtCenteredCard>

        <BgtCreatePlayerModal
          open={openCreateNewPlayerModal}
          hasScoring={game?.hasScoring ?? true}
          onClose={closeNewPlayPlayer}
          onCancel={() => setOpenCreateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
        />
        <BgtUpdatePlayerModal
          open={openUpdateNewPlayerModal}
          hasScoring={game?.hasScoring ?? true}
          onClose={closeUpdatePlayPlayer}
          onCancel={() => setOpenUpdateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
          playerToEdit={players.find((x) => x.id === playerIdToEdit)}
        />
      </BgtPageContent>
    </BgtPage>
  );
};
