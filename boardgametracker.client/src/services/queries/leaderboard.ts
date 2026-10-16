import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import { type LeaderboardParams, QUERY_KEYS } from "@/models";
import { getLeaderboardCall } from "../leaderboardService";

export const getLeaderboard = (params: LeaderboardParams) =>
	queryOptions({
		queryKey: [QUERY_KEYS.leaderboard, params],
		queryFn: () => getLeaderboardCall(params),
		placeholderData: keepPreviousData,
	});
