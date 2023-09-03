import {ListResult, Location} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'location';

export const getLocations = (): Promise<ListResult<Location>> => {
  return axiosInstance.get<ListResult<Location>>(domain)
    .then((response) => {
      return response.data;
    });
};
