import { axiosInstance } from '../../utils/axiosInstance';
import { DashboardStatistics, DashboardCharts, Result } from '../../models';

const domain = 'dashboard';

export const getStatistics = (signal: AbortSignal): Promise<Result<DashboardStatistics>> => {
  return axiosInstance.get<Result<DashboardStatistics>>(`${domain}/statistics`, { signal }).then((response) => {
    return response.data;
  });
};

export const getCharts = (signal: AbortSignal): Promise<Result<DashboardCharts>> => {
  return axiosInstance.get<Result<DashboardCharts>>(`${domain}/charts`, { signal }).then((response) => {
    return response.data;
  });
};
