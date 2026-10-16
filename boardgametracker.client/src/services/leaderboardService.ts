import type { Leaderboard, LeaderboardParams } from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

export const getLeaderboardCall = (params: LeaderboardParams): Promise<Leaderboard> => {
	return axiosInstance.get<Leaderboard>("leaderboard", { params }).then((response) => response.data);
};
