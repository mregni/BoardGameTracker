import type { IntegrationKind, IntegrationStatus } from "@/models/Settings/IntegrationStatus";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "settings/integrations";

export const getIntegrationStatusesCall = (): Promise<IntegrationStatus[]> => {
	return axiosInstance.get<IntegrationStatus[]>(`${domain}/status`).then((response) => {
		return response.data;
	});
};

export const testIntegrationCall = (integration: IntegrationKind): Promise<IntegrationStatus> => {
	return axiosInstance.post<IntegrationStatus>(`${domain}/${integration}/test`).then((response) => {
		return response.data;
	});
};
