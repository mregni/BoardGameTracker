import { Bars } from 'react-loading-icons';
import { useForm, useFieldArray } from 'react-hook-form';
import { useState, useEffect } from 'react';
import { t } from 'i18next';
import { addMinutes } from 'date-fns';
import { useRouter } from '@tanstack/react-router';
import { zodResolver } from '@hookform/resolvers/zod';

import { UpdateSessionPlayerModal } from '../-modals/UpdateSessionPlayerModal';
import { CreateSessionPlayerModal } from '../-modals/CreateSessionPlayerModal';
import { useSessionForm } from '../-hooks/useSessionForm';

import {
  CreateSession,
  CreateSessionSchema,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  Game,
  Expansion,
} from '@/models';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtTextArea } from '@/components/BgtForm/BgtTextArea';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtPlayerSelector } from '@/components/BgtForm/BgtPlayerSelector';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtCheckboxList } from '@/components/BgtForm/BgtCheckboxList';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  game?: Game | undefined;
  locationId?: string | undefined;
  minutes?: number | undefined;
  comment?: string | null;
  start?: Date | undefined;
  expansions?: Expansion[] | undefined;
  playerSessions?: CreateSessionPlayer[] | CreatePlayerSessionNoScoring[] | undefined;
  onClick: (data: CreateSession) => Promise<void>;
  buttonText: string;
  title: string;
  disabled: boolean;
}

export const SessionForm = (props: Props) => {
  const {
    game,
    locationId,
    minutes,
    comment = '',
    start,
    playerSessions = [],
    expansions = [],
    onClick,
    buttonText,
    title,
    disabled,
  } = props;
  const router = useRouter();
  const { locations, games, players: playerList } = useSessionForm();
  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);
  const [openUpdateNewPlayerModal, setOpenUpdateNewPlayerModal] = useState(false);
  const [playerIdToEdit, setPlayerIdToEdit] = useState<string | null>(null);
  const [expansionList, setExpansionList] = useState<Expansion[]>([]);
  const [selectedIds, setSelectedIds] = useState<number[]>(expansions.map((x) => x.id));

  const { register, handleSubmit, control, setValue, watch } = useForm<CreateSession>({
    resolver: zodResolver(CreateSessionSchema),
    defaultValues: {
      gameId: game?.id.toString(),
      locationId: locationId,
      minutes: minutes,
      comment: comment,
      start: start,
      playerSessions: playerSessions,
    },
  });

  const selectedGameId = watch('gameId');

  useEffect(() => {
    if (selectedGameId !== undefined) {
      const selectedBoardGame = games.find((game) => game.id.toString() === selectedGameId);
      if (selectedBoardGame) {
        setValue('minutes', selectedBoardGame.maxPlayTime ?? 30, { shouldValidate: true });
        setValue('start', addMinutes(new Date(), -(selectedBoardGame?.maxPlayTime ?? 30)));
        setExpansionList(selectedBoardGame.expansions);
      }
    }
  }, [games, selectedGameId, setValue, start]);

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

  const onSubmit = async (data: CreateSession) => {
    data.expansionIds = selectedIds;
    await onClick(data);
  };

  return (
    <BgtPage>
      <BgtPageContent>
        <BgtCenteredCard title={title}>
          <form onSubmit={(event) => void handleSubmit(onSubmit)(event)} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <BgtSelect
                hasSearch
                control={control}
                name="gameId"
                items={
                  games.map((x) => ({
                    value: x.id.toString(),
                    label: x.title,
                    image: x.image,
                  })) ?? []
                }
                label={t('player-session.new.game.label')}
                disabled={disabled}
                placeholder={t('player-session.new.game.placeholder')}
              />
              <BgtSelect
                hasSearch
                control={control}
                name="locationId"
                items={
                  locations.map((x) => ({
                    value: x.id.toString(),
                    label: x.name,
                  })) ?? []
                }
                label={t('player-session.new.location.label')}
                disabled={disabled}
                placeholder={t('player-session.new.location.placeholder')}
              />
              <div className="flex flex-col justify-start">
                <div className="text-[15px] font-medium leading-[35px] uppercase pb-2">
                  {t('game.expansions.title')}
                </div>
                {expansionList.length === 0 && selectedGameId !== undefined && t('game.expansions.none')}
                {expansionList.length === 0 && selectedGameId === undefined && t('game.expansions.no-game')}
                <BgtCheckboxList
                  items={expansionList.map((x) => ({ id: x.id, value: x.title }))}
                  selectedIds={selectedIds}
                  onSelectionChange={(ids) => setSelectedIds(ids)}
                  disabled={disabled}
                />
              </div>
              <BgtInputField
                valueAsNumber
                name="minutes"
                type="number"
                control={control}
                disabled={disabled}
                label={t('player-session.new.duration.label')}
                placeholder={t('player-session.new.duration.placeholder')}
              />
              <BgtInputField
                name="start"
                type="datetime-local"
                control={control}
                disabled={disabled}
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
                disabled={disabled}
              />
              <BgtTextArea
                name="comment"
                register={register}
                disabled={disabled}
                label={t('player-session.new.comment.label')}
              />
              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="outline"
                  type="button"
                  disabled={disabled}
                  className="flex-none"
                  onClick={() => router.history.back()}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" disabled={disabled} className="flex-1" variant="soft">
                  {disabled && <Bars className="size-4" />}
                  {buttonText}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>

        <CreateSessionPlayerModal
          open={openCreateNewPlayerModal}
          hasScoring={game?.hasScoring ?? true}
          onClose={closeNewPlayPlayer}
          onCancel={() => setOpenCreateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
          players={playerList}
        />
        <UpdateSessionPlayerModal
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
