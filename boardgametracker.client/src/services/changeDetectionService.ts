import type { ChangeDetectionWatchInfo } from "@/models/ChangeDetection/ChangeDetection";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "changedetection";

export const getWatchInfoCall = (watchId: string): Promise<ChangeDetectionWatchInfo> => {
	return axiosInstance.get<ChangeDetectionWatchInfo>(`${domain}/watch/${watchId}`).then((response) => {
		return response.data;
	});
};
