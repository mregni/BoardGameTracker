import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getEnvironment, getLanguages, getSettings } from "@/services/queries/settings";
import { updateSettingsCall } from "@/services/settingsService";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useSettingsData = () => {
	const queryClient = useQueryClient();
	const { successToast } = useToasts();

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
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.settings] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] });
			queryClient.invalidateQueries({
				predicate: (query) => query.queryKey[0] === QUERY_KEYS.game && query.queryKey[2] === QUERY_KEYS.price,
			});
		},
		onError: (error) => {
			toast.error(apiErrorMessage(error, "settings:save.failed"));
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
