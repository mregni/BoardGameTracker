import { AxiosError } from 'axios';
import { useMutation } from '@tanstack/react-query';

import { uploadImages } from './services/imageService';

import { FailResult } from '@/models';

export interface ImageUpload {
  type: number;
  file: File | undefined;
}

export const useImages = () => {
  const { mutateAsync, isPending } = useMutation<string, AxiosError<FailResult>, ImageUpload>({
    mutationFn: uploadImages,
  });

  const uploadPlayerImage = (file: File | undefined): Promise<string | undefined> => {
    return mutateAsync({ type: 0, file });
  };

  const uploadGameImage = (file: File | undefined): Promise<string | undefined> => {
    return mutateAsync({ type: 1, file });
  };

  return {
    isPending,
    uploadPlayerImage,
    uploadGameImage,
  };
};
