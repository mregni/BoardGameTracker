import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import type { PlayerOverview } from "@/models";
import { getPlayersOverview } from "@/services/queries/players";

export const sortByActivity = (players: PlayerOverview[]): PlayerOverview[] =>
	[...players].sort((a, b) => b.playCount - a.playCount || a.name.localeCompare(b.name));

export const usePlayersData = () => {
	const { data, isLoading } = useQuery(getPlayersOverview());

	const players = useMemo(() => sortByActivity(data ?? []), [data]);

	return {
		players,
		isLoading,
	};
};
