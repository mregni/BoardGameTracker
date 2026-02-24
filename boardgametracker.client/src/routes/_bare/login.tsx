import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "@/hooks/useAuth";
import type { OidcProvider } from "@/models/Auth/Auth";
import { getOidcProvidersCall } from "@/services/authService";
import { apiUrl } from "@/utils/apiUrl";
import { useQuery } from "@tanstack/react-query";

export const Route = createFileRoute("/_bare/login")({
	component: LoginPage,
});

function LoginPage() {
	const { t } = useTranslation();
	const navigate = useNavigate();
	const { login, isLoading } = useAuth();
	const [username, setUsername] = useState("");
	const [password, setPassword] = useState("");
	const [error, setError] = useState<string | null>(null);

	const { data: oidcProviders } = useQuery<OidcProvider[]>({
		queryKey: ["oidcProviders"],
		queryFn: getOidcProvidersCall,
	});

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();
		setError(null);

		try {
			await login({ username, password });
			await navigate({ to: "/" });
		} catch {
			setError(t("auth.invalid-credentials"));
		}
	};

	const handleOidcLogin = (provider: OidcProvider) => {
		const redirectUri = `${window.location.origin}/auth-callback`;
		window.location.href = `${apiUrl}auth/oidc/${provider.name}/login?redirectUri=${encodeURIComponent(redirectUri)}&state=${provider.name}`;
	};

	return (
		<div className="flex items-center justify-center min-h-screen bg-background">
			<div className="w-full max-w-md p-8 space-y-6 bg-card rounded-lg border border-white/10">
				<div className="text-center">
					<h1 className="text-2xl font-bold text-white">{t("auth.login-title")}</h1>
					<p className="text-sm text-gray-400 mt-1">{t("auth.login-subtitle")}</p>
				</div>

				<form onSubmit={handleSubmit} className="space-y-4">
					<div>
						<label htmlFor="username" className="block text-sm font-medium text-gray-300 mb-1">
							{t("auth.username")}
						</label>
						<input
							id="username"
							type="text"
							value={username}
							onChange={(e) => setUsername(e.target.value)}
							className="w-full px-3 py-2 bg-background border border-white/10 rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
							placeholder={t("auth.username-placeholder")}
							required
							autoComplete="username"
						/>
					</div>

					<div>
						<label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-1">
							{t("auth.password")}
						</label>
						<input
							id="password"
							type="password"
							value={password}
							onChange={(e) => setPassword(e.target.value)}
							className="w-full px-3 py-2 bg-background border border-white/10 rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent"
							placeholder={t("auth.password-placeholder")}
							required
							autoComplete="current-password"
						/>
					</div>

					{error && (
						<div className="p-3 bg-red-500/10 border border-red-500/20 rounded-md">
							<p className="text-sm text-red-400">{error}</p>
						</div>
					)}

					<button
						type="submit"
						disabled={isLoading}
						className="w-full py-2 px-4 bg-primary text-white rounded-md font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
					>
						{isLoading ? t("auth.logging-in") : t("auth.login")}
					</button>
				</form>

				{oidcProviders && oidcProviders.length > 0 && (
					<>
						<div className="relative">
							<div className="absolute inset-0 flex items-center">
								<div className="w-full border-t border-white/10" />
							</div>
							<div className="relative flex justify-center text-sm">
								<span className="px-2 bg-card text-gray-400">{t("auth.or-continue-with")}</span>
							</div>
						</div>

						<div className="space-y-2">
							{oidcProviders.map((provider) => (
								<button
									key={provider.name}
									type="button"
									onClick={() => handleOidcLogin(provider)}
									className="w-full py-2 px-4 border border-white/10 rounded-md text-white font-medium hover:bg-white/5 transition-colors flex items-center justify-center gap-2"
									style={provider.buttonColor ? { borderColor: provider.buttonColor } : undefined}
								>
									{provider.iconUrl && <img src={provider.iconUrl} alt="" className="w-5 h-5" />}
									{provider.displayName}
								</button>
							))}
						</div>
					</>
				)}
			</div>
		</div>
	);
}
