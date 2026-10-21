import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/hooks/useAuth";
import { getUsers } from "@/services/queries/auth";
import { getPlayers } from "@/services/queries/players";

export const useLinkedAccountOptions = () => {
	const authEnabled = useAuth((s) => s.authStatus?.authEnabled === true);
	const isAdmin = useAuth((s) => s.hasRole("Admin"));
	const canManage = authEnabled && isAdmin;

	const usersQuery = useQuery({ ...getUsers(), enabled: canManage });
	const playersQuery = useQuery({ ...getPlayers(), enabled: canManage });

	return {
		authEnabled,
		canManage,
		users: usersQuery.data ?? [],
		players: playersQuery.data ?? [],
	};
};
