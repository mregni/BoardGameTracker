import { axiosInstance } from '../../utils/axiosInstance';
import { CreateSession } from '../../models/Session/CreateSession';
import { Session, Result } from '../../models';

const domain = 'session';

export const addSession = (play: CreateSession): Promise<Result<Session>> => {
  return axiosInstance.post<Result<Session>>(domain, play).then((response) => {
    return response.data;
  });
};

export const deleteSession = (id: number): Promise<void> => {
  return axiosInstance.delete(`${domain}/${id}`);
};
