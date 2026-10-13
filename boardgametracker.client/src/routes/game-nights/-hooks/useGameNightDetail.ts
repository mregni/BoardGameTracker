import { useMutation, useQuery } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { FinishRunningGame, Session, StartRunningGame } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import {
	discardRunningGameCall,
	endGameNightCall,
	finishRunningGameCall,
	reopenGameNightCall,
	startRunningGameCall,
} from "@/services/gameNightService";
import { getGameNight } from "@/services/queries/gameNights";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useGameNightDetail = (gameNightId: number) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();
	const detailQuery = useQuery(getGameNight(gameNightId));

	const refreshSession = (session: Session) =>
		Promise.all([
			invalidator.invalidateGameNights(),
			invalidator.invalidateSession(
				session.id,
				session.gameId,
				session.playerSessions.map((x) => x.playerId),
			),
		]);

	const startMutation = useMutation({
		mutationFn: (command: StartRunningGame) => startRunningGameCall(gameNightId, command),
		onSuccess: async () => {
			successToast("game-nights:play.notifications.started");
			await invalidator.invalidateGameNights();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game-nights:play.notifications.start-failed"));
		},
	});

	const finishMutation = useMutation({
		mutationFn: ({ id, command }: { id: number; command: FinishRunningGame }) => finishRunningGameCall(id, command),
		onSuccess: async (session) => {
			successToast("player-session:new.notifications.created");
			await refreshSession(session);
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "player-session:new.notifications.create-failed"));
		},
	});

	const discardMutation = useMutation({
		mutationFn: discardRunningGameCall,
		onSuccess: async () => {
			successToast("game-nights:play.notifications.discarded");
			await invalidator.invalidateGameNights();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game-nights:play.notifications.discard-failed"));
		},
	});

	const endMutation = useMutation({
		mutationFn: () => endGameNightCall(gameNightId),
		onSuccess: async () => {
			successToast("game-nights:end.notifications.ended");
			await invalidator.invalidateGameNights();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game-nights:end.notifications.end-failed"));
		},
	});

	const reopenMutation = useMutation({
		mutationFn: () => reopenGameNightCall(gameNightId),
		onSuccess: async () => {
			successToast("game-nights:end.notifications.reopened");
			await invalidator.invalidateGameNights();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "game-nights:end.notifications.reopen-failed"));
		},
	});

	return {
		detail: detailQuery.data,
		endNight: endMutation.mutateAsync,
		reopenNight: reopenMutation.mutateAsync,
		isLoading: detailQuery.isLoading,
		startGame: startMutation.mutateAsync,
		finishGame: finishMutation.mutateAsync,
		discardGame: discardMutation.mutateAsync,
		isPending:
			startMutation.isPending ||
			finishMutation.isPending ||
			discardMutation.isPending ||
			endMutation.isPending ||
			reopenMutation.isPending,
	};
};
