import { AxiosError } from 'axios';

import { useMutation } from '@tanstack/react-query';

import { FailResult, Result } from '../models';
import { uploadImages } from './services/imageService';

export interface RemoteImages {
  isPending: boolean;
  uploadPlayerImage: (file: File | undefined) => Promise<Result<string> | undefined>;
}

export interface ImageUpload {
  type: number;
  file: File | undefined;
}

export const useImages = (): RemoteImages => {
  const { mutateAsync, isPending } = useMutation<Result<string>, AxiosError<FailResult>, ImageUpload>({
    mutationFn: uploadImages,
  });

  const uploadPlayerImage = (file: File | undefined): Promise<Result<string> | undefined> => {
    return mutateAsync({ type: 0, file });
  };

  return {
    isPending,
    uploadPlayerImage,
  };
};
