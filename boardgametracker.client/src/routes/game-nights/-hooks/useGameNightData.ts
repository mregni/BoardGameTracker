import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import {
	createGameNightCall,
	deleteGameNightCall,
	updateGameNightCall,
	updateGameNightRsvpCall,
} from "@/services/gameNightService";
import { getGameNights } from "@/services/queries/gameNights";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";

export const useGameNightData = () => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const [gameNightsQuery, settingsQuery, playersQuery, gamesQuery, locationsQuery] = useQueries({
		queries: [getGameNights(), getSettings(), getPlayers(), getGames(), getLocations()],
	});

	const gameNights = gameNightsQuery.data ?? [];
	const settings = settingsQuery.data;
	const players = playersQuery.data ?? [];
	const games = gamesQuery.data ?? [];
	const locations = locationsQuery.data ?? [];

	const invalidateQueries = () => {
		queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.gameNights] });
		queryClient.invalidateQueries({
			queryKey: [QUERY_KEYS.gameNightStatistics],
		});
	};

	const createMutation = useMutation({
		mutationFn: createGameNightCall,
		onSuccess() {
			successToast("game-nights:notifications.created");
			invalidateQueries();
		},
		onError: () => {
			errorToast("game-nights:notifications.create-failed");
		},
	});

	const updateMutation = useMutation({
		mutationFn: updateGameNightCall,
		onSuccess() {
			successToast("game-nights:notifications.updated");
			invalidateQueries();
		},
		onError: () => {
			errorToast("game-nights:notifications.update-failed");
		},
	});

	const deleteMutation = useMutation({
		mutationFn: deleteGameNightCall,
		onSuccess() {
			successToast("game-nights:notifications.deleted");
			invalidateQueries();
		},
		onError: () => {
			errorToast("game-nights:notifications.delete-failed");
		},
	});

	const rsvpMutation = useMutation({
		mutationFn: updateGameNightRsvpCall,
		onSuccess() {
			successToast("game-nights:notifications.rsvp-updated");
			invalidateQueries();
		},
		onError: () => {
			errorToast("game-nights:notifications.rsvp-update-failed");
		},
	});

	return {
		gameNights,
		settings,
		players,
		games,
		locations,
		isLoading:
			gameNightsQuery.isLoading ||
			settingsQuery.isLoading ||
			playersQuery.isLoading ||
			gamesQuery.isLoading ||
			locationsQuery.isLoading,
		createGameNight: createMutation.mutateAsync,
		updateGameNight: updateMutation.mutateAsync,
		deleteGameNight: deleteMutation.mutateAsync,
		updateRsvp: rsvpMutation.mutateAsync,
		isCreating: createMutation.isPending,
		isUpdating: updateMutation.isPending,
		isDeleting: deleteMutation.isPending,
	};
};
