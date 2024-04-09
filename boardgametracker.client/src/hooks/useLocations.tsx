import {useQuery} from '@tanstack/react-query';

import {ListResult, Location, QUERY_KEYS} from '../models';
import {getLocations} from './services/locationService';

export interface Props {
  location: Location[] | undefined
  byId: (id: number) => Location | null;
}

export const useLocations = (): Props => {
  const { data } = useQuery<ListResult<Location>>({
    queryKey: [QUERY_KEYS.locations],
    queryFn: ({ signal }) => getLocations(signal)
  });

  const byId = (id: number): Location | null => {
    if (data === undefined)
      return null;

    const index = data.list.findIndex(x => x.id === id);
    if (index !== -1) {
      return data.list[index];
    }

    return null;
  }

  return {
    location: data?.list,
    byId
  }
}

