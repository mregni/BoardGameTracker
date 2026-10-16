import { useModalState } from "@/hooks/useModalState";

export const useLoanModals = () => {
	const createModal = useModalState();
	const deleteModal = useModalState();
	const editModal = useModalState();

	return {
		createModal,
		deleteModal,
		editModal,
	};
};
