import type { GameManual, GameNightManuals } from "@/models";
import { apiUrl } from "../utils/apiUrl";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "manual";

export const getGameManualsCall = (gameId: number): Promise<GameManual[]> => {
	return axiosInstance.get<GameManual[]>(`${domain}/game/${gameId}`).then((response) => response.data);
};

export const uploadManualsCall = (gameId: number, files: File[]): Promise<GameManual[]> => {
	const formData = new FormData();
	for (const file of files) {
		formData.append("files", file);
	}

	return axiosInstance
		.post<GameManual[]>(`${domain}/game/${gameId}`, formData, {
			headers: { "Content-Type": "multipart/form-data" },
		})
		.then((response) => response.data);
};

export const deleteManualCall = (id: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/${id}`);
};

export const reindexManualCall = (id: number): Promise<void> => {
	return axiosInstance.post(`${domain}/${id}/reindex`);
};

export const getManualPageImageCall = (url: string): Promise<Blob> => {
	return axiosInstance.get<Blob>(url, { responseType: "blob" }).then((response) => response.data);
};

export const getGameNightManualsCall = (linkId: string): Promise<GameNightManuals[]> => {
	return axiosInstance.get<GameNightManuals[]>(`${domain}/gamenight/${linkId}`).then((response) => response.data);
};

export const downloadManualCall = async (id: number, fileName: string): Promise<void> => {
	const response = await axiosInstance.get<Blob>(`${domain}/${id}/download`, { responseType: "blob" });
	triggerBlobDownload(response.data, fileName);
};

export const manualInviteDownloadUrl = (linkId: string, manualId: number): string =>
	`${apiUrl}${domain}/gamenight/${linkId}/manual/${manualId}/download`;

const triggerBlobDownload = (blob: Blob, fileName: string): void => {
	const url = URL.createObjectURL(blob);
	const anchor = document.createElement("a");
	anchor.href = url;
	anchor.download = fileName;
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
	URL.revokeObjectURL(url);
};
