import { useMutation, useQueries } from "@tanstack/react-query";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getLanguages, getSettings } from "@/services/queries/settings";
import { updateSettingsCall } from "@/services/settingsService";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useSettingsData = () => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [settingsQuery, languageQuery] = useQueries({
		queries: [getSettings(), getLanguages()],
	});

	const settings = settingsQuery.data;
	const languages = languageQuery.data ?? [];

	const saveSettingsMutation = useMutation({
		mutationFn: updateSettingsCall,
		onSuccess() {
			successToast("settings:save.successfull");
			void invalidator.invalidateSettings();
		},
		onError: (error) => {
			errorToast(apiErrorMessage(error, "settings:save.failed"));
		},
	});

	return {
		settings,
		languages,
		saveSettings: saveSettingsMutation.mutateAsync,
		isSaving: saveSettingsMutation.isPending,
		isLoading: settingsQuery.isLoading || languageQuery.isLoading,
	};
};
