import {KeyValuePair, Result} from '../../models';
import {axiosInstance} from '../../utils/axiosInstance';

export const getCounts = (signal: AbortSignal): Promise<Result<KeyValuePair<string, number>[]>> => {
  return axiosInstance
    .get<Result<KeyValuePair<string, number>[]>>('count', { signal })
    .then((response) => {
      return response.data;
    });
};
