import { useNavigate, useRouter } from "@tanstack/react-router";
import { useCallback } from "react";

export const useGoBack = (fallback: string) => {
	const router = useRouter();
	const navigate = useNavigate();

	return useCallback(() => {
		if (router.history.canGoBack()) {
			router.history.back();
			return;
		}
		navigate({ to: fallback });
	}, [router, navigate, fallback]);
};
