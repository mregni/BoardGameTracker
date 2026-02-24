import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { z } from "zod";
import { useAuth } from "@/hooks/useAuth";

const callbackSearchSchema = z.object({
	accessToken: z.string().optional(),
	refreshToken: z.string().optional(),
	error: z.string().optional(),
});

export const Route = createFileRoute("/_bare/auth-callback")({
	component: AuthCallbackPage,
	validateSearch: callbackSearchSchema,
});

function AuthCallbackPage() {
	const navigate = useNavigate();
	const { setTokens } = useAuth();
	const { accessToken, refreshToken, error } = Route.useSearch();

	useEffect(() => {
		if (error) {
			navigate({ to: "/login" });
			return;
		}

		if (accessToken && refreshToken) {
			// Decode user from JWT payload
			try {
				const payload = JSON.parse(atob(accessToken.split(".")[1]));
				const user = {
					id: payload.sub,
					username: payload.unique_name,
					displayName: payload.display_name ?? null,
					roles: Array.isArray(payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"])
						? payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
						: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
							? [payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]]
							: [],
				};

				setTokens(accessToken, refreshToken, user);
				navigate({ to: "/" });
			} catch {
				navigate({ to: "/login" });
			}
		} else {
			navigate({ to: "/login" });
		}
	}, [accessToken, refreshToken, error, navigate, setTokens]);

	return (
		<div className="flex items-center justify-center min-h-screen bg-background">
			<div className="text-center">
				<div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
				<p className="text-white mt-4">Authenticating...</p>
			</div>
		</div>
	);
}
