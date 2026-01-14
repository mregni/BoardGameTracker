import { useModalState } from '@/hooks/useModalState';

export const useGameModals = () => {
  const deleteModal = useModalState();
  const expansionModal = useModalState();

  return {
    deleteModal,
    expansionModal,
  };
};
