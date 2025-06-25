import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { Location, QUERY_KEYS } from '../models';

import { addLocationCall, getLocationsCall, deleteLocationCall, updateLocationCall } from './locationService';

interface Props {
  onDeleteSuccess?: () => void;
  onDeleteFailed?: () => void;
  onEditSuccess?: () => void;
  onEditFailed?: () => void;
  onNewSuccess?: () => void;
  onNewFailed?: () => void;
}

export const useLocations = (props: Props) => {
  const { onDeleteSuccess, onDeleteFailed, onEditSuccess, onEditFailed, onNewSuccess, onNewFailed } = props;
  const queryClient = useQueryClient();

  const { data, refetch } = useQuery<Location[]>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: () => getLocationsCall(),
  });

  const { mutateAsync: save, isPending: isSaving } = useMutation({
    mutationFn: addLocationCall,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await refetch();
      await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
    },
    onSuccess() {
      onNewSuccess?.();
    },
    onError: () => {
      onNewFailed?.();
    },
  });

  const { mutateAsync: update, isPending: isUpdating } = useMutation({
    mutationFn: updateLocationCall,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: [QUERY_KEYS.locations] });
    },
    onSettled: async () => {
      await refetch();
    },
    onSuccess() {
      onEditSuccess?.();
    },
    onError: () => {
      onEditFailed?.();
    },
  });

  const byId = (id: string | null): Location | null => {
    if (id === null) return null;
    if (data === undefined) return null;

    const index = data.findIndex((x) => x.id === id);
    if (index !== -1) {
      return data[index];
    }

    return null;
  };

  const deleteLocation = (id: string) => {
    void deleteLocationCall(id)
      .then(() => {
        refetch();
        queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
        onDeleteSuccess?.();
      })
      .catch(() => {
        onDeleteFailed?.();
      });
  };

  return {
    locations: data ?? [],
    byId,
    save,
    isSaving,
    deleteLocation,
    update,
    isUpdating,
  };
};
