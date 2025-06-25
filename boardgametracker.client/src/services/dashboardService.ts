import { axiosInstance } from '../utils/axiosInstance';
import { DashboardStatistics, DashboardCharts } from '../models';

const domain = 'dashboard';

export const getStatistics = (): Promise<DashboardStatistics> => {
  return axiosInstance.get<DashboardStatistics>(`${domain}/statistics`).then((response) => {
    return response.data;
  });
};

export const getCharts = (): Promise<DashboardCharts> => {
  return axiosInstance.get<DashboardCharts>(`${domain}/charts`).then((response) => {
    return response.data;
  });
};
