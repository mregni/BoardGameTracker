import { axiosInstance } from '@/utils/axiosInstance';
import { CreateGameNight, GameNight, GameNightStatistics, UpdateGameNightRsvp } from '@/models';

const domain = 'gamenight';

export const getGameNightsCall = (): Promise<GameNight[]> => {
  return axiosInstance.get<GameNight[]>(domain).then((response) => {
    return response.data;
  });
};

export const getGameNightStatisticsCall = (): Promise<GameNightStatistics> => {
  return axiosInstance.get<GameNightStatistics>(`${domain}/statistics`).then((response) => {
    return response.data;
  });
};

export const createGameNightCall = (gameNight: CreateGameNight): Promise<GameNight> => {
  return axiosInstance.post<GameNight>(domain, { ...gameNight }).then((response) => {
    return response.data;
  });
};

export const updateGameNightCall = (gameNight: GameNight): Promise<GameNight> => {
  return axiosInstance.put<GameNight>(domain, { ...gameNight }).then((response) => {
    return response.data;
  });
};

export const deleteGameNightCall = (id: number): Promise<void> => {
  return axiosInstance.delete<void>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const updateGameNightRsvpCall = (rsvp: UpdateGameNightRsvp): Promise<GameNight> => {
  return axiosInstance.put<GameNight>(`${domain}/rsvp`, { ...rsvp }).then((response) => {
    return response.data;
  });
};
