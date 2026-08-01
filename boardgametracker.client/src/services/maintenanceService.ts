import { axiosInstance } from "../utils/axiosInstance";

const domain = "maintenance";

export const resetDatabaseCall = (): Promise<void> => {
	return axiosInstance.post(`${domain}/reset`).then(() => undefined);
};

export const factoryResetCall = (): Promise<void> => {
	return axiosInstance.post(`${domain}/factory-reset`).then(() => undefined);
};
