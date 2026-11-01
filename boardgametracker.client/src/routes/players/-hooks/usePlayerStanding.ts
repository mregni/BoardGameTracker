import { useQuery } from "@tanstack/react-query";
import { toLeaderboardParams } from "@/routes/leaderboard/-utils/leaderboardSearch";
import { getLeaderboard } from "@/services/queries/leaderboard";

export const usePlayerStanding = (playerId: number) => {
	const { data } = useQuery(getLeaderboard(toLeaderboardParams({})));
	const entry = data?.players.find((player) => player.playerId === playerId);

	if (!data || !entry || entry.playCount === 0) return null;

	return {
		rank: entry.rank,
		isRanked: entry.isRanked,
		rankedCount: data.rankedCount,
		sessionsToRank: entry.sessionsToRank,
	};
};
