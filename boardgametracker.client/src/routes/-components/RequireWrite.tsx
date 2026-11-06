import { Navigate } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { useAuth } from "@/hooks/useAuth";
import { usePermissions } from "@/hooks/usePermissions";

export const RequireWrite = ({ children }: { children: ReactNode }) => {
	const { canWrite } = usePermissions();
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	if (canWrite) return children;
	return isAuthenticated ? <Navigate to="/" /> : null;
};
