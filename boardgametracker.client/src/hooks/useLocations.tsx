import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useToast } from '../providers/BgtToastProvider';
import { CreateLocation, FailResult, ListResult, Location, QUERY_KEYS, Result } from '../models';

import { addLocation, getLocations } from './services/locationService';

export interface Props {
  locations: Location[] | undefined;
  byId: (id: number) => Location | null;
  save: (location: CreateLocation) => Promise<Result<Location>>;
  isSaving: boolean;
}

export const useLocations = (): Props => {
  const queryClient = useQueryClient();
  const { showInfoToast, showErrorToast } = useToast();

  const { data, refetch } = useQuery<ListResult<Location>>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: ({ signal }) => getLocations(signal),
  });

  const { mutateAsync: save, isPending: isSaving } = useMutation<Result<Location>, AxiosError<FailResult>, CreateLocation>({
    mutationFn: addLocation,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await refetch();
    },
    onSuccess(data) {
      const previousLocations = queryClient.getQueryData<ListResult<Location>>([QUERY_KEYS.locations]);

      if (previousLocations !== undefined) {
        previousLocations.count = previousLocations.count + 1;
        previousLocations.list = [...previousLocations.list, data.model];
        queryClient.setQueryData([QUERY_KEYS.locations], previousLocations);
      }

      showInfoToast('location.notifications.created');
    },
    onError: () => {
      showErrorToast('location.notifications.failed');
    },
  });

  const byId = (id: number): Location | null => {
    if (data === undefined) return null;

    const index = data.list.findIndex((x) => x.id === id);
    if (index !== -1) {
      return data.list[index];
    }

    return null;
  };

  return {
    locations: data?.list,
    byId,
    save,
    isSaving,
  };
};
