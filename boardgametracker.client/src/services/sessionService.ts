import { axiosInstance } from '@/utils/axiosInstance';
import { Session, CreateSession } from '@/models';

const domain = 'session';

export const addSessionCall = (play: CreateSession): Promise<Session> => {
  return axiosInstance.post<Session>(domain, play).then((response) => {
    return response.data;
  });
};

export const deleteSessionCall = (id: string): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};

export const getSessionCall = (id: string): Promise<Session> => {
  return axiosInstance.get<Session>(`${domain}/${id}`).then((response) => {
    return response.data;
  });
};

export const updateSessionCall = (play: Session): Promise<Session> => {
  return axiosInstance.put<Session>(domain, play).then((response) => {
    return response.data;
  });
};
