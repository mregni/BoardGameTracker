import {Settings} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'settings';

export const getSettings = (): Promise<Settings> => {
  return axiosInstance
    .get<Settings>(domain)
    .then((response) => {
      return response.data;
    });
};

