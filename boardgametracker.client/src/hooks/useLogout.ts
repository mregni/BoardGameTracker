import { useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useAuth } from "@/hooks/useAuth";

export const useLogout = () => {
	const logout = useAuth((s) => s.logout);
	const queryClient = useQueryClient();
	const navigate = useNavigate();

	return useCallback(async () => {
		await logout();
		queryClient.clear();
		await navigate({ to: "/login", replace: true });
	}, [logout, queryClient, navigate]);
};
