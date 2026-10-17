import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useAuth } from "@/hooks/useAuth";
import { endOidcSessionCall } from "@/services/authService";

export const useLogout = () => {
	const logout = useAuth((s) => s.logout);
	const queryClient = useQueryClient();
	const navigate = useNavigate();

	return useCallback(async () => {
		const endSessionUrl = await endOidcSessionCall().catch(() => null);
		await logout().catch(() => undefined);
		queryClient.clear();
		if (endSessionUrl) {
			globalThis.location.assign(endSessionUrl);
			return;
		}

		await navigate({ to: "/login", replace: true });
	}, [logout, queryClient, navigate]);
};
