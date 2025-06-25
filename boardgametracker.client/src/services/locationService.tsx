import { axiosInstance } from '../utils/axiosInstance';
import { CreateLocation, Location } from '../models';

const domain = 'location';

export const getLocationsCall = (): Promise<Location[]> => {
  return axiosInstance.get<Location[]>(domain).then((response) => {
    return response.data;
  });
};

export const addLocationCall = (location: CreateLocation): Promise<Location> => {
  return axiosInstance.post<Location>(domain, location).then((response) => {
    return response.data;
  });
};

export const deleteLocationCall = (id: string): Promise<void> => {
  return axiosInstance.delete<void>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const updateLocationCall = (location: Location): Promise<Location> => {
  return axiosInstance.put<Location>(domain, location).then((response) => {
    return response.data;
  });
};
