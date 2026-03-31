import type { DashboardStatistics } from "../models";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "dashboard";

export const getStatistics = (): Promise<DashboardStatistics> => {
	return axiosInstance.get<DashboardStatistics>(`${domain}/statistics`).then((response) => {
		return response.data;
	});
};
