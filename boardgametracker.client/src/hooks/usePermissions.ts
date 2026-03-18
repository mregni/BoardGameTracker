import { useAuth } from "@/hooks/useAuth";

export const usePermissions = () => {
	const hasRole = useAuth((s) => s.hasRole);
	const authStatus = useAuth((s) => s.authStatus);
	const authDisabled = !authStatus?.authEnabled;

	const isAdmin = authDisabled || hasRole("Admin");
	const isUser = authDisabled || hasRole("User");

	return {
		isAdmin,
		canWrite: isAdmin || isUser,
		canManageSettings: isAdmin,
	};
};
