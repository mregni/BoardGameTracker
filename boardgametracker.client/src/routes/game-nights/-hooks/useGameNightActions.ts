import { CreateGameNight, GameNight } from '@/models';

interface UseGameNightActionsProps {
  createGameNight: (data: CreateGameNight) => Promise<unknown>;
  deleteGameNight: (id: number) => Promise<unknown>;
  selectedGameNightId: number | null;
  onCreateModalClose: () => void;
  onEditModalOpen: () => void;
  onDeleteModalClose: () => void;
  onManageRsvpModalOpen: () => void;
  setSelectedGameNightId: (id: number | null) => void;
}

export const useGameNightActions = (props: UseGameNightActionsProps) => {
  const {
    createGameNight,
    deleteGameNight,
    selectedGameNightId,
    onCreateModalClose,
    onEditModalOpen,
    onDeleteModalClose,
    onManageRsvpModalOpen,
    setSelectedGameNightId,
  } = props;

  const handleCreate = async (gameNight: CreateGameNight) => {
    await createGameNight(gameNight);
    onCreateModalClose();
  };

  const handleDelete = async () => {
    if (selectedGameNightId === null) return;
    await deleteGameNight(selectedGameNightId);
    onDeleteModalClose();
    setSelectedGameNightId(null);
  };

  const handleDeleteClick = (gameNight: GameNight) => {
    setSelectedGameNightId(gameNight.id);
  };

  const handleManageRsvps = (gameNight: GameNight) => {
    setSelectedGameNightId(gameNight.id);
    onManageRsvpModalOpen();
  };

  const handleEditGameNight = (gameNight: GameNight) => {
    setSelectedGameNightId(gameNight.id);
    onEditModalOpen();
  };

  return {
    handleCreate,
    handleDelete,
    handleDeleteClick,
    handleManageRsvps,
    handleEditGameNight,
  };
};
