import type {
	OidcDiscoveryResult,
	OidcProviderConfig,
	OidcProviderRequest,
	OidcProviderSummary,
	OidcSetup,
} from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

const domain = "admin/oidc-providers";

export const getOidcProvidersCall = (): Promise<OidcProviderSummary[]> =>
	axiosInstance.get<OidcProviderSummary[]>(domain).then((response) => response.data);

export const getOidcProviderConfigCall = (id: number): Promise<OidcProviderConfig> =>
	axiosInstance.get<OidcProviderConfig>(`${domain}/${id}`).then((response) => response.data);

export const getOidcSetupCall = (): Promise<OidcSetup> =>
	axiosInstance.get<OidcSetup>(`${domain}/setup`).then((response) => response.data);

export const testOidcDiscoveryCall = (authority: string): Promise<OidcDiscoveryResult> =>
	axiosInstance.post<OidcDiscoveryResult>(`${domain}/test-discovery`, { authority }).then((response) => response.data);

const toPayload = (request: OidcProviderRequest) => ({
	...request,
	clientSecret: request.clientSecret === "" ? null : request.clientSecret,
	authorizationEndpoint: null,
	tokenEndpoint: null,
	userInfoEndpoint: null,
	iconUrl: null,
	buttonColor: null,
});

export const createOidcProviderCall = (request: OidcProviderRequest): Promise<OidcProviderConfig> =>
	axiosInstance.post<OidcProviderConfig>(domain, toPayload(request)).then((response) => response.data);

export const updateOidcProviderCall = (id: number, request: OidcProviderRequest): Promise<OidcProviderConfig> =>
	axiosInstance
		.put<OidcProviderConfig>(`${domain}/${id}`, { id, ...toPayload(request) })
		.then((response) => response.data);

export const deleteOidcProviderCall = (id: number): Promise<void> => axiosInstance.delete(`${domain}/${id}`);
