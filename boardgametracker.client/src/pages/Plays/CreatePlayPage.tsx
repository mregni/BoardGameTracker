import { useParams } from 'react-router-dom';
import { useFieldArray, useForm } from 'react-hook-form';
import { useState } from 'react';
import { t } from 'i18next';
import { Button } from '@radix-ui/themes';
import { zodResolver } from '@hookform/resolvers/zod';

import { CreatePlay, CreatePlayPlayer, CreatePlayPlayerNoScoring, CreatePlaySchema } from '../../models/Plays/CreatePlay';
import { useLocations } from '../../hooks/useLocations';
import { useGames } from '../../hooks/useGames';
import { useGame } from '../../hooks/useGame';
import { BgtNewLocationModal } from '../../components/Modals/BgtNewLocationModal';
import { BgtCreatePlayerModal } from '../../components/Modals/BgtCreatePlayerModal';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtTextArea } from '../../components/BgtForm/BgtTextArea';
import { BgtSelectNoLabel } from '../../components/BgtForm/BgtSelectNoLabel';
import { BgtPlayerSelector } from '../../components/BgtForm/BgtPlayerSelector';
import { BgtInputField } from '../../components/BgtForm/BgtInputField';
import { BgtFormRow } from '../../components/BgtForm/BgtFormRow';
import { BgtEmptyFormRow } from '../../components/BgtForm/BgtEmptyFormRow';
import { BgtComboBox } from '../../components/BgtForm/BgtComboBox';

export const CreatePlayPage = () => {
  const { gameId } = useParams();
  const { game } = useGame(gameId);
  const { games } = useGames();
  const { locations, save: saveLocation, isSaving } = useLocations();

  const [openNewLocationModal, setOpenNewLocationModal] = useState(false);
  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    formState: { errors },
  } = useForm<CreatePlay>({
    resolver: zodResolver(CreatePlaySchema),
    defaultValues: {
      gameId: gameId,
      locationId: undefined,
      minutes: game?.maxPlayTime ?? 30,
      comment: '',
      players: [],
    },
    mode: 'onSubmit',
  });

  const {
    fields: players,
    append,
    remove,
  } = useFieldArray<CreatePlay>({
    name: 'players',
    control,
  });

  const closeNewPlayPlayer = (player: CreatePlayPlayer | CreatePlayPlayerNoScoring) => {
    append(player);
    setOpenCreateNewPlayerModal(false);
  };

  if (locations === undefined || games === undefined) return null;

  const onSubmit = (data: CreatePlay) => {
    console.log(data);
  };

  const onError = () => {
    console.log('error');
  };

  return (
    <BgtPage>
      <BgtPageContent className="flex flex-col gap-8">
        <div className="flex justify-center">Create game page header (CHANGE ME)</div>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-col gap-3">
            <BgtFormRow
              title={t('playplayer.new.game.label')}
              subTitle={t('playplayer.new.game.sub-label')}
              right={
                <BgtSelectNoLabel<CreatePlay>
                  items={
                    games?.map((x) => ({
                      value: x.id.toString(),
                      label: x.title,
                      image: x.image,
                    })) ?? []
                  }
                  control={control}
                  name="gameId"
                />
              }
            />
            <BgtFormRow
              title={t('playplayer.new.location.label')}
              subTitle={t('playplayer.new.location.sub-label')}
              right={
                <div className="flex flex-row gap-3">
                  <BgtComboBox
                    control={control}
                    name="locationId"
                    options={
                      locations?.map((x) => ({
                        value: x.id.toString(),
                        label: x.name,
                      })) ?? []
                    }
                    placeholder={''}
                    addOptionText={(value) => t('playplayer.new.location.create-new', { name: value })}
                    onChange={(value) => setValue('locationId', value?.value ?? '')}
                    onCreate={(value) => saveLocation({ name: value })}
                    isSaving={isSaving}
                    getSelectedItem={(x) => ({ value: x.model.id.toString(), label: x.model.name })}
                  />
                </div>
              }
            />
            <BgtFormRow
              title={t('playplayer.new.players.label')}
              subTitle={t('playplayer.new.players.sub-label')}
              right={
                <BgtPlayerSelector name="players" control={control} setModalOpen={setOpenCreateNewPlayerModal} remove={remove} players={players} />
              }
            />
            <BgtFormRow
              title={t('playplayer.new.start.label')}
              subTitle={t('playplayer.new.start.sub-label')}
              right={<BgtInputField name="start" type="date" register={register} className="max-w-64" control={control} />}
            />
            <BgtFormRow
              title={t('playplayer.new.duration.label')}
              subTitle={t('playplayer.new.duration.sub-label')}
              right={<BgtInputField valueAsNumber name="minutes" type="number" register={register} className="max-w-64" control={control} />}
            />
            <BgtFormRow
              title={t('playplayer.new.comment.label')}
              subTitle={t('playplayer.new.comment.sub-label')}
              right={<BgtTextArea name="comment" register={register} disabled={false} className="max-w-80" />}
            />
            <BgtEmptyFormRow right={<Button type="submit">{t('playplayer.save')}</Button>} />
          </div>
        </form>
        <BgtNewLocationModal open={openNewLocationModal} setOpen={setOpenNewLocationModal} />
        {openCreateNewPlayerModal && (
          <BgtCreatePlayerModal
            open={openCreateNewPlayerModal}
            setOpen={setOpenCreateNewPlayerModal}
            hasScoring={game?.hasScoring ?? true}
            onClose={closeNewPlayPlayer}
            selectedPlayerIds={players.map((x) => x.playerId)}
          />
        )}
      </BgtPageContent>
    </BgtPage>
  );
};
