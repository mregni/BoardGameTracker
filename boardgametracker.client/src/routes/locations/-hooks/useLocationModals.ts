import { useModalState } from "@/hooks/useModalState";

export const useLocationModals = () => {
	const createModal = useModalState();
	const editModal = useModalState();
	const deleteModal = useModalState();

	return {
		createModal,
		editModal,
		deleteModal,
	};
};
