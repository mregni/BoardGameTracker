import { useQuery } from "@tanstack/react-query";
import { getPlayersOverview } from "@/services/queries/players";

export const usePlayersData = () => {
	const { data, isLoading, isError, refetch } = useQuery(getPlayersOverview());

	return {
		players: data ?? [],
		isLoading,
		isError,
		refetch,
	};
};
