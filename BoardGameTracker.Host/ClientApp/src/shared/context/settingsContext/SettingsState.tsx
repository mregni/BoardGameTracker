import {createContext, useCallback, useEffect, useState} from 'react';

import {CreationResultType, Settings} from '../../models';
import {getSettings, saveSettings as SaveSettingsCall} from '../../services/SettingsService';

export interface SettingsContextProps {
  loading: boolean;
  failed: boolean;
  settings: Settings;
  loadSettings: () => Promise<void>;
  saveSettings: (values: Settings) => Promise<void>;
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

  const saveSettings = async (values: Settings): Promise<void> => {
    setLoading(true);
    const result = await SaveSettingsCall(values);
    if(result.type === CreationResultType.Success && result.data !== null){
      setSettings(result.data);
    }

    setLoading(false);
  }

  return {
    loading, settings, loadSettings, failed, saveSettings
  };
};
