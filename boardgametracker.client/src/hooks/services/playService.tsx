import { axiosInstance } from '../../utils/axiosInstance';
import { CreatePlay } from '../../models/Plays/CreatePlay';
import { Play, Result } from '../../models';

const domain = 'play';

export const addPlay = (play: CreatePlay): Promise<Result<Play>> => {
  return axiosInstance.post<Result<Play>>(domain, play).then((response) => {
    return response.data;
  });
};

export const deletePlay = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
