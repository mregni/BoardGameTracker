import type {
	CreateGameNight,
	FinishRunningGame,
	GameNight,
	GameNightDetail,
	RunningGame,
	SendInvitesResult,
	Session,
	StartRunningGame,
	UpdateGameNight,
	UpdateGameNightRsvp,
} from "@/models";
import { axiosInstance } from "@/utils/axiosInstance";

const domain = "gamenight";

export const getGameNightsCall = (): Promise<GameNight[]> => {
	return axiosInstance.get<GameNight[]>(domain).then((response) => {
		return response.data;
	});
};

export const createGameNightCall = (gameNight: CreateGameNight): Promise<GameNight> => {
	return axiosInstance.post<GameNight>(domain, { ...gameNight }).then((response) => {
		return response.data;
	});
};

export const updateGameNightCall = (gameNight: UpdateGameNight): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(domain, { ...gameNight }).then((response) => {
		return response.data;
	});
};

export const deleteGameNightCall = (id: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/${id}`);
};

export const addSuggestedGameCall = (gameNightId: number, gameId: number): Promise<GameNight> => {
	return axiosInstance
		.put<GameNight>(`${domain}/${gameNightId}/suggestions/${gameId}`)
		.then((response) => response.data);
};

export const removeSuggestedGameCall = (gameNightId: number, gameId: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/${gameNightId}/suggestions/${gameId}`).then(() => undefined);
};

export const sendInvitesCall = (id: number): Promise<SendInvitesResult> => {
	return axiosInstance.post<SendInvitesResult>(`${domain}/${id}/send-invites`).then((response) => response.data);
};

export const updateGameNightRsvpCall = (rsvp: UpdateGameNightRsvp): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(`${domain}/rsvp`, { ...rsvp }).then((response) => {
		return response.data;
	});
};

export const updateGameNightRsvpByLinkCall = (linkId: string, rsvp: UpdateGameNightRsvp): Promise<GameNight> => {
	return axiosInstance.put<GameNight>(`${domain}/link/${linkId}/rsvp`, { ...rsvp }).then((response) => {
		return response.data;
	});
};

export const getGameNightByLinkCall = (linkId: string): Promise<GameNight> => {
	return axiosInstance.get<GameNight>(`${domain}/link/${linkId}`).then((response) => {
		return response.data;
	});
};

export const getGameNightCall = (id: number): Promise<GameNightDetail> => {
	return axiosInstance.get<GameNightDetail>(`${domain}/${id}`).then((response) => response.data);
};

export const startRunningGameCall = (gameNightId: number, command: StartRunningGame): Promise<RunningGame> => {
	return axiosInstance
		.post<RunningGame>(`${domain}/${gameNightId}/running-games`, command)
		.then((response) => response.data);
};

export const finishRunningGameCall = (id: number, command: FinishRunningGame): Promise<Session> => {
	return axiosInstance.post<Session>(`${domain}/running-games/${id}/finish`, command).then((response) => response.data);
};

export const endGameNightCall = (id: number): Promise<void> => {
	return axiosInstance.post(`${domain}/${id}/end`);
};

export const reopenGameNightCall = (id: number): Promise<void> => {
	return axiosInstance.post(`${domain}/${id}/reopen`);
};

export const discardRunningGameCall = (id: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/running-games/${id}`);
};
