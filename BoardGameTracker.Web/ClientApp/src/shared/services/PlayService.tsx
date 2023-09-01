import {CreationResult, Play, SearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'play';

export const AddPlay = (play: Play): Promise<CreationResult<Play>> => {
  return axiosInstance
    .post<CreationResult<Play>>(domain, { ...play })
    .then((response) => {
      return response.data;
    });
};

export const UpdatePlay = (play: Play): Promise<CreationResult<Play>> => {
  return axiosInstance
    .put<CreationResult<Play>>(domain, { ...play })
    .then((response) => {
      return response.data;
    });
};

export const deletePlay = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}

export const getPlays = (id: number, type: 'player' | 'game'): Promise<SearchResult<Play[]>> => {
  return axiosInstance
    .get<SearchResult<Play[]>>(`${type}/${id}/plays`, {
      transformResponse: (data) => {
        const obj = JSON.parse(data);
        const returnObj = {
          ...obj,
          model: obj.model.map((play: Play) => {
            return {
              ...play,
              start: new Date(play.start)
            }
          })
        };

        return returnObj;
      }
    })
    .then((response) => { 
      return response.data;
    });
}