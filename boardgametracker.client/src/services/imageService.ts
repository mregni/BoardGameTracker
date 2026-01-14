import { axiosInstance } from '../utils/axiosInstance';

import { ImageUpload } from '@/models';

const domain = 'image';

export const uploadImageCall = async (data: ImageUpload): Promise<string> => {
  const formData = new FormData();
  if (data.file) {
    formData.append('file', data.file);
  }
  formData.append('type', data.type.toString());

  const response = await axiosInstance.post<string>(domain, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response.data;
};
