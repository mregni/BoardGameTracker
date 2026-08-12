import type { RagAnswer } from "@/models";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "rag";

export const askRagCall = (gameId: number, question: string, manualId?: number): Promise<RagAnswer> => {
	return axiosInstance
		.post<RagAnswer>(`${domain}/game/${gameId}/ask`, { question, manualId }, { timeout: 120000 })
		.then((response) => response.data);
};
