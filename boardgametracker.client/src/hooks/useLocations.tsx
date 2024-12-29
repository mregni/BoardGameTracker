import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { CreateLocation, FailResult, Location, QUERY_KEYS } from '../models';

import { addLocation, getLocations } from './services/locationService';

export const useLocations = () => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const { data, refetch } = useQuery<Location[]>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: ({ signal }) => getLocations(signal),
  });

  const { mutateAsync: save, isPending: isSaving } = useMutation<Location, AxiosError<FailResult>, CreateLocation>({
    mutationFn: addLocation,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await refetch();
    },
    onSuccess(data) {
      const previousLocations = queryClient.getQueryData<Location[]>([QUERY_KEYS.locations]);

      if (previousLocations !== undefined) {
        previousLocations.length = previousLocations.length + 1;
        queryClient.setQueryData([QUERY_KEYS.locations], [...previousLocations, data]);
      }

      showInfoToast('location.notifications.created');
    },
    onError: () => {
      showErrorToast('location.notifications.failed');
    },
  });

  const byId = (id: number): Location | null => {
    if (data === undefined) return null;

    const index = data.findIndex((x) => x.id === id);
    if (index !== -1) {
      return data[index];
    }

    return null;
  };

  return {
    locations: data ?? [],
    byId,
    save,
    isSaving,
  };
};
