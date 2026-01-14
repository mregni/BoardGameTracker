import { useMutation } from '@tanstack/react-query';

import { addSessionCall } from '@/services/sessionService';
import { useQueryInvalidator } from '@/hooks/useQueryInvalidator';

interface Props {
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useNewSessionData = ({ onSaveSuccess, onSaveError }: Props) => {
  const invalidator = useQueryInvalidator();

  const saveSessionMutation = useMutation({
    mutationFn: addSessionCall,
    async onSuccess(sessionResult) {
      onSaveSuccess?.();

      await invalidator.invalidateSession(sessionResult.id, sessionResult.gameId);
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
  };
};
