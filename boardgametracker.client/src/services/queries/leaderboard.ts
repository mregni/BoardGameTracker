import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getLeaderboardCall } from "../leaderboardService";

export const getLeaderboard = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.leaderboard],
		queryFn: () => getLeaderboardCall(),
	});
