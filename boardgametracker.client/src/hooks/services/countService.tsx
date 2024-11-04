import { axiosInstance } from '../../utils/axiosInstance';
import { KeyValuePair } from '../../models';

export const getCounts = (signal: AbortSignal): Promise<KeyValuePair<string, number>[]> => {
  return axiosInstance.get<KeyValuePair<string, number>[]>('count', { signal }).then((response) => {
    return response.data;
  });
};
