import { axiosInstance } from '../utils/axiosInstance';
import { KeyValuePair } from '../models';

export const getCountsCall = (): Promise<KeyValuePair<string, number>[]> => {
  return axiosInstance.get<KeyValuePair<string, number>[]>('count').then((response) => {
    return response.data;
  });
};
