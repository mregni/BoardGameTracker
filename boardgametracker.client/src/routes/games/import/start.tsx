import type { AnyFieldApi } from "@tanstack/react-form";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, redirect, useNavigate, useRouter } from "@tanstack/react-router";
import { type ChangeEvent, useRef } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtInputField } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import { BggUserNameSchema, isApiError } from "@/models";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { useToasts } from "@/routes/-hooks/useToasts";
import { uploadCsvImportCall } from "@/services/gameService";
import { csvImportGamesKey, csvImportRejectedKey } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { translateApiError } from "@/utils/errorUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";

export const Route = createFileRoute("/games/import/start")({
	component: WriteRouteComponent,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.bggStatus?.isConfigured) {
			throw redirect({ to: "/games" });
		}
	},
});

function RouteComponent() {
	const { t } = useTranslation(["games", "common"]);
	const router = useRouter();
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const { errorToast } = useToasts();
	const fileInputRef = useRef<HTMLInputElement>(null);

	const uploadCsv = useMutation({
		mutationFn: uploadCsvImportCall,
		onSuccess: (result) => {
			queryClient.setQueryData(csvImportGamesKey, result.games);
			queryClient.setQueryData(csvImportRejectedKey, result.rejected);
			navigate({ to: "/games/import/csv" });
		},
		onError: (error) => {
			errorToast(
				isApiError(error)
					? translateApiError(error.message, "games:import.csv.upload-failed", error.params)
					: "games:import.csv.upload-failed",
			);
		},
	});

	const onCsvSelected = (event: ChangeEvent<HTMLInputElement>) => {
		const file = event.target.files?.[0];
		event.target.value = "";
		if (file) uploadCsv.mutate(file);
	};

	const form = useAppForm({
		defaultValues: {
			username: "",
		},
		onSubmit: ({ value }) => {
			const validatedData = BggUserNameSchema.parse(value);
			navigate({ to: `/games/import/list/${validatedData.username}` });
		},
	});

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard title={t("import.start.title")} className="max-w-150">
					<form onSubmit={handleFormSubmit(form)}>
						<div className="flex flex-col gap-5 w-full">
							<div>{t("import.start.description")}</div>
							<form.Field name="username" validators={zodValidator(BggUserNameSchema, "username")}>
								{(field: AnyFieldApi) => (
									<BgtInputField field={field} label={t("import.start.bgg-username.label")} type="text" />
								)}
							</form.Field>
							<div className="flex flex-row gap-2">
								<BgtButton variant="cancel" type="button" className="flex-none" onClick={() => router.history.back()}>
									{t("common:cancel")}
								</BgtButton>
								<BgtButton type="submit" className="flex-1" variant="primary">
									{t("import.start.button")}
								</BgtButton>
							</div>
							<div className="flex flex-col gap-3 border-t border-white/10 pt-5">
								<div>{t("import.start.csv.description")}</div>
								<input
									ref={fileInputRef}
									type="file"
									accept=".csv,text/csv"
									className="hidden"
									aria-label={t("import.start.csv.button")}
									onChange={onCsvSelected}
								/>
								<BgtButton
									type="button"
									variant="primary"
									disabled={uploadCsv.isPending}
									onClick={() => fileInputRef.current?.click()}
								>
									{uploadCsv.isPending ? t("import.start.csv.uploading") : t("import.start.csv.button")}
								</BgtButton>
							</div>
						</div>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
