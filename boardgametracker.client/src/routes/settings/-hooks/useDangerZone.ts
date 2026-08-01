import { useMutation } from "@tanstack/react-query";
import { useAuth } from "@/hooks/useAuth";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import { useToasts } from "@/routes/-hooks/useToasts";
import { factoryResetCall, resetDatabaseCall } from "@/services/maintenanceService";

export const useDangerZone = () => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();
	const clearAuth = useAuth((s) => s.clearAuth);

	const resetMutation = useMutation({
		mutationFn: resetDatabaseCall,
		onSuccess: async () => {
			successToast("settings:advanced.danger.notifications.reset-success");
			await invalidator.invalidateAll();
		},
		onError: () => {
			errorToast("settings:advanced.danger.notifications.reset-failed");
		},
	});

	const factoryResetMutation = useMutation({
		mutationFn: factoryResetCall,
		onSuccess: () => {
			clearAuth();
			window.location.href = "/login";
		},
		onError: () => {
			errorToast("settings:advanced.danger.notifications.factory-reset-failed");
		},
	});

	return {
		resetDatabase: resetMutation.mutateAsync,
		isResetting: resetMutation.isPending,
		factoryReset: factoryResetMutation.mutateAsync,
		isFactoryResetting: factoryResetMutation.isPending,
	};
};
