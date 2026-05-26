import type { AnyFieldApi } from "@tanstack/react-form";
import { createFileRoute, useNavigate, useRouter } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtDatePicker, BgtInputField, BgtSelect, BgtSwitch } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import { BggSearchSchema, type Game, GameState } from "@/models";
import { getSettings } from "@/services/queries/settings";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useBggGameModal } from "./-hooks/useBggGameModal";

export const Route = createFileRoute("/games/bgg")({
	component: RouteComponent,
	loader: async ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getSettings());
	},
});

function RouteComponent() {
	const { t } = useTranslation(["game", "common"]);
	const navigate = useNavigate();
	const router = useRouter();

	const onSuccess = useCallback(
		(game: Game) => {
			navigate({ to: `/games/${game.id}` });
		},
		[navigate],
	);

	const { save, isPending, settings } = useBggGameModal({ onSuccess });

	const openBgg = useCallback(() => {
		window.open("https://boardgamegeek.com/browse/boardgame", "_blank", "noopener,noreferrer");
	}, []);

	const handleCancel = useCallback(() => {
		router.history.back();
	}, [router]);

	const form = useAppForm({
		defaultValues: {
			bggId: "",
			price: 0,
			date: toInputDate(undefined, true),
			state: GameState.Owned,
			hasScoring: true,
		},
		onSubmit: async ({ value }) => {
			const validatedData = BggSearchSchema.parse(value);
			await save(validatedData).catch(() => {});
		},
	});

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCard title={t("new.title")} className="w-full max-w-xl">
					<p className="text-cancel mb-4">{t("new.bgg-description")}</p>
					<form onSubmit={handleFormSubmit(form)} className="w-full">
						<div className="flex flex-col gap-4 mb-6">
							<BgtButton onClick={openBgg} disabled={isPending} variant="primary" type="button">
								<SquareOutIcon className="size-5" />
								{t("bgg.external-page")}
							</BgtButton>
							<form.Field name="bggId" validators={zodValidator(BggSearchSchema, "bggId")}>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										disabled={isPending}
										label={t("bgg.label")}
										type="text"
										placeholder={t("bgg.placeholder")}
									/>
								)}
							</form.Field>
							<form.Field name="price" validators={zodValidator(BggSearchSchema, "price")}>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										disabled={isPending}
										label={t("price.label")}
										type="number"
										placeholder={t("price.placeholder")}
										prefixLabel={settings?.currency}
									/>
								)}
							</form.Field>
							<form.Field name="date" validators={zodValidator(BggSearchSchema, "date")}>
								{(field: AnyFieldApi) => (
									<BgtDatePicker
										field={field}
										disabled={isPending}
										label={t("added-date.label")}
										placeholder={t("added-date.placeholder")}
									/>
								)}
							</form.Field>
							<form.Field name="state" validators={zodValidator(BggSearchSchema, "state")}>
								{(field: AnyFieldApi) => (
									<BgtSelect
										field={field}
										disabled={isPending}
										label={t("state.label")}
										items={Object.values(GameState).map((value) => ({
											label: t(getItemStateTranslationKey(value, false)),
											value: value,
										}))}
									/>
								)}
							</form.Field>
							<form.Field name="hasScoring" validators={zodValidator(BggSearchSchema, "hasScoring")}>
								{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("scoring.label")} disabled={isPending} />}
							</form.Field>
						</div>
						<div className="flex justify-between gap-3">
							<BgtButton variant="cancel" type="button" onClick={handleCancel} disabled={isPending}>
								{t("common:cancel")}
							</BgtButton>
							<BgtButton type="submit" variant="primary" disabled={isPending} className="flex-1">
								{t("new.save")}
							</BgtButton>
						</div>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
