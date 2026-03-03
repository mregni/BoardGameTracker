import { useAuth } from "@/hooks/useAuth";

export const usePermissions = () => {
	const hasRole = useAuth((s) => s.hasRole);
	const isAdmin = hasRole("Admin");
	const isUser = hasRole("User");

	return {
		isAdmin,
		canWrite: isAdmin || isUser,
		canManageSettings: isAdmin,
	};
};
