import { queryOptions } from "@tanstack/react-query";
import { QUERY_KEYS } from "@/models";
import { getExternalLoginsCall, getOidcProviderCall } from "../authService";
import { getOidcProviderConfigCall, getOidcProvidersCall, getOidcSetupCall } from "../oidcAdminService";

export const getOidcProviders = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.oidcProviders],
		queryFn: () => getOidcProvidersCall(),
	});

export const getOidcProviderConfig = (id: number) =>
	queryOptions({
		queryKey: [QUERY_KEYS.oidcProviders, id],
		queryFn: () => getOidcProviderConfigCall(id),
	});

export const getOidcSetup = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.oidcSetup],
		queryFn: () => getOidcSetupCall(),
	});

export const getPublicOidcProvider = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.oidcProvider],
		queryFn: () => getOidcProviderCall(),
	});

export const getExternalLogins = () =>
	queryOptions({
		queryKey: [QUERY_KEYS.externalLogins],
		queryFn: () => getExternalLoginsCall(),
	});
