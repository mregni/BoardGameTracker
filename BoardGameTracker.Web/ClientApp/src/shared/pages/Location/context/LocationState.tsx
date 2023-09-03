import {createContext, useCallback, useEffect, useState} from 'react';

import {Location} from '../../../models';
import {getLocations} from '../../../services/LocationService';

export interface LocationContextProps {
  loading: boolean;
  locations: Location[];
}

export const LocationContext = createContext<LocationContextProps>(null!);

export const useLocationContext = (): LocationContextProps => {
  const [loading, setLoading] = useState(false);
  const [locations, setLocations] = useState<Location[]>([]);

  const loadLocation = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getLocations();
    setLocations(result.list);
    setLoading(false);
  }, []);
  
  useEffect(() => {
    loadLocation();
  }, [loadLocation]);

  return {
    loading, locations
  };
};
