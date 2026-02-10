import { useModalState } from '@/hooks/useModalState';

export const usePlayerModals = () => {
  const createModal = useModalState();
  const editModal = useModalState();
  const deleteModal = useModalState();

  return {
    createModal,
    editModal,
    deleteModal,
  };
};
