import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useGoUp } from "@/routes/-hooks/useGoUp";

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

	const handleBackToPlayers = useGoUp("/players");

	return {
		handleDelete,
		handleViewSessions,
		handleBackToPlayers,
	};
};
