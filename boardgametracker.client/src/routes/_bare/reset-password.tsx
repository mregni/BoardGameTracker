import type { AnyFieldApi } from "@tanstack/react-form";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtInputField } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtText } from "@/components/BgtText/BgtText";
import { useAppForm } from "@/hooks/form";
import { useToasts } from "@/routes/-hooks/useToasts";
import { confirmResetPasswordCall } from "@/services/authService";
import { handleFormSubmit } from "@/utils/formUtils";

const resetPasswordSearchSchema = z.object({
	userId: z.string(),
	token: z.string(),
});

export const Route = createFileRoute("/_bare/reset-password")({
	component: ResetPasswordPage,
	validateSearch: resetPasswordSearchSchema,
});

function ResetPasswordPage() {
	const { t } = useTranslation(["auth", "settings", "common"]);
	const navigate = useNavigate();
	const { userId, token } = Route.useSearch();
	const { successToast } = useToasts();
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	const form = useAppForm({
		defaultValues: {
			newPassword: "",
			confirmPassword: "",
		},
		onSubmit: async ({ value }) => {
			setError(null);
			if (value.newPassword !== value.confirmPassword) {
				setError(t("settings:account.password.mismatch"));
				return;
			}

			setIsLoading(true);
			try {
				await confirmResetPasswordCall({ userId, token, newPassword: value.newPassword });
				successToast("auth:reset-success");
				await navigate({ to: "/login" });
			} catch {
				setError(t("reset-invalid"));
			} finally {
				setIsLoading(false);
			}
		},
	});

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard className="w-full max-w-md space-y-6">
					<div className="text-center">
						<BgtText size="6" weight="bold" color="white">
							{t("reset-title")}
						</BgtText>
						<BgtText size="2" color="gray">
							{t("reset-subtitle")}
						</BgtText>
					</div>

					<form onSubmit={handleFormSubmit(form)} className="space-y-4">
						<form.Field
							name="newPassword"
							validators={{
								onChange: ({ value }: { value: string }) => {
									if (!value) return t("common:required", "Required");
									if (value.length < 4) return t("settings:account.password.min-length");
									return undefined;
								},
							}}
						>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("settings:account.password.new.label")}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field name="confirmPassword">
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="password"
									label={t("settings:account.password.confirm.label")}
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

						<BgtButton type="submit" size="3" className="w-full" disabled={isLoading}>
							{t("reset-submit")}
						</BgtButton>
						<BgtButton variant="cancel" size="3" className="w-full" onClick={() => navigate({ to: "/login" })}>
							{t("back-to-login")}
						</BgtButton>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
