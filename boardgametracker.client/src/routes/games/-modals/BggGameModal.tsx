import type { AnyFieldApi } from "@tanstack/react-form";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import SquareOutIcon from "@/assets/icons/square-out.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtDatePicker, BgtInputField, BgtSelect, BgtSwitch } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import { BggSearchSchema, type Game, GameState, type ModalProps } from "@/models";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useBggGameModal } from "../-hooks/useBggGameModal";

export const BggGameModal = (props: ModalProps) => {
	const { open, close } = props;
	const { t } = useTranslation();
	const navigate = useNavigate();

	const onSuccess = useCallback(
		(game: Game) => {
			navigate({ to: `/games/${game.id}` });
		},
		[navigate],
	);

	const { save, isPending, settings } = useBggGameModal({ onSuccess });

	const openBgg = useCallback(() => {
		window.open("https://boardgamegeek.com/browse/boardgame", "_blank");
	}, []);

	const handleClose = useCallback(() => {
		close();
	}, [close]);

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
			await save(validatedData);
		},
	});

	return (
		<BgtDialog open={open} onClose={handleClose}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("game.new.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("game.new.bgg-description")}</BgtDialogDescription>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-col gap-4 mt-3 mb-6">
						<BgtButton onClick={openBgg} disabled={isPending} variant="primary" type="button">
							<SquareOutIcon className="size-5" />
							{t("game.bgg.external-page")}
						</BgtButton>
						<form.Field name="bggId" validators={zodValidator(BggSearchSchema, "bggId")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={isPending}
									label={t("game.bgg.label")}
									type="text"
									placeholder={t("game.bgg.placeholder")}
								/>
							)}
						</form.Field>
						<form.Field name="price" validators={zodValidator(BggSearchSchema, "price")}>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									disabled={isPending}
									label={t("game.price.label")}
									type="number"
									placeholder={t("game.price.placeholder")}
									prefixLabel={settings?.currency}
								/>
							)}
						</form.Field>
						<form.Field name="date" validators={zodValidator(BggSearchSchema, "date")}>
							{(field: AnyFieldApi) => (
								<BgtDatePicker
									field={field}
									disabled={isPending}
									label={t("game.added-date.label")}
									placeholder={t("game.added-date.placeholder")}
								/>
							)}
						</form.Field>
						<form.Field name="state" validators={zodValidator(BggSearchSchema, "state")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									disabled={isPending}
									label={t("game.state.label")}
									items={Object.values(GameState).map((value) => ({
										label: t(getItemStateTranslationKey(value, false)),
										value: value,
									}))}
								/>
							)}
						</form.Field>
						<form.Field name="hasScoring" validators={zodValidator(BggSearchSchema, "hasScoring")}>
							{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("game.scoring.label")} disabled={isPending} />}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton variant="cancel" onClick={handleClose} disabled={isPending}>
							{t("common.cancel")}
						</BgtButton>
						<BgtButton type="submit" variant="primary" disabled={isPending} className="flex-1">
							{t("game.new.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
