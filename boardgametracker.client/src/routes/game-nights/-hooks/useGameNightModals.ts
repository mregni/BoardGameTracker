import { useModalState } from '@/hooks/useModalState';

export const useGameNightModals = () => {
  const createModal = useModalState();
  const deleteModal = useModalState();
  const manageRsvpModal = useModalState();

  return {
    createModal,
    deleteModal,
    manageRsvpModal,
  };
};
