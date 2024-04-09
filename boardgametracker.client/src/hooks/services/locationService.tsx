import {ListResult, Location} from '../../models';
import {axiosInstance} from '../../utils/axiosInstance';

const domain = 'location';

export const getLocations = (signal: AbortSignal): Promise<ListResult<Location>> => {
  return axiosInstance.get<ListResult<Location>>(domain, { signal })
    .then((response) => {
      return response.data;
    });
};