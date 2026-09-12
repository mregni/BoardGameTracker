import type { ChangeDetectionConnectionTest, ChangeDetectionWatchInfo } from "@/models/ChangeDetection/ChangeDetection";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "changedetection";

export const testChangeDetectionConnectionCall = (): Promise<ChangeDetectionConnectionTest> => {
	return axiosInstance.get<ChangeDetectionConnectionTest>(`${domain}/test`).then((response) => {
		return response.data;
	});
};

export const getWatchInfoCall = (watchId: string): Promise<ChangeDetectionWatchInfo> => {
	return axiosInstance.get<ChangeDetectionWatchInfo>(`${domain}/watch/${watchId}`).then((response) => {
		return response.data;
	});
};
