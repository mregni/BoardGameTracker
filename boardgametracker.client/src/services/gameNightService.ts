import type { CreateGameNight, GameNight, GameNightStatistics, UpdateGameNightRsvp } from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

const domain = "gamenight";

export const getGameNightsCall = (): Promise<GameNight[]> => {
	return axiosInstance.get<GameNight[]>(domain).then((response) => {
		return response.data;
	});
};

export const getGameNightStatisticsCall = (): Promise<GameNightStatistics> => {
	return axiosInstance.get<GameNightStatistics>(`${domain}/statistics`).then((response) => {
		return response.data;
	});
};

export const createGameNightCall = (gameNight: CreateGameNight): Promise<GameNight> => {
	return axiosInstance.post<GameNight>(domain, { ...gameNight }).then((response) => {
		return response.data;
	});
};

export const updateGameNightCall = (gameNight: GameNight): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(domain, { ...gameNight }).then((response) => {
		return response.data;
	});
};

export const deleteGameNightCall = (id: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/${id}`);
};

export const updateGameNightRsvpCall = (rsvp: UpdateGameNightRsvp): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(`${domain}/rsvp`, { ...rsvp }).then((response) => {
		return response.data;
	});
};

export const getGameNightByLinkCall = (linkId: string): Promise<GameNight> => {
	return axiosInstance.get<GameNight>(`${domain}/link/${linkId}`).then((response) => {
		return response.data;
	});
};

export const submitRsvpByLinkCall = (rsvp: UpdateGameNightRsvp): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(`${domain}/rsvp`, { ...rsvp }).then((response) => {
		return response.data;
	});
};
