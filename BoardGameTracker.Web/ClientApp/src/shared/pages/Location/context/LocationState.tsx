import {App} from 'antd';
import {createContext, useCallback, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {CreationResultType, FormLocation, Location} from '../../../models';
import {
  AddLocation as AddLocationCall, deleteLocation as deleteLocationCall, getLocations,
  UpdateLocation as UpdateLocationCall,
} from '../../../services/LocationService';
import {createErrorNotification, createInfoNotification} from '../../../utils';

export interface LocationContextProps {
  loading: boolean;
  locations: Location[];
  loadLocations: () => Promise<void>;
  deleteLocation: (id: number) => Promise<void>;
  addLocation: (location: FormLocation) => Promise<Location | null>;
  updateLocation: (location: FormLocation) => Promise<void>;
}

export const LocationContext = createContext<LocationContextProps>(null!);

export const useLocationContext = (): LocationContextProps => {
  const [loading, setLoading] = useState(false);
  const [locations, setLocations] = useState<Location[]>([]);
  const { t } = useTranslation();
  const { notification } = App.useApp();

  const loadLocations = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getLocations();
    setLocations(result.list);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadLocations();
  }, [loadLocations]);

  const deleteLocation = async (id: number): Promise<void> => {
    await deleteLocationCall(id);
    createInfoNotification(
      notification,
      t('location.deleted.title'),
      t('location.deleted.description')
    );
    await loadLocations();
  }

  const addLocation = async (location: FormLocation): Promise<Location | null> => {
    const result = await AddLocationCall(location);
    if (result.type === CreationResultType.Success) {
      createInfoNotification(
        notification,
        t('location.new.notification.title'),
        t('location.new.notification.description')
      );
    } else {
      createErrorNotification(
        notification,
        t('location.new.notification.title-failed'),
        t('location.new.notification.description-failed')
      );
    }
    await loadLocations();
    return result.data;
  }

  const updateLocation = async (location: FormLocation): Promise<void> => {
    const result = await UpdateLocationCall(location);
    if (result.type === CreationResultType.Success) {
      createInfoNotification(
        notification,
        t('location.update.notification.title'),
        t('location.update.notification.description')
      );
    } else {
      createErrorNotification(
        notification,
        t('location.update.notification.title-failed'),
        t('location.update.notification.description-failed')
      );
    }
    await loadLocations();
  }

  return {
    loading, locations, deleteLocation, addLocation, updateLocation, loadLocations
  };
};