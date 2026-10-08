import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { startOidcLinkCall, unlinkExternalLoginCall } from "@/services/authService";
import { getExternalLogins, getPublicOidcProvider } from "@/services/queries/oidc";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useExternalLogins = () => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();

	const loginsQuery = useQuery(getExternalLogins());
	const providerQuery = useQuery(getPublicOidcProvider());

	const unlinkMutation = useMutation({
		mutationFn: (id: number) => unlinkExternalLoginCall(id),
		onSuccess: async () => {
			successToast("settings:account.external-logins.notifications.unlinked");
			await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.externalLogins] });
		},
		onError: (error) =>
			errorToast(apiErrorMessage(error, "settings:account.external-logins.notifications.unlink-failed")),
	});

	const linkMutation = useMutation({
		mutationFn: (provider: string) => startOidcLinkCall(provider),
		onSuccess: (url) => {
			globalThis.location.href = url;
		},
		onError: (error) =>
			errorToast(apiErrorMessage(error, "settings:account.external-logins.notifications.link-failed")),
	});

	return {
		logins: loginsQuery.data ?? [],
		provider: providerQuery.data ?? null,
		isLoading: loginsQuery.isLoading || providerQuery.isLoading,
		unlink: unlinkMutation.mutate,
		isUnlinking: unlinkMutation.isPending,
		link: linkMutation.mutate,
		isLinking: linkMutation.isPending,
	};
};
