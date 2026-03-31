import { useQueries } from "@tanstack/react-query";
import { getGames } from "@/services/queries/games";
import { getLocations } from "@/services/queries/locations";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";

export const useSessionForm = () => {
	const [settingsQuery, locationQuery, gamesQuery, playersQuery] = useQueries({
		queries: [getSettings(), getLocations(), getGames(), getPlayers()],
	});

	const settings = settingsQuery.data;
	const locations = locationQuery.data ?? [];
	const games = gamesQuery.data ?? [];
	const players = playersQuery.data ?? [];

	const isLoading = settingsQuery.isLoading || locationQuery.isLoading || gamesQuery.isLoading;

	return {
		isLoading,
		settings,
		locations,
		games,
		players,
	};
};
