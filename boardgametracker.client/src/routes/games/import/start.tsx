import type { AnyFieldApi } from "@tanstack/react-form";
import { createFileRoute, useNavigate, useRouter } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCenteredCard } from "@/components/BgtCard/BgtCenteredCard";
import { BgtInputField } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import { BggUserNameSchema } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";

export const Route = createFileRoute("/games/import/start")({
	component: RouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation(["games", "common"]);
	const router = useRouter();
	const navigate = useNavigate();

	const form = useAppForm({
		defaultValues: {
			username: "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = BggUserNameSchema.parse(value);
			navigate({ to: `/games/import/list/${validatedData.username}` });
		},
	});

	const isLoading = false;

	return (
		<BgtPage>
			<BgtPageContent>
				<BgtCenteredCard title={t("import.start.title")} className="max-w-[600px]">
					<form onSubmit={handleFormSubmit(form)}>
						<div className="flex flex-col gap-5 w-full">
							<div>{t("import.start.description")}</div>
							<form.Field name="username" validators={zodValidator(BggUserNameSchema, "username")}>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										disabled={isLoading}
										label={t("import.start.bgg-username.label")}
										type="text"
									/>
								)}
							</form.Field>
							<div className="flex flex-row gap-2">
								<BgtButton
									variant="cancel"
									type="button"
									disabled={isLoading}
									className="flex-none"
									onClick={() => router.history.back()}
								>
									{t("common:cancel")}
								</BgtButton>
								<BgtButton type="submit" disabled={isLoading} className="flex-1" variant="primary">
									{isLoading && <Bars className="size-4" />}
									{t("import.start.button")}
								</BgtButton>
							</div>
						</div>
					</form>
				</BgtCenteredCard>
			</BgtPageContent>
		</BgtPage>
	);
}
