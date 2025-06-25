import { useMemo } from 'react';
import { useMutation, useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { uploadImageCall } from '@/services/imageService';

export const useGameForm = () => {
  const [settingsQuery] = useQueries({
    queries: [getSettings()],
  });

  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);

  const uploadImageMutation = useMutation({
    mutationFn: uploadImageCall,
  });

  return {
    isLoading: settingsQuery.isLoading || uploadImageMutation.isPending,
    uploadImage: uploadImageMutation.mutateAsync,
    settings,
  };
};
