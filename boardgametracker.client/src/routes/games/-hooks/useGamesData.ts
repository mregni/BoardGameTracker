import { useQuery } from "@tanstack/react-query";

import { getGames } from "@/services/queries/games";

export const useGamesData = () => {
	const { data, isLoading, isError, refetch } = useQuery(getGames());

	return {
		isLoading,
		isError: isError && data === undefined,
		retry: refetch,
		games: data ?? [],
	};
};
