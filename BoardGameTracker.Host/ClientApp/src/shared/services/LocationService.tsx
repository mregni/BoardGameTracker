import {CreationResult, FormLocation, ListResult, Location} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'location';

export const getLocations = (): Promise<ListResult<Location>> => {
  return axiosInstance.get<ListResult<Location>>(domain)
    .then((response) => {
      return response.data;
    });
};

export const AddLocation = (location: FormLocation): Promise<CreationResult<Location>> => {
  return axiosInstance
    .post<CreationResult<Location>>(domain, { ...location })
    .then((response) => {
      return response.data;
    });
};

export const UpdateLocation = (location: FormLocation): Promise<CreationResult<Location>> => {
  return axiosInstance
    .put<CreationResult<Location>>(domain, { ...location })
    .then((response) => {
      return response.data;
    });
};

export const deleteLocation = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}