import { axiosInstance } from '@/utils/axiosInstance';
import { Badge } from '@/models';

const domain = 'badge';

export const getAllBadgesCall = (): Promise<Badge[]> => {
  return axiosInstance.get<Badge[]>(`${domain}`).then((response) => {
    return response.data;
  });
};
