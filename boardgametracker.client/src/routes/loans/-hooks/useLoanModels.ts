import { useModalState } from '@/hooks/useModalState';

export const useLoanModals = () => {
  const deleteModal = useModalState();

  return {
    deleteModal,
  };
};
