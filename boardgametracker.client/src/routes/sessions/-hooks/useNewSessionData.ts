import { useMutation } from '@tanstack/react-query';

import { addSessionCall } from '@/services/sessionService';
import { useToasts } from '@/routes/-hooks/useToasts';
import { useQueryInvalidator } from '@/hooks/useQueryInvalidator';

interface Props {
  onSuccess?: () => void;
}

export const useNewSessionData = ({ onSuccess }: Props = {}) => {
  const invalidator = useQueryInvalidator();
  const { successToast, errorToast } = useToasts();

  const saveSessionMutation = useMutation({
    mutationFn: addSessionCall,
    async onSuccess(sessionResult) {
      successToast('player-session.new.notifications.created');
      onSuccess?.();

      await invalidator.invalidateSession(sessionResult.id, sessionResult.gameId);
    },
    onError: () => {
      errorToast('player-session.new.notifications.create-failed');
    },
  });

  return {
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
  };
};
