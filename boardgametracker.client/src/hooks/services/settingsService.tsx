import { axiosInstance } from '../../utils/axiosInstance';
import { Settings } from '../../models';

export const getSettings = (signal: AbortSignal): Promise<Settings> => {
  return axiosInstance.get<Settings>('settings', { signal }).then((response) => {
    return response.data;
  });
};
