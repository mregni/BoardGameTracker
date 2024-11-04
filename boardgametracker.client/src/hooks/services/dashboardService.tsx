import { axiosInstance } from '../../utils/axiosInstance';
import { DashboardStatistics, DashboardCharts } from '../../models';

const domain = 'dashboard';

export const getStatistics = (signal: AbortSignal): Promise<DashboardStatistics> => {
  return axiosInstance.get<DashboardStatistics>(`${domain}/statistics`, { signal }).then((response) => {
    return response.data;
  });
};

export const getCharts = (signal: AbortSignal): Promise<DashboardCharts> => {
  return axiosInstance.get<DashboardCharts>(`${domain}/charts`, { signal }).then((response) => {
    return response.data;
  });
};
