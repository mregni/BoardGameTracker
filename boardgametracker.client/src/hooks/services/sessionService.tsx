import { axiosInstance } from '../../utils/axiosInstance';
import { CreateSession } from '../../models/Session/CreateSession';
import { Session } from '../../models';

const domain = 'session';

export const addSession = (play: CreateSession): Promise<Session> => {
  return axiosInstance.post<Session>(domain, play).then((response) => {
    return response.data;
  });
};

export const deleteSessionCall = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const getSession = (id: string, signal: AbortSignal): Promise<Session> => {
  return axiosInstance.get<Session>(`${domain}/${id}`, { signal }).then((response) => {
    return response.data;
  });
};

export const updateSession = (play: Session): Promise<Session> => {
  return axiosInstance.put<Session>(domain, play).then((response) => {
    return response.data;
  });
};
