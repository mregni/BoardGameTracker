import { Result, Settings } from '../../models';
import { axiosInstance } from '../../utils/axiosInstance';

export const getSettings = (signal: AbortSignal): Promise<Result<Settings>> => {
  return axiosInstance.get<Result<Settings>>('settings', { signal }).then((response) => {
    return response.data;
  });
};
