import { AxiosError } from 'axios';
import { useMutation } from '@tanstack/react-query';

import { uploadImages } from './services/imageService';

import { FailResult } from '@/models';

export interface RemoteImages {
  isPending: boolean;
  uploadPlayerImage: (file: File | undefined) => Promise<string | undefined>;
}

export interface ImageUpload {
  type: number;
  file: File | undefined;
}

export const useImages = (): RemoteImages => {
  const { mutateAsync, isPending } = useMutation<string, AxiosError<FailResult>, ImageUpload>({
    mutationFn: uploadImages,
  });

  const uploadPlayerImage = (file: File | undefined): Promise<string | undefined> => {
    return mutateAsync({ type: 0, file });
  };

  return {
    isPending,
    uploadPlayerImage,
  };
};
