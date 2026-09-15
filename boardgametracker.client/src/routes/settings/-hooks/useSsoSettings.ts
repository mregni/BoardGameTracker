import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { type OidcDiscoveryResult, type OidcProviderRequest, QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import {
	createOidcProviderCall,
	deleteOidcProviderCall,
	testOidcDiscoveryCall,
	updateOidcProviderCall,
} from "@/services/oidcAdminService";
import { getOidcProviderConfig, getOidcProviders, getOidcSetup } from "@/services/queries/oidc";
import { apiErrorMessage } from "@/utils/errorUtils";

export const useSsoSettings = () => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();
	const [discovery, setDiscovery] = useState<OidcDiscoveryResult | null>(null);
	const [discoveryError, setDiscoveryError] = useState<string | null>(null);

	const providersQuery = useQuery(getOidcProviders());
	const providerId = providersQuery.data?.[0]?.id;
	const providerQuery = useQuery({ ...getOidcProviderConfig(providerId ?? 0), enabled: providerId !== undefined });
	const setupQuery = useQuery(getOidcSetup());

	const invalidate = async () => {
		await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.oidcProviders] });
		await queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.oidcProvider] });
	};

	const saveMutation = useMutation({
		mutationFn: (request: OidcProviderRequest) =>
			providerId === undefined ? createOidcProviderCall(request) : updateOidcProviderCall(providerId, request),
		onSuccess: async () => {
			successToast("settings:sso.notifications.saved");
			await invalidate();
		},
		onError: (error) => errorToast(apiErrorMessage(error, "settings:sso.notifications.save-failed")),
	});

	const deleteMutation = useMutation({
		mutationFn: (id: number) => deleteOidcProviderCall(id),
		onSuccess: async () => {
			successToast("settings:sso.notifications.deleted");
			setDiscovery(null);
			await invalidate();
		},
		onError: (error) => errorToast(apiErrorMessage(error, "settings:sso.notifications.delete-failed")),
	});

	const discoveryMutation = useMutation({
		mutationFn: (authority: string) => testOidcDiscoveryCall(authority),
		onMutate: () => {
			setDiscovery(null);
			setDiscoveryError(null);
		},
		onSuccess: (result) => setDiscovery(result),
		onError: (error) => setDiscoveryError(apiErrorMessage(error, "settings:sso.discovery.failed")),
	});

	return {
		isLoading:
			providersQuery.isLoading || setupQuery.isLoading || (providerId !== undefined && providerQuery.isLoading),
		provider: providerId === undefined ? null : (providerQuery.data ?? null),
		setup: setupQuery.data,
		save: saveMutation.mutateAsync,
		isSaving: saveMutation.isPending,
		remove: deleteMutation.mutateAsync,
		isDeleting: deleteMutation.isPending,
		testDiscovery: discoveryMutation.mutate,
		isTestingDiscovery: discoveryMutation.isPending,
		discovery,
		discoveryError,
	};
};
