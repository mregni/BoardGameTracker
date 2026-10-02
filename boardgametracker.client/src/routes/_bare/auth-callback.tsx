import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAuth } from "@/hooks/useAuth";
import type { LoginResponse } from "@/models/Auth/Auth";
import { useToasts } from "@/routes/-hooks/useToasts";
import { adoptOidcLoginCall } from "@/services/authService";
import { translateApiError } from "@/utils/errorUtils";
import { safeRedirectPath } from "@/utils/redirectUtils";

const callbackSearchSchema = z.object({
	error: z.string().optional(),
	redirect: z.string().optional(),
	linked: z.string().optional(),
});

export const Route = createFileRoute("/_bare/auth-callback")({
	component: AuthCallbackPage,
	validateSearch: callbackSearchSchema,
});

function AuthCallbackPage() {
	const navigate = useNavigate();
	const { t } = useTranslation("auth");
	const { successToast } = useToasts();
	const setTokens = useAuth((s) => s.setTokens);
	const { error, redirect, linked } = Route.useSearch();
	const [failure, setFailure] = useState<string | null>(null);
	const adoption = useRef<Promise<LoginResponse> | null>(null);
	const handled = useRef(false);

	useEffect(() => {
		if (error) {
			setFailure(translateApiError(error, "error:auth.oidc-failed"));
			return;
		}

		if (linked) {
			if (!handled.current) {
				handled.current = true;
				successToast("auth:oidc.linked");
				navigate({ to: "/settings", replace: true });
			}
			return;
		}

		adoption.current ??= adoptOidcLoginCall();
		let cancelled = false;
		adoption.current
			.then((login) => {
				if (cancelled || handled.current) return;
				handled.current = true;
				setTokens(login.accessToken, login.refreshToken, login.user);
				navigate({ to: safeRedirectPath(redirect), replace: true });
			})
			.catch(() => {
				if (!cancelled) {
					setFailure(translateApiError("error.auth.oidc-handoff-expired"));
				}
			});

		return () => {
			cancelled = true;
		};
	}, [error, linked, redirect, navigate, setTokens, successToast]);

	return (
		<BgtPage>
			<BgtPageContent centered>
				{failure ? (
					<BgtCard className="max-w-md w-full">
						<div className="flex flex-col gap-4 text-center">
							<BgtText size="4" color="white" className="font-semibold">
								{t("oidc.failed-title")}
							</BgtText>
							<BgtText size="2" color="red">
								{failure}
							</BgtText>
							<Link to="/login" className="text-primary hover:underline">
								{t("back-to-login")}
							</Link>
						</div>
					</BgtCard>
				) : (
					<div className="text-center">
						<div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
						<BgtText size="3" color="white" className="mt-4">
							{t("authenticating")}
						</BgtText>
					</div>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
