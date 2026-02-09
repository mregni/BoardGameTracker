import { useModalState } from '@/hooks/useModalState';

export const useGameNightModals = () => {
  const createModal = useModalState();
  const editModal = useModalState();
  const deleteModal = useModalState();
  const manageRsvpModal = useModalState();

  return {
    createModal,
    editModal,
    deleteModal,
    manageRsvpModal,
  };
};
