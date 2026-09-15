import type { Leaderboard } from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

export const getLeaderboardCall = (): Promise<Leaderboard> => {
	return axiosInstance.get<Leaderboard>("leaderboard").then((response) => response.data);
};
