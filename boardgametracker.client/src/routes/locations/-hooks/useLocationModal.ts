import { useQueryClient, useMutation } from '@tanstack/react-query';

import { addLocationCall, updateLocationCall } from '@/services/locationService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
  onUpdateSuccess?: () => void;
  onUpdateError?: () => void;
}

export const useLocationModal = ({ onSaveSuccess, onUpdateSuccess, onUpdateError, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const saveMutation = useMutation({
    mutationFn: addLocationCall,
    async onSuccess() {
      onSaveSuccess?.();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
    },
    onError: () => {
      onSaveError?.();
    },
  });

  const updateMutation = useMutation({
    mutationFn: updateLocationCall,
    async onSuccess() {
      onUpdateSuccess?.();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onError: () => {
      onUpdateError?.();
    },
  });

  const isLoading = saveMutation.isPending || updateMutation.isPending;

  return {
    saveLocation: saveMutation.mutateAsync,
    updateLocation: updateMutation.mutateAsync,
    isLoading,
  };
};
