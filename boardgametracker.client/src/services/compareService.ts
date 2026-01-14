import { axiosInstance } from '@/utils/axiosInstance';
import { CompareResult } from '@/models';

const domain = 'compare';

export const getCompareCall = (playerOne: number, playerTwo: number): Promise<CompareResult> => {
  return axiosInstance.get<CompareResult>(`${domain}/${playerOne}/${playerTwo}`).then((response) => {
    return response.data;
  });
};
