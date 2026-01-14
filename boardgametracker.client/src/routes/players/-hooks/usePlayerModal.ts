import { useMutation, useQueryClient } from '@tanstack/react-query';

import { addPlayerCall, updatePlayerCall } from '@/services/playerService';
import { uploadImageCall } from '@/services/imageService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
  onUpdateSuccess?: () => void;
  onUpdateError?: () => void;
}

export const usePlayerModal = ({ onSaveSuccess, onUpdateSuccess, onUpdateError, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const uploadImageMutation = useMutation({
    mutationFn: uploadImageCall,
  });

  const saveMutation = useMutation({
    mutationFn: addPlayerCall,
    async onSuccess() {
      onSaveSuccess?.();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
    },
    onError: () => {
      onSaveError?.();
    },
  });

  const updateMutation = useMutation({
    mutationFn: updatePlayerCall,
    async onSuccess() {
      onUpdateSuccess?.();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player] });
    },
    onError: () => {
      onUpdateError?.();
    },
  });

  const isLoading = uploadImageMutation.isPending || saveMutation.isPending || updateMutation.isPending;

  return {
    savePlayer: saveMutation.mutateAsync,
    updatePlayer: updateMutation.mutateAsync,
    uploadImage: uploadImageMutation.mutateAsync,
    isLoading,
  };
};
