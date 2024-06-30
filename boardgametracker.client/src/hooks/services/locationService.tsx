import { axiosInstance } from '../../utils/axiosInstance';
import { CreateLocation, ListResult, Location, Result } from '../../models';

const domain = 'location';

export const getLocations = (signal: AbortSignal): Promise<ListResult<Location>> => {
  return axiosInstance.get<ListResult<Location>>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const addLocation = (location: CreateLocation): Promise<Result<Location>> => {
  return axiosInstance.post<Result<Location>>(domain, location).then((response) => {
    return response.data;
  });
};
