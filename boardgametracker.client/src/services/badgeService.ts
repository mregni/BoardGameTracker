import type { Badge } from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

const domain = "badge";

export const getAllBadgesCall = (): Promise<Badge[]> => {
	return axiosInstance.get<Badge[]>(`${domain}`).then((response) => {
		return response.data;
	});
};
