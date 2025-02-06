import { axiosInstance } from '../../utils/axiosInstance';
import { CreateSession } from '../../models/Session/CreateSession';
import { Session } from '../../models';

const domain = 'session';

export const addSession = (play: CreateSession): Promise<Session> => {
  return axiosInstance.post<Session>(domain, play).then((response) => {
    return response.data;
  });
};

export const deleteSessionCall = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
