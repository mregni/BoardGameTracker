import { Navigate } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { usePermissions } from "@/hooks/usePermissions";

export const RequireWrite = ({ children }: { children: ReactNode }) => {
	const { canWrite } = usePermissions();
	return canWrite ? children : <Navigate to="/" />;
};
