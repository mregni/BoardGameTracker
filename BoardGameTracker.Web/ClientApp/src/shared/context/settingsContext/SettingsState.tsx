import {createContext, useCallback, useEffect, useState} from 'react';

import {Settings} from '../../models';
import {getSettings} from '../../services/SettingsService';

export interface SettingsContextProps {
  loading: boolean;
  settings: Settings;
  loadSettings: () => Promise<void>;
}

export const SettingsContext = createContext<SettingsContextProps>(null!);

export const useSettingsContext = (): SettingsContextProps => {
  const [loading, setLoading] = useState(false);
  const [settings, setSettings] = useState<Settings>(null!);

  const loadSettings = useCallback(async (): Promise<void> => {
    setLoading(true);
    const result = await getSettings();
    setSettings(result);
    setLoading(false);
  }, []);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  return {
    loading, settings, loadSettings
  };
};
