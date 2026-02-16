import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useCallback } from 'react';
import { useRouter } from '@tanstack/react-router';

import { sessionFormOpts } from '../-utils/sessionFormOpts';
import { useSessionFormState } from '../-hooks/useSessionFormState';
import { useSessionForm } from '../-hooks/useSessionForm';

import { SessionPlayerManager } from './SessionPlayerManager';
import { SessionFormFields } from './SessionFormFields';
import { SessionExpansionSelector } from './SessionExpansionSelector';

import { handleFormSubmit } from '@/utils/formUtils';
import {
  CreateSession,
  CreateSessionPlayer,
  CreatePlayerSessionNoScoring,
  Game,
  Expansion,
  CreateSessionSchema,
} from '@/models';
import { useAppForm } from '@/hooks/form';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCenteredCard } from '@/components/BgtCard/BgtCenteredCard';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  game?: Game | undefined;
  locationId?: number | undefined;
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

  const { t } = useTranslation();
  const router = useRouter();
  const { locations, games, players: playerList } = useSessionForm();

  const form = useAppForm({
    ...sessionFormOpts,
    defaultValues: {
      ...sessionFormOpts.defaultValues,
      gameId: game?.id ?? 0,
      locationId: locationId ?? 0,
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
    openEditPlayerModal,
    isCreateModalOpen,
    isUpdateModalOpen,
    openCreateModal,
    closeModal,
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

  return (
    <BgtPage>
      <BgtPageContent centered>
        <BgtCenteredCard title={title}>
          <form onSubmit={handleFormSubmit(form)} className="w-full">
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
                isCreateModalOpen={isCreateModalOpen}
                isUpdateModalOpen={isUpdateModalOpen}
                playerIdToEdit={playerIdToEdit}
                onOpenCreateModal={openCreateModal}
                onCloseModal={closeModal}
                onEditPlayer={openEditPlayerModal}
                onAddPlayer={addPlayer}
                onUpdatePlayer={updatePlayer}
                onRemovePlayer={removePlayer}
              />

              <div className="flex flex-row gap-2 mt-2">
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
