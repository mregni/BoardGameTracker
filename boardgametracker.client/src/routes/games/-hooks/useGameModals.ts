import { useModalState } from "@/hooks/useModalState";

export const useGameModals = () => {
	const createModal = useModalState();
	const bggModal = useModalState();
	const deleteModal = useModalState();
	const expansionModal = useModalState();

	return {
		createModal,
		bggModal,
		deleteModal,
		expansionModal,
	};
};
