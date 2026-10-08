import axios from "axios";
import { useAuth } from "@/hooks/useAuth";
import type { LoginResponse } from "@/models/Auth/Auth";
import { apiUrl } from "./apiUrl";

const REFRESH_LOCK = "bgt-token-refresh";

const withRefreshLock = <T>(task: () => Promise<T>): Promise<T> => {
	const locks = globalThis.navigator?.locks;
	return locks ? (locks.request(REFRESH_LOCK, () => task()) as Promise<T>) : task();
};

export const tokensRefreshedElsewhere = async (staleRefreshToken: string | null): Promise<string | null> => {
	await useAuth.persist.rehydrate();
	const { accessToken, refreshToken } = useAuth.getState();
	return accessToken && refreshToken && refreshToken !== staleRefreshToken ? accessToken : null;
};

export const refreshAccessToken = (staleRefreshToken: string | null): Promise<string> =>
	withRefreshLock(async () => {
		const refreshedElsewhere = await tokensRefreshedElsewhere(staleRefreshToken);
		if (refreshedElsewhere) {
			return refreshedElsewhere;
		}

		const { refreshToken } = useAuth.getState();
		if (!refreshToken) {
			throw new Error("No refresh token");
		}

		const response = await axios.post<LoginResponse>(`${apiUrl}auth/refresh`, { refreshToken });
		const { accessToken, refreshToken: rotatedRefreshToken, user } = response.data;
		useAuth.getState().setTokens(accessToken, rotatedRefreshToken, user);
		return accessToken;
	});
