import { Bars } from 'react-loading-icons';
import { useCallback } from 'react';
import { t } from 'i18next';
import { useRouter } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useSessionFormState } from '../-hooks/useSessionFormState';
import { useSessionForm } from '../-hooks/useSessionForm';

import { SessionPlayerManager } from './SessionPlayerManager';
import { SessionFormFields } from './SessionFormFields';
import { SessionExpansionSelector } from './SessionExpansionSelector';

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

  const form = useForm({
    defaultValues: {
      gameId: game?.id ?? 0,
      locationId: locationId ?? '',
      minutes: minutes ?? 30,
      comment: comment ?? '',
      start: start ?? new Date(),
      playerSessions: playerSessions,
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateSessionSchema.parse({
        ...value,
        playerSessions: players,
        expansionIds: selectedExpansionIds,
      });
      await onClick(validatedData);
    },
  });

  const {
    selectedGameId,
    expansionList,
    selectedExpansionIds,
    setSelectedExpansionIds,
    players,
    addPlayer,
    updatePlayer,
    removePlayer,
    openCreatePlayerModal,
    setOpenCreatePlayerModal,
    openUpdatePlayerModal,
    setOpenUpdatePlayerModal,
    playerIdToEdit,
  } = useSessionFormState({
    form,
    games,
    initialGameId: game?.id,
    initialExpansions: expansions,
    initialPlayerSessions: playerSessions,
  });

  const handleCancel = useCallback(() => {
    router.history.back();
  }, [router]);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      e.stopPropagation();
      form.handleSubmit();
    },
    [form]
  );

  return (
    <BgtPage>
      <BgtPageContent centered>
        <BgtCenteredCard title={title}>
          <form onSubmit={handleSubmit} className="w-full">
            <div className="flex flex-col gap-3 w-full">
              <SessionFormFields form={form} games={games} locations={locations} disabled={disabled} />

              <SessionExpansionSelector
                expansionList={expansionList}
                selectedIds={selectedExpansionIds}
                selectedGameId={selectedGameId}
                disabled={disabled}
                onSelectionChange={setSelectedExpansionIds}
              />

              <SessionPlayerManager
                form={form}
                players={players}
                playerList={playerList}
                hasScoring={game?.hasScoring ?? true}
                disabled={disabled}
                openCreatePlayerModal={openCreatePlayerModal}
                openUpdatePlayerModal={openUpdatePlayerModal}
                playerIdToEdit={playerIdToEdit}
                setOpenCreatePlayerModal={setOpenCreatePlayerModal}
                setOpenUpdatePlayerModal={setOpenUpdatePlayerModal}
                onAddPlayer={addPlayer}
                onUpdatePlayer={updatePlayer}
                onRemovePlayer={removePlayer}
                setPlayerIdToEdit={() => {}}
              />

              <div className="flex flex-row gap-2">
                <BgtButton
                  variant="cancel"
                  type="button"
                  disabled={disabled}
                  className="flex-none"
                  onClick={handleCancel}
                >
                  {t('common.cancel')}
                </BgtButton>
                <BgtButton type="submit" disabled={disabled} className="flex-1" variant="primary">
                  {disabled && <Bars className="size-4" />}
                  {buttonText}
                </BgtButton>
              </div>
            </div>
          </form>
        </BgtCenteredCard>
      </BgtPageContent>
    </BgtPage>
  );
};
