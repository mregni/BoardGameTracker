import type { AnyFieldApi } from "@tanstack/react-form";
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
import { forgotPasswordCall } from "@/services/authService";
import { handleFormSubmit } from "@/utils/formUtils";

export const Route = createFileRoute("/_bare/forgot-password")({
	component: ForgotPasswordPage,
});

function ForgotPasswordPage() {
	const { t } = useTranslation("auth");
	const navigate = useNavigate();
	const [submitted, setSubmitted] = useState(false);
	const [isLoading, setIsLoading] = useState(false);

	const form = useAppForm({
		defaultValues: {
			username: "",
		},
		onSubmit: async ({ value }) => {
			setIsLoading(true);
			try {
				await forgotPasswordCall({ username: value.username });
			} catch {
				setSubmitted(true);
			} finally {
				setIsLoading(false);
				setSubmitted(true);
			}
		},
	});

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard className="w-full max-w-md space-y-6">
					<div className="text-center">
						<BgtText size="6" weight="bold" color="white">
							{t("forgot-title")}
						</BgtText>
						<BgtText size="2" color="gray">
							{t("forgot-subtitle")}
						</BgtText>
					</div>

					{submitted ? (
						<div className="space-y-4">
							<BgtText size="2" color="gray" className="block text-center">
								{t("forgot-confirmation")}
							</BgtText>
							<BgtButton size="3" className="w-full" onClick={() => navigate({ to: "/login" })}>
								{t("back-to-login")}
							</BgtButton>
						</div>
					) : (
						<form onSubmit={handleFormSubmit(form)} className="space-y-4">
							<form.Field name="username">
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										type="text"
										label={t("username")}
										placeholder={t("username-placeholder")}
										disabled={isLoading}
									/>
								)}
							</form.Field>

							<BgtButton type="submit" size="3" className="w-full" disabled={isLoading}>
								{t("forgot-submit")}
							</BgtButton>
							<BgtButton variant="cancel" size="3" className="w-full" onClick={() => navigate({ to: "/login" })}>
								{t("back-to-login")}
							</BgtButton>
						</form>
					)}
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
