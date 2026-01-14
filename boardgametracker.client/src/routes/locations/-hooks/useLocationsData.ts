import { useQueries, useQueryClient } from '@tanstack/react-query';

import { getLocations } from '@/services/queries/locations';
import { deleteLocationCall } from '@/services/locationService';
import { QUERY_KEYS } from '@/models';

interface Props {
  onDeleteSuccess?: () => void;
  onDeleteError?: () => void;
}

export const useLocationsData = ({ onDeleteSuccess, onDeleteError }: Props) => {
  const queryClient = useQueryClient();

  const [locationsQuery] = useQueries({
    queries: [getLocations()],
  });

  const locations = locationsQuery.data ?? [];

  const deleteLocation = async (id: number) => {
    try {
      await deleteLocationCall(id);
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] });
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] });
      onDeleteSuccess?.();
    } catch {
      onDeleteError?.();
    }
  };

  return {
    locations,
    deleteLocation,
  };
};
