import { useNavigate, useRouter } from "@tanstack/react-router";
import { useCallback } from "react";
import { normalizePathname, previousPathname } from "@/utils/historyTrail";

export const useGoUp = (parent: string, isParent?: (pathname: string) => boolean) => {
	const router = useRouter();
	const navigate = useNavigate();

	return useCallback(() => {
		const previous = previousPathname(router.history);
		const matches = isParent ?? ((pathname: string) => pathname === normalizePathname(parent));
		if (previous !== undefined && matches(previous)) {
			router.history.back();
			return;
		}
		navigate({ to: parent, replace: true });
	}, [router, navigate, parent, isParent]);
};
