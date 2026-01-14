import { useState, useCallback } from 'react';

import { useGameForm } from './useGameForm';

export const useImageUpload = (initialImage?: string | null) => {
  const { uploadImage } = useGameForm();
  const [poster, setPoster] = useState<File | undefined | null>(undefined);

  const handleImageUpload = useCallback(
    async (imageToUpload: File | undefined | null) => {
      if (imageToUpload !== undefined && imageToUpload !== null) {
        const savedImage = await uploadImage({ type: 1, file: imageToUpload });
        return savedImage ?? null;
      } else if (imageToUpload === null) {
        return null;
      }
      return initialImage ?? null;
    },
    [uploadImage, initialImage]
  );

  return {
    poster,
    setPoster,
    uploadPoster: handleImageUpload,
  };
};
