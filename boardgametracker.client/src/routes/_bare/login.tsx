import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtInputField } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAppForm } from "@/hooks/form";
import { useAuth } from "@/hooks/useAuth";
import type { OidcProvider } from "@/models/Auth/Auth";
import { getOidcProviderCall } from "@/services/authService";
import { apiUrl } from "@/utils/apiUrl";
import { handleFormSubmit } from "@/utils/formUtils";

export const Route = createFileRoute("/_bare/login")({
	component: LoginPage,
});

function LoginPage() {
	const { t } = useTranslation();
	const navigate = useNavigate();
	const { login, isLoading } = useAuth();
	const [error, setError] = useState<string | null>(null);

	const { data: oidcProvider } = useQuery<OidcProvider | null>({
		queryKey: ["oidcProvider"],
		queryFn: getOidcProviderCall,
	});

	const form = useAppForm({
		defaultValues: {
			username: "",
			password: "",
		},
		onSubmit: async ({ value }) => {
			setError(null);
			try {
				await login({ username: value.username, password: value.password });
				await navigate({ to: "/" });
			} catch {
				setError(t("auth.invalid-credentials"));
			}
		},
	});

	const handleOidcLogin = (provider: OidcProvider) => {
		const redirectUri = `${window.location.origin}/auth-callback`;
		window.location.href = `${apiUrl}auth/oidc/${provider.name}/login?redirectUri=${encodeURIComponent(redirectUri)}&state=${provider.name}`;
	};

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard className="w-full max-w-md space-y-6">
					<div className="text-center">
						<BgtText size="6" weight="bold" color="white">
							{t("auth.login-title")}
						</BgtText>
						<BgtText size="2" color="gray">
							{t("auth.login-subtitle")}
						</BgtText>
					</div>

					<form onSubmit={handleFormSubmit(form)} className="space-y-4">
						<form.Field name="username">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="text"
									label={t("auth.username")}
									placeholder={t("auth.username-placeholder")}
									disabled={isLoading}
								/>
							)}
						</form.Field>

						<form.Field name="password">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("auth.password")}
									placeholder={t("auth.password-placeholder")}
									disabled={isLoading}
								/>
							)}
						</form.Field>

						{error && (
							<div className="p-3 bg-error/10 border border-error/20 rounded-md">
								<BgtText size="2" color="red">
									{error}
								</BgtText>
							</div>
						)}

						<BgtButton type="submit" disabled={isLoading} size="3" className="w-full">
							{isLoading ? t("auth.logging-in") : t("auth.login")}
						</BgtButton>
					</form>

					{oidcProvider && (
						<>
							<div className="relative">
								<div className="absolute inset-0 flex items-center">
									<div className="w-full border-t border-white/10" />
								</div>
								<div className="relative flex justify-center text-sm">
									<span className="px-2 bg-primary/10 text-gray-400">{t("auth.or-continue-with")}</span>
								</div>
							</div>

							<div className="space-y-2">
								<BgtButton
									variant="cancel"
									size="3"
									className="w-full"
									onClick={() => handleOidcLogin(oidcProvider)}
									style={oidcProvider.buttonColor ? { borderColor: oidcProvider.buttonColor } : undefined}
								>
									{oidcProvider.iconUrl && <img src={oidcProvider.iconUrl} alt="" className="w-5 h-5" />}
									{oidcProvider.displayName}
								</BgtButton>
							</div>
						</>
					)}
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
