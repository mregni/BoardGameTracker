import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { addSessionCall } from "@/services/sessionService";
import { getGames } from "@/services/queries/games";

interface Props {
  onSuccess?: () => void;
}

export const useNewSessionData = ({ onSuccess }: Props = {}) => {
  const invalidator = useQueryInvalidator();
  const { successToast, errorToast } = useToasts();

  const [gamesQuery] = useQueries({
    queries: [getGames()],
  });

  const games = gamesQuery.data ?? [];

  const saveSessionMutation = useMutation({
    mutationFn: addSessionCall,
    async onSuccess(sessionResult) {
      successToast("player-session.new.notifications.created");
      onSuccess?.();

      await invalidator.invalidateSession(
        sessionResult.id,
        sessionResult.gameId,
      );
    },
    onError: () => {
      errorToast("player-session.new.notifications.create-failed");
    },
  });

  return {
    isPending: saveSessionMutation.isPending,
    saveSession: saveSessionMutation.mutateAsync,
    games,
  };
};
