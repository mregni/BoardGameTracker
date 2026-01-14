import { useModalState } from '@/hooks/useModalState';

export const usePlayerModals = () => {
  const editModal = useModalState();
  const deleteModal = useModalState();

  return {
    editModal,
    deleteModal,
  };
};
