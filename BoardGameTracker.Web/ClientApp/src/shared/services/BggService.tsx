import {GameSearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'bgg';

export const addGame = (id: string, state: string): Promise<GameSearchResult> => {
  return axiosInstance
    .get<GameSearchResult>(`${domain}/add/${id}?state=${state}`)
    .then((response) => {
      return response.data;
    });
};
