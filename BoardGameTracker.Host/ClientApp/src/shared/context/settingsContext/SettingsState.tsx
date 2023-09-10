import {createContext, useCallback, useEffect, useState} from 'react';

import {Settings} from '../../models';
import {getSettings} from '../../services/SettingsService';

export interface SettingsContextProps {
  loading: boolean;
  failed: boolean;
  settings: Settings;
  loadSettings: () => Promise<void>;
}

export const SettingsContext = createContext<SettingsContextProps>(null!);

export const useSettingsContext = (): SettingsContextProps => {
  const [loading, setLoading] = useState(true);
  const [settings, setSettings] = useState<Settings>(null!);
  const [failed, setFailed] = useState(false);

  const loadSettings = useCallback(async (): Promise<void> => {
    setLoading(true);
    getSettings()
      .then((result) => {
        setSettings(result);
        setLoading(false);
      })
      .catch(() => {
        setFailed(true);
        setLoading(false);
      });

  }, []);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  return {
    loading, settings, loadSettings, failed
  };
};
