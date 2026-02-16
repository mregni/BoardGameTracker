import type { CompareResult } from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

const domain = "compare";

export const getCompareCall = (playerOne: number, playerTwo: number): Promise<CompareResult> => {
	return axiosInstance.get<CompareResult>(`${domain}/${playerOne}/${playerTwo}`).then((response) => {
		return response.data;
	});
};
