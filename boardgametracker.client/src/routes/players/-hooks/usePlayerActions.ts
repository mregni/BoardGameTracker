import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useBack } from "@/routes/-hooks/useBack";

interface UsePlayerActionsProps {
	playerId: number;
	deletePlayer: (playerId: number, onDeleted?: () => Promise<unknown>) => Promise<void>;
	onDeleteModalClose: () => void;
}

export const usePlayerActions = (props: UsePlayerActionsProps) => {
	const { playerId, deletePlayer, onDeleteModalClose } = props;
	const navigate = useNavigate();

	const handleDelete = useCallback(async () => {
		await deletePlayer(playerId, () => navigate({ to: "/players" }));
		onDeleteModalClose();
	}, [deletePlayer, playerId, navigate, onDeleteModalClose]);

	const handleViewSessions = useCallback(() => {
		navigate({ to: `/players/${playerId}/sessions` });
	}, [navigate, playerId]);

	const handleBackToPlayers = useBack("/players");

	return {
		handleDelete,
		handleViewSessions,
		handleBackToPlayers,
	};
};
