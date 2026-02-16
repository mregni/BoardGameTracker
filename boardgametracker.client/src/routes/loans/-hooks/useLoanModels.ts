import { useModalState } from "@/hooks/useModalState";

export const useLoanModals = () => {
	const createModal = useModalState();
	const deleteModal = useModalState();

	return {
		createModal,
		deleteModal,
	};
};
