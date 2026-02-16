import { useEffect } from 'react';
import type { AnyFieldApi } from '@tanstack/react-form';

import { sessionFormOpts } from '../-utils/sessionFormOpts';
import { UpdateSessionPlayerModal } from '../-modals/UpdateSessionPlayerModal';
import { CreateSessionPlayerModal } from '../-modals/CreateSessionPlayerModal';

import { zodValidator } from '@/utils/zodValidator';
import { type CreateSessionPlayer, type CreatePlayerSessionNoScoring, type Player, CreateSessionSchema } from '@/models';
import { withForm } from '@/hooks/form';
import { BgtPlayerSelector } from '@/components/BgtForm';

export const SessionPlayerManager = withForm({
  ...sessionFormOpts,
  props: {
    players: [] as (CreateSessionPlayer | CreatePlayerSessionNoScoring)[],
    playerList: [] as Player[],
    hasScoring: true,
    disabled: false,
    isCreateModalOpen: false,
    isUpdateModalOpen: false,
    playerIdToEdit: null as number | null,
    onOpenCreateModal: () => {},
    onCloseModal: () => {},
    onEditPlayer: (() => {}) as (playerId: number) => void,
    onAddPlayer: (() => {}) as (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void,
    onUpdatePlayer: (() => {}) as (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void,
    onRemovePlayer: (() => {}) as (index: number) => void,
  },
  render: function Render({
    form,
    players,
    playerList,
    hasScoring,
    disabled,
    isCreateModalOpen,
    isUpdateModalOpen,
    playerIdToEdit,
    onOpenCreateModal,
    onCloseModal,
    onEditPlayer,
    onAddPlayer,
    onUpdatePlayer,
    onRemovePlayer,
  }) {
    useEffect(() => {
      form.setFieldValue('playerSessions', players);
    }, [form, players]);

    return (
      <>
        <form.Field name="playerSessions" validators={zodValidator(CreateSessionSchema, 'playerSessions')}>
          {(field: AnyFieldApi) => (
            <BgtPlayerSelector
              onOpenCreateModal={onOpenCreateModal}
              onEditPlayer={onEditPlayer}
              remove={onRemovePlayer}
              players={players}
              disabled={disabled}
              errors={field.state.meta.errors}
            />
          )}
        </form.Field>

        <CreateSessionPlayerModal
          open={isCreateModalOpen}
          hasScoring={hasScoring}
          onClose={onAddPlayer}
          onCancel={onCloseModal}
          selectedPlayerIds={players.map((x: CreateSessionPlayer | CreatePlayerSessionNoScoring) => x.playerId)}
          players={playerList}
        />
        <UpdateSessionPlayerModal
          open={isUpdateModalOpen}
          hasScoring={hasScoring}
          onClose={onUpdatePlayer}
          onCancel={onCloseModal}
          selectedPlayerIds={players.map((x: CreateSessionPlayer | CreatePlayerSessionNoScoring) => x.playerId)}
          playerToEdit={players.find((x: CreateSessionPlayer | CreatePlayerSessionNoScoring) => x.playerId === playerIdToEdit)}
        />
      </>
    );
  },
});
