import { useNavigate } from '@tanstack/react-router';

interface UsePlayerActionsProps {
  playerId: number;
  deletePlayer: (playerId: number) => Promise<void>;
  onDeleteModalClose: () => void;
}

export const usePlayerActions = (props: UsePlayerActionsProps) => {
  const { playerId, deletePlayer, onDeleteModalClose } = props;
  const navigate = useNavigate();

  const handleDelete = async () => {
    await deletePlayer(playerId);
    navigate({ to: '/players' });
    onDeleteModalClose();
  };

  const handleViewSessions = () => {
    navigate({ to: `/players/${playerId}/sessions` });
  };

  const handleBackToPlayers = () => {
    navigate({ to: '/players' });
  };

  return {
    handleDelete,
    handleViewSessions,
    handleBackToPlayers,
  };
};
