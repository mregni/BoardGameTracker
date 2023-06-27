import {GameSearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'bgg';

export const searchGame = (id: string): Promise<GameSearchResult> => {
  return axiosInstance
    .get<GameSearchResult>(`${domain}/search/${id}`)
    .then((response) => {
      return response.data;
    });
};
