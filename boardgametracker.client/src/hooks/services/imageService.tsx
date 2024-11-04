import { ImageUpload } from '../useImages';
import { axiosInstance } from '../../utils/axiosInstance';

const domain = 'image';

export const uploadImages = async (data: ImageUpload): Promise<string> => {
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
