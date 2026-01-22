import { memo } from 'react';
import { UseFormReturn } from '@tanstack/react-form';

import { UpdateSessionPlayerModal } from '../-modals/UpdateSessionPlayerModal';
import { CreateSessionPlayerModal } from '../-modals/CreateSessionPlayerModal';

import { CreateSession, CreateSessionPlayer, CreatePlayerSessionNoScoring, Player } from '@/models';
import { CreateSessionSchema } from '@/models';
import { BgtPlayerSelector, BgtFormField } from '@/components/BgtForm';

interface SessionPlayerManagerProps {
  form: UseFormReturn<CreateSession>;
  players: (CreateSessionPlayer | CreatePlayerSessionNoScoring)[];
  playerList: Player[];
  hasScoring: boolean;
  disabled: boolean;
  openCreatePlayerModal: boolean;
  openUpdatePlayerModal: boolean;
  playerIdToEdit: number | null;
  setOpenCreatePlayerModal: (open: boolean) => void;
  setOpenUpdatePlayerModal: (open: boolean) => void;
  onAddPlayer: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onUpdatePlayer: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
  onRemovePlayer: (index: number) => void;
  setPlayerIdToEdit: (id: number | null) => void;
}

const SessionPlayerManagerComponent = ({
  form,
  players,
  playerList,
  hasScoring,
  disabled,
  openCreatePlayerModal,
  openUpdatePlayerModal,
  playerIdToEdit,
  setOpenCreatePlayerModal,
  setOpenUpdatePlayerModal,
  onAddPlayer,
  onUpdatePlayer,
  onRemovePlayer,
  setPlayerIdToEdit,
}: SessionPlayerManagerProps) => {
  return (
    <>
      <BgtFormField form={form} name="playerSessions" schema={CreateSessionSchema.shape.playerSessions}>
        {(field) => {
          if (field.state.value !== players) {
            field.handleChange(players);
          }
          return (
            <BgtPlayerSelector
              setCreateModalOpen={setOpenCreatePlayerModal}
              setUpdateModalOpen={setOpenUpdatePlayerModal}
              remove={onRemovePlayer}
              players={players}
              setPlayerIdToEdit={setPlayerIdToEdit}
              disabled={disabled}
              errors={field.state.meta.errors}
            />
          );
        }}
      </BgtFormField>

      <CreateSessionPlayerModal
        open={openCreatePlayerModal}
        hasScoring={hasScoring}
        onClose={onAddPlayer}
        onCancel={() => setOpenCreatePlayerModal(false)}
        selectedPlayerIds={players.map((x) => x.playerId)}
        players={playerList}
      />
      <UpdateSessionPlayerModal
        open={openUpdatePlayerModal}
        hasScoring={hasScoring}
        onClose={onUpdatePlayer}
        onCancel={() => setOpenUpdatePlayerModal(false)}
        selectedPlayerIds={players.map((x) => x.playerId)}
        playerToEdit={players.find((x) => x.playerId === playerIdToEdit)}
      />
    </>
  );
};

SessionPlayerManagerComponent.displayName = 'SessionPlayerManager';

export const SessionPlayerManager = memo(SessionPlayerManagerComponent);
