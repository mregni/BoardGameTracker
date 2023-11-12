import {createContext, useCallback, useEffect, useState} from 'react';

import {CreationResultType, Environment, Settings} from '../../models';
import {
  getEnvironment, getSettings, saveSettings as SaveSettingsCall,
} from '../../services/SettingsService';

export interface SettingsContextProps {
  loading: boolean;
  failed: boolean;
  settings: Settings;
  environment: Environment;
  loadSettings: () => Promise<void>;
  saveSettings: (values: Settings) => Promise<void>;
}

export const SettingsContext = createContext<SettingsContextProps>(null!);

export const useSettingsContext = (): SettingsContextProps => {
  const [loading, setLoading] = useState(true);
  const [settings, setSettings] = useState<Settings>(null!);
  const [environment, setEnvironment] = useState<Environment>(null!)
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

      getEnvironment()
      .then((result) => {
        setEnvironment(result);
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
    loading, settings, loadSettings, failed, saveSettings, environment
  };
};
