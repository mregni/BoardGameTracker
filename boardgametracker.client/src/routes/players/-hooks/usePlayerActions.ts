import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";

interface UsePlayerActionsProps {
	playerId: number;
	deletePlayer: (playerId: number) => Promise<void>;
	onDeleteModalClose: () => void;
}

export const usePlayerActions = (props: UsePlayerActionsProps) => {
	const { playerId, deletePlayer, onDeleteModalClose } = props;
	const navigate = useNavigate();

	const handleDelete = useCallback(async () => {
		await deletePlayer(playerId);
		navigate({ to: "/players" });
		onDeleteModalClose();
	}, [deletePlayer, playerId, navigate, onDeleteModalClose]);

	const handleViewSessions = useCallback(() => {
		navigate({ to: `/players/${playerId}/sessions` });
	}, [navigate, playerId]);

	const handleBackToPlayers = useCallback(() => {
		navigate({ to: "/players" });
	}, [navigate]);

	return {
		handleDelete,
		handleViewSessions,
		handleBackToPlayers,
	};
};
