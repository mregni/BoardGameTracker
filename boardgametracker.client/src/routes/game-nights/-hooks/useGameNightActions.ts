import { useCallback } from "react";

import type { CreateGameNight, GameNight } from "@/models";

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

	const handleCreate = useCallback(
		async (gameNight: CreateGameNight) => {
			await createGameNight(gameNight);
			onCreateModalClose();
		},
		[createGameNight, onCreateModalClose],
	);

	const handleDelete = useCallback(async () => {
		if (selectedGameNightId === null) return;
		await deleteGameNight(selectedGameNightId);
		onDeleteModalClose();
		setSelectedGameNightId(null);
	}, [selectedGameNightId, deleteGameNight, onDeleteModalClose, setSelectedGameNightId]);

	const handleDeleteClick = useCallback(
		(gameNight: GameNight) => {
			setSelectedGameNightId(gameNight.id);
		},
		[setSelectedGameNightId],
	);

	const handleManageRsvps = useCallback(
		(gameNight: GameNight) => {
			setSelectedGameNightId(gameNight.id);
			onManageRsvpModalOpen();
		},
		[setSelectedGameNightId, onManageRsvpModalOpen],
	);

	const handleEditGameNight = useCallback(
		(gameNight: GameNight) => {
			setSelectedGameNightId(gameNight.id);
			onEditModalOpen();
		},
		[setSelectedGameNightId, onEditModalOpen],
	);

	return {
		handleCreate,
		handleDelete,
		handleDeleteClick,
		handleManageRsvps,
		handleEditGameNight,
	};
};
