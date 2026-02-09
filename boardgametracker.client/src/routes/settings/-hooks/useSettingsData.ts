import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';

import { updateSettingsCall } from '@/services/settingsService';
import { getEnvironment, getLanguages, getSettings } from '@/services/queries/settings';
import { QUERY_KEYS } from '@/models';

interface Props {
  onSaveSuccess?: () => void;
  onSaveError?: () => void;
}

export const useSettingsData = ({ onSaveSuccess, onSaveError }: Props) => {
  const queryClient = useQueryClient();

  const [settingsQuery, languageQuery, environmentQuery] = useQueries({
    queries: [getSettings(), getLanguages(), getEnvironment()],
  });

  const settings = settingsQuery.data;
  const languages = languageQuery.data ?? [];
  const environment = environmentQuery.data;

  const saveSettingsMutation = useMutation({
    mutationFn: updateSettingsCall,
    onSuccess() {
      onSaveSuccess?.();
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.settings] });
    },
    onError: () => {
      onSaveError?.();
    },
  });

  return {
    settings,
    languages,
    environment,
    saveSettings: saveSettingsMutation.mutateAsync,
    isLoading: saveSettingsMutation.isPending,
  };
};
