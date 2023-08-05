import {CreationResult, Play} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'play';

export const AddPlay = (play: Play): Promise<CreationResult<Play>> => {
  return axiosInstance
    .post<CreationResult<Play>>(domain, {...play})
    .then((response) => {
      return response.data;
    });
};
