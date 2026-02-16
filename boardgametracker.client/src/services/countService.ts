import type { KeyValuePair } from "../models";
import { axiosInstance } from "../utils/axiosInstance";

export const getCountsCall = (): Promise<KeyValuePair<string, number>[]> => {
	return axiosInstance.get<KeyValuePair<string, number>[]>("count").then((response) => {
		return response.data;
	});
};
