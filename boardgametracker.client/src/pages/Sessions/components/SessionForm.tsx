import { useNavigate } from 'react-router-dom';
import { Bars } from 'react-loading-icons';
import { useForm, useFieldArray } from 'react-hook-form';
import { useState, useEffect } from 'react';
import { t } from 'i18next';
import { addMinutes } from 'date-fns';
import { zodResolver } from '@hookform/resolvers/zod';

import { UpdateSessionPlayerModal } from '../modals/UpdateSessionPlayerModal';
import { CreateSessionPlayerModal } from '../modals/CreateSessionPlayerModal';

import { useGames } from '@/pages/Games/hooks/useGames';
import { useGame } from '@/pages/Games/hooks/useGame';
import {
  CreateSession,
  CreateSessionSchema,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
} from '@/models/Session/CreateSession';
import { useLocations } from '@/hooks/useLocations';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtTextArea } from '@/components/BgtForm/BgtTextArea';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtPlayerSelector } from '@/components/BgtForm/BgtPlayerSelector';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  gameId: string | undefined;
  locationId?: string | undefined;
  minutes?: number | undefined;
  comment?: string | null;
  start?: Date | undefined;
  playerSessions?: CreateSessionPlayer[] | CreatePlayerSessionNoScoring[] | undefined;
  onClick: (data: CreateSession) => Promise<void>;
  buttonText: string;
  title: string;
  disabled: boolean;
}

export const SessionForm = (props: Props) => {
  const {
    gameId,
    locationId,
    minutes,
    comment = '',
    start,
    playerSessions = [],
    onClick,
    buttonText,
    title,
    disabled,
  } = props;
  const navigate = useNavigate();

  const { locations } = useLocations({});
  const { games } = useGames({});
  const { game } = useGame({ id: gameId });

  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);
  const [openUpdateNewPlayerModal, setOpenUpdateNewPlayerModal] = useState(false);
  const [playerIdToEdit, setPlayerIdToEdit] = useState<string | null>(null);

  const { register, handleSubmit, control, setValue, watch } = useForm<CreateSession>({
    resolver: zodResolver(CreateSessionSchema),
    defaultValues: {
      gameId: gameId,
      locationId: locationId,
      minutes: minutes,
      comment: comment,
      start: start,
      playerSessions: playerSessions,
    },
  });

  const selectedGameId = watch('gameId');

  useEffect(() => {
    if (selectedGameId !== undefined && start == undefined) {
      const selectedBoardGame = games.data?.find((game) => game.id.toString() === selectedGameId);
      if (selectedBoardGame) {
        setValue('minutes', selectedBoardGame.maxPlayTime ?? 30, { shouldValidate: true });
        setValue('start', addMinutes(new Date(), -(selectedBoardGame?.maxPlayTime ?? 30)));
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

  if (locations === undefined || games.data === undefined) return null;

  const onSubmit = async (data: CreateSession) => {
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
                  games.data.map((x) => ({
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
                  onClick={() => navigate(-1)}
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
          hasScoring={game.data?.hasScoring ?? true}
          onClose={closeNewPlayPlayer}
          onCancel={() => setOpenCreateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
        />
        <UpdateSessionPlayerModal
          open={openUpdateNewPlayerModal}
          hasScoring={game.data?.hasScoring ?? true}
          onClose={closeUpdatePlayPlayer}
          onCancel={() => setOpenUpdateNewPlayerModal(false)}
          selectedPlayerIds={players.map((x) => x.playerId)}
          playerToEdit={players.find((x) => x.id === playerIdToEdit)}
        />
      </BgtPageContent>
    </BgtPage>
  );
};
