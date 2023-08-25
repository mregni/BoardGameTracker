import {Game, GameStatistics, ListResult, Play, SearchResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'game';

export const getAllGames = (): Promise<ListResult<Game>> => {
  return axiosInstance
    .get<ListResult<Game>>(domain)
    .then((response) => {
      return response.data;
    });
};

export const getGame = (id: string): Promise<SearchResult<Game>> => {
  return axiosInstance
    .get<SearchResult<Game>>(`${domain}/${id}`)
    .then((response) => {
      return response.data;
    });
}

export const getGamePlays = (id: number, skip: number, take: number): Promise<SearchResult<Play[]>> => {
  return axiosInstance
    .get<SearchResult<Play[]>>(`${domain}/${id}/plays`, {params: {skip, take},
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

export const getGameStatistics = (id: number): Promise<SearchResult<GameStatistics>> => {
  return axiosInstance
    .get<SearchResult<GameStatistics>>(`${domain}/${id}/stats`)
    .then((response) => {
      return response.data;
    });
}

export const deleteGameCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
}