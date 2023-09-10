import {CreationResult} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'image';

export const uploadImage = (file: File | null, type: number): Promise<CreationResult<string>> => {
  const formData = new FormData();
  if(file !== null){
    formData.append('file', file);
  }
  formData.append('type', type.toString());

  return axiosInstance
    .post<CreationResult<string>>(domain, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
    .then((response) => {
      return response.data;
    });
};
