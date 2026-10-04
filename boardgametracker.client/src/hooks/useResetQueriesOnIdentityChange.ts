import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef } from "react";
import { useAuth } from "@/hooks/useAuth";
import type { User } from "@/models/Auth/Auth";

const identityOf = (user: User | null) =>
	user ? `${user.id}|${[...user.roles].sort((a, b) => a.localeCompare(b)).join(",")}` : "";

export const useResetQueriesOnIdentityChange = () => {
	const queryClient = useQueryClient();
	const identity = useAuth((s) => identityOf(s.user));
	const previous = useRef(identity);

	useEffect(() => {
		if (previous.current === identity) {
			return;
		}

		previous.current = identity;
		void queryClient.resetQueries();
	}, [identity, queryClient]);
};
