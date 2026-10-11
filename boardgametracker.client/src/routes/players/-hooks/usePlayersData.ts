import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { getPlayersOverview } from "@/services/queries/players";
import { sortByActivity } from "@/utils/sortUtils";

export const usePlayersData = () => {
	const { data, isLoading } = useQuery(getPlayersOverview());

	const players = useMemo(() => sortByActivity(data ?? []), [data]);

	return {
		players,
		isLoading,
	};
};
