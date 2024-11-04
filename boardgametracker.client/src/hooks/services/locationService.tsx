import { axiosInstance } from '../../utils/axiosInstance';
import { CreateLocation, Location } from '../../models';

const domain = 'location';

export const getLocations = (signal: AbortSignal): Promise<Location[]> => {
  return axiosInstance.get<Location[]>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const addLocation = (location: CreateLocation): Promise<Location> => {
  return axiosInstance.post<Location>(domain, location).then((response) => {
    return response.data;
  });
};
