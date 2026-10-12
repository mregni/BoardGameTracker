import { useNavigate, useRouter } from "@tanstack/react-router";
import { useCallback } from "react";

interface UseGameActionsProps {
	gameId: number;
	deleteGame: (onDeleted?: () => Promise<unknown>) => Promise<void>;
	deleteExpansion: (expansionId: number, gameId: number) => void;
	onDeleteModalClose: () => void;
	onExpansionModalOpen: () => void;
}

export const useGameActions = (props: UseGameActionsProps) => {
	const { gameId, deleteGame, deleteExpansion, onDeleteModalClose, onExpansionModalOpen } = props;
	const navigate = useNavigate();
	const router = useRouter();

	const handleAddSession = useCallback(() => {
		navigate({ to: `/sessions/new/${gameId}` });
	}, [navigate, gameId]);

	const handleEdit = useCallback(() => {
		navigate({ to: `/games/${gameId}/update` });
	}, [navigate, gameId]);

	const handleDelete = useCallback(async () => {
		await deleteGame(() => navigate({ to: "/games" }));
		onDeleteModalClose();
	}, [deleteGame, navigate, onDeleteModalClose]);

	const handleDeleteExpansion = useCallback(
		(expansionId: number) => {
			deleteExpansion(expansionId, gameId);
		},
		[deleteExpansion, gameId],
	);

	const handleAddExpansion = useCallback(() => {
		onExpansionModalOpen();
	}, [onExpansionModalOpen]);

	const handleViewAllSessions = useCallback(() => {
		navigate({ to: `/games/${gameId}/sessions` });
	}, [navigate, gameId]);

	const handleBackToGames = useCallback(() => {
		if (router.history.canGoBack()) {
			router.history.back();
			return;
		}
		navigate({ to: "/games" });
	}, [navigate, router]);

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
