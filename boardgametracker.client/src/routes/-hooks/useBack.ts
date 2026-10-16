import { useNavigate, useRouter } from "@tanstack/react-router";
import { useCallback } from "react";
import { previousPathname } from "@/utils/historyTrail";

export const useBackTo = () => {
	const router = useRouter();
	const navigate = useNavigate();

	return useCallback(
		(parent: string) => {
			if (previousPathname(router.history) !== undefined) {
				router.history.back();
				return;
			}
			navigate({ to: parent, replace: true });
		},
		[router, navigate],
	);
};

export const useBack = (parent: string) => {
	const backTo = useBackTo();
	return useCallback(() => backTo(parent), [backTo, parent]);
};
