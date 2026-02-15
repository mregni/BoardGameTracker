import { memo } from 'react';

import { UpdateSessionPlayerModal } from '../-modals/UpdateSessionPlayerModal';
import { CreateSessionPlayerModal } from '../-modals/CreateSessionPlayerModal';

import { CreateSessionPlayer, CreatePlayerSessionNoScoring, Player } from '@/models';
import { CreateSessionSchema } from '@/models';
import { type AnyReactForm, BgtPlayerSelector, BgtFormField } from '@/components/BgtForm';

interface SessionPlayerManagerProps {
  form: AnyReactForm;
  players: (CreateSessionPlayer | CreatePlayerSessionNoScoring)[];
  playerList: Player[];
  hasScoring: boolean;
  disabled: boolean;
  isCreateModalOpen: boolean;
  isUpdateModalOpen: boolean;
  playerIdToEdit: number | null;
  onOpenCreateModal: () => void;
  onCloseModal: () => void;
  onEditPlayer: (playerId: number) => void;
  onAddPlayer: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onUpdatePlayer: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onRemovePlayer: (index: number) => void;
}

const SessionPlayerManagerComponent = ({
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
}: SessionPlayerManagerProps) => {
  return (
    <>
      <BgtFormField form={form} name="playerSessions" schema={CreateSessionSchema}>
        {(field) => {
          if (field.state.value !== players) {
            field.handleChange(players);
          }
          return (
            <BgtPlayerSelector
              onOpenCreateModal={onOpenCreateModal}
              onEditPlayer={onEditPlayer}
              remove={onRemovePlayer}
              players={players}
              disabled={disabled}
              errors={field.state.meta.errors}
            />
          );
        }}
      </BgtFormField>

      <CreateSessionPlayerModal
        open={isCreateModalOpen}
        hasScoring={hasScoring}
        onClose={onAddPlayer}
        onCancel={onCloseModal}
        selectedPlayerIds={players.map((x) => x.playerId)}
        players={playerList}
      />
      <UpdateSessionPlayerModal
        open={isUpdateModalOpen}
        hasScoring={hasScoring}
        onClose={onUpdatePlayer}
        onCancel={onCloseModal}
        selectedPlayerIds={players.map((x) => x.playerId)}
        playerToEdit={players.find((x) => x.playerId === playerIdToEdit)}
      />
    </>
  );
};

SessionPlayerManagerComponent.displayName = 'SessionPlayerManager';

export const SessionPlayerManager = memo(SessionPlayerManagerComponent);
