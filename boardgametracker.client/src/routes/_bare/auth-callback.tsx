import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";
import { z } from "zod";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "react-i18next";

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
	const { t } = useTranslation();
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
		<BgtPage>
			<BgtPageContent centered>
				<div className="text-center">
					<div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
					<BgtText size="3" color="white" className="mt-4">
						{t("auth.authenticating")}
					</BgtText>
				</div>
			</BgtPageContent>
		</BgtPage>
	);
}
