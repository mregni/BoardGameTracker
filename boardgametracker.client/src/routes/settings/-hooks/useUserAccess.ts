import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import {
	disableUserCall,
	enableUserCall,
	getUserExternalLoginsCall,
	manageRoleByIdentityProviderCall,
	unlinkUserExternalLoginCall,
} from "@/services/authService";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useUserAccess = (userId: string) => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const loginsQuery = useQuery({
		queryKey: [QUERY_KEYS.users, userId, QUERY_KEYS.externalLogins],
		queryFn: () => getUserExternalLoginsCall(userId),
	});

	const refresh = () => queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.users] });

	const disableMutation = useMutation({
		mutationFn: () => disableUserCall(userId),
		onSuccess: async () => {
			successToast("settings:account.notifications.user-disabled");
			await refresh();
		},
		onError: (error) => errorToast(apiErrorMessage(error, "settings:account.notifications.user-disable-failed")),
	});

	const enableMutation = useMutation({
		mutationFn: () => enableUserCall(userId),
		onSuccess: async () => {
			successToast("settings:account.notifications.user-enabled");
			await refresh();
		},
		onError: (error) => errorToast(apiErrorMessage(error, "settings:account.notifications.user-enable-failed")),
	});

	const unlinkMutation = useMutation({
		mutationFn: (loginId: number) => unlinkUserExternalLoginCall(userId, loginId),
		onSuccess: async () => {
			successToast("settings:account.notifications.external-login-unlinked");
			await refresh();
		},
		onError: (error) =>
			errorToast(apiErrorMessage(error, "settings:account.notifications.external-login-unlink-failed")),
	});

	const manageRoleMutation = useMutation({
		mutationFn: () => manageRoleByIdentityProviderCall(userId),
		onSuccess: async () => {
			successToast("settings:account.notifications.role-managed");
			await refresh();
		},
		onError: (error) => errorToast(apiErrorMessage(error, "settings:account.notifications.role-manage-failed")),
	});

	return {
		logins: loginsQuery.data ?? [],
		isLoading: loginsQuery.isLoading,
		disable: disableMutation.mutate,
		enable: enableMutation.mutate,
		unlink: unlinkMutation.mutate,
		manageRoleByIdentityProvider: manageRoleMutation.mutate,
		isBusy:
			disableMutation.isPending || enableMutation.isPending || unlinkMutation.isPending || manageRoleMutation.isPending,
	};
};
