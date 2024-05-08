import { Result } from '../../models';
import { axiosInstance } from '../../utils/axiosInstance';
import { ImageUpload } from '../useImages';

const domain = 'image';

export const uploadImages = async (data: ImageUpload): Promise<Result<string>> => {
  const formData = new FormData();
  if (data.file) {
    formData.append('file', data.file);
  }
  formData.append('type', data.type.toString());

  const response = await axiosInstance.post<Result<string>>(domain, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response.data;
};
