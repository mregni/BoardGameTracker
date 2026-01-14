import { useNavigate } from '@tanstack/react-router';

interface UseGameActionsProps {
  gameId: number;
  deleteGame: () => Promise<void>;
  deleteExpansion: (expansionId: number, gameId: number) => void;
  onDeleteModalClose: () => void;
  onExpansionModalOpen: () => void;
}

export const useGameActions = (props: UseGameActionsProps) => {
  const { gameId, deleteGame, deleteExpansion, onDeleteModalClose, onExpansionModalOpen } = props;
  const navigate = useNavigate();

  const handleAddSession = () => {
    navigate({ to: `/sessions/new/${gameId}` });
  };

  const handleEdit = () => {
    navigate({ to: `/games/${gameId}/update` });
  };

  const handleDelete = async () => {
    await deleteGame();
    navigate({ to: '/games' });
    onDeleteModalClose();
  };

  const handleDeleteExpansion = (expansionId: number) => {
    deleteExpansion(expansionId, gameId);
  };

  const handleAddExpansion = () => {
    onExpansionModalOpen();
  };

  const handleViewAllSessions = () => {
    navigate({ to: `/games/${gameId}/sessions` });
  };

  const handleBackToGames = () => {
    navigate({ to: '/games' });
  };

  return {
    handleAddSession,
    handleEdit,
    handleDelete,
    handleDeleteExpansion,
    handleAddExpansion,
    handleViewAllSessions,
    handleBackToGames,
  };
};
