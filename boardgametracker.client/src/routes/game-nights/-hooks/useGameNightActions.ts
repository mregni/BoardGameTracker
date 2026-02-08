import { CreateGameNight, GameNight } from '@/models';

interface UseGameNightActionsProps {
  createGameNight: (data: CreateGameNight) => Promise<unknown>;
  deleteGameNight: (id: number) => Promise<unknown>;
  selectedGameNight: GameNight | null;
  onCreateModalClose: () => void;
  onDeleteModalClose: () => void;
  onManageRsvpModalOpen: () => void;
  setSelectedGameNight: (gameNight: GameNight | null) => void;
}

export const useGameNightActions = (props: UseGameNightActionsProps) => {
  const {
    createGameNight,
    deleteGameNight,
    selectedGameNight,
    onCreateModalClose,
    onDeleteModalClose,
    onManageRsvpModalOpen,
    setSelectedGameNight,
  } = props;

  const handleCreate = async (gameNight: CreateGameNight) => {
    await createGameNight(gameNight);
    onCreateModalClose();
  };

  const handleDelete = async () => {
    if (!selectedGameNight) return;
    await deleteGameNight(selectedGameNight.id);
    onDeleteModalClose();
    setSelectedGameNight(null);
  };

  const handleDeleteClick = (gameNight: GameNight) => {
    setSelectedGameNight(gameNight);
  };

  const handleManageRsvps = (gameNight: GameNight) => {
    setSelectedGameNight(gameNight);
    onManageRsvpModalOpen();
  };

  const handleEditGameNight = (gameNight: GameNight) => {
    // TODO: Implement edit functionality
    console.log('Edit game night:', gameNight);
  };

  return {
    handleCreate,
    handleDelete,
    handleDeleteClick,
    handleManageRsvps,
    handleEditGameNight,
  };
};
