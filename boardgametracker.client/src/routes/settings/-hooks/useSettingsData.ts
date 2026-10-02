import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getEnvironment, getLanguages, getSettings } from "@/services/queries/settings";
import { updateSettingsCall } from "@/services/settingsService";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useSettingsData = () => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery, languageQuery, environmentQuery] = useQueries({
		queries: [getSettings(), getLanguages(), getEnvironment()],
	});

	const settings = settingsQuery.data;
	const languages = languageQuery.data ?? [];
	const environment = environmentQuery.data;

	const saveSettingsMutation = useMutation({
		mutationFn: updateSettingsCall,
		onSuccess() {
			successToast("settings:save.successfull");
			invalidator.invalidateSettings();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "settings:save.failed"));
		},
	});

	return {
		settings,
		languages,
		environment,
		saveSettings: saveSettingsMutation.mutateAsync,
		isSaving: saveSettingsMutation.isPending,
		isLoading: settingsQuery.isLoading || languageQuery.isLoading || environmentQuery.isLoading,
	};
};
