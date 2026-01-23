import { axiosInstance } from '../utils/axiosInstance';
import { DashboardStatistics } from '../models';

const domain = 'dashboard';

export const getStatistics = (): Promise<DashboardStatistics> => {
  return axiosInstance.get<DashboardStatistics>(`${domain}/statistics`).then((response) => {
    return response.data;
  });
};
