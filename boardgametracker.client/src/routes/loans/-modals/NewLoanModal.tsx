import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtDatePicker, BgtSelect } from "@/components/BgtForm";
import { BgtFieldLabel } from "@/components/BgtForm/BgtFieldLabel";
import { useAppForm } from "@/hooks/form";
import type { ModalProps, Player } from "@/models";
import { CreateLoanSchema } from "@/models/Loan/CreateLoan";
import { CreatePlayerModal } from "@/routes/players/-modals/CreatePlayerModal";
import { handleFormSubmit } from "@/utils/formUtils";
import { notBeforeValidator, zodValidator } from "@/utils/zodValidator";
import { LoanGamePicker } from "../-components/LoanGamePicker";
import { useNewLoanModal } from "../-hooks/useNewLoanModal";
import { buildLoanGameOptions } from "../-utils/loanGameOptions";
import { DUE_PRESETS, type DuePreset, getPresetDueDate, resolveDueDate, toShortDate } from "../-utils/loanStatus";

const DEFAULT_PRESET: DuePreset = "two-weeks";

const defaultValues = () => ({
	gameId: "" as number | "",
	playerId: "",
	loanDate: format(new Date(), "yyyy-MM-dd"),
	duePreset: DEFAULT_PRESET as DuePreset,
	dueDate: "",
});

const NewLoanModal = (props: ModalProps) => {
	const { open, close } = props;
	const { t, i18n } = useTranslation(["loans", "player-session", "common"]);

	const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
	const newlyCreatedPlayerIdRef = useRef<number | null>(null);

	const onSuccess = () => {
		form.reset(defaultValues());
		close();
	};

	const { games, players, loans, isLoading, saveLoan } = useNewLoanModal({
		onSuccess,
	});

	const gameOptions = useMemo(() => buildLoanGameOptions(games, loans, players), [games, loans, players]);

	const playersSelectItems = useMemo(
		() =>
			players.map((x) => ({
				value: x.id,
				label: x.name,
				image: x.image,
			})),
		[players],
	);

	const form = useAppForm({
		defaultValues: defaultValues(),
		onSubmit: async ({ value }) => {
			const validatedData = CreateLoanSchema.parse({
				gameId: value.gameId,
				playerId: value.playerId,
				loanDate: value.loanDate,
				dueDate: resolveDueDate(value.loanDate, value.duePreset, value.dueDate),
			});
			await saveLoan(validatedData);
		},
	});

	const handlePlayerCreated = (player: Player) => {
		newlyCreatedPlayerIdRef.current = player.id;
	};

	useEffect(() => {
		if (newlyCreatedPlayerIdRef.current !== null) {
			const playerExists = players.some((p) => p.id === newlyCreatedPlayerIdRef.current);
			if (playerExists) {
				form.setFieldValue("playerId", String(newlyCreatedPlayerIdRef.current));
				newlyCreatedPlayerIdRef.current = null;
			}
		}
	}, [players, form]);

	useEffect(() => {
		if (!open) {
			form.reset(defaultValues());
		}
	}, [open, form]);

	const presetLabel = (preset: DuePreset, loanDate: string, selected: boolean) => {
		const label = t(`new.due.${preset}`);
		const date = getPresetDueDate(loanDate, preset);
		return selected && date ? `${label} · ${toShortDate(date, i18n.language)}` : label;
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("new.title")}</BgtDialogTitle>
					<BgtDialogDescription>{t("new.description")}</BgtDialogDescription>
					<div className="flex flex-col gap-3 mt-3 mb-3 w-full">
						<form.Field name="gameId" validators={zodValidator(CreateLoanSchema, "gameId")}>
							{(field: AnyFieldApi) => (
								<LoanGamePicker
									label={t("new.game.label")}
									options={gameOptions}
									value={typeof field.state.value === "number" ? field.state.value : null}
									onChange={(gameId) => field.handleChange(gameId)}
									errors={field.state.meta.errors}
									disabled={isLoading}
								/>
							)}
						</form.Field>
						<form.Field name="playerId" validators={zodValidator(CreateLoanSchema, "playerId")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									hasSearch
									showAvatars
									items={playersSelectItems}
									label={t("new.player.label")}
									disabled={isLoading}
									placeholder={t("new.player.placeholder")}
								/>
							)}
						</form.Field>
						<div>
							<BgtButton type="button" variant="text" size="1" onClick={() => setOpenCreatePlayerModal(true)}>
								+ {t("player-session:new.create-player")}
							</BgtButton>
						</div>
						<form.Field name="loanDate" validators={zodValidator(CreateLoanSchema, "loanDate")}>
							{(field: AnyFieldApi) => (
								<BgtDatePicker
									field={field}
									label={t("new.start.label")}
									disabled={isLoading}
									placeholder={t("new.start.placeholder")}
								/>
							)}
						</form.Field>
						<form.Subscribe selector={(state) => [state.values.loanDate, state.values.duePreset] as const}>
							{([loanDate, duePreset]) => (
								<div className="flex flex-col gap-2">
									<BgtFieldLabel>{t("new.end.label")}</BgtFieldLabel>
									<div className="flex flex-wrap gap-1.5">
										{DUE_PRESETS.map((preset) => {
											const selected = preset === duePreset;
											return (
												<button
													key={preset}
													type="button"
													aria-pressed={selected}
													disabled={isLoading}
													onClick={() => {
														form.setFieldValue("duePreset", preset);
														if (preset !== "custom") {
															form.setFieldValue("dueDate", "");
														}
													}}
													className={cx(
														"rounded-full border px-3 py-1.5 text-xs transition-colors",
														selected
															? "border-primary bg-primary/30 text-white"
															: "border-primary/20 bg-primary/10 text-white/80 hover:border-primary/50",
													)}
												>
													{presetLabel(preset, loanDate, selected)}
												</button>
											);
										})}
									</div>
									{duePreset === "custom" && (
										<form.Field
											name="dueDate"
											validators={notBeforeValidator(
												CreateLoanSchema,
												"dueDate",
												"loanDate",
												"loans:new.end.before-start",
											)}
										>
											{(field: AnyFieldApi) => (
												<BgtDatePicker
													field={field}
													label=""
													disabled={isLoading}
													placeholder={t("new.end.placeholder")}
													minValue={loanDate || null}
												/>
											)}
										</form.Field>
									)}
								</div>
							)}
						</form.Subscribe>
					</div>
					<BgtDialogClose>
						<BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={close}>
							{t("common:cancel")}
						</BgtButton>
						<form.Subscribe selector={(state) => state.values.gameId}>
							{(gameId) => {
								const game = gameOptions.find((option) => option.id === gameId);
								return (
									<BgtButton type="submit" disabled={isLoading} className="flex-1" variant="primary">
										{isLoading && <Bars className="size-4" />}
										{game ? t("new.lend-game", { game: game.title }) : t("new.save")}
									</BgtButton>
								);
							}}
						</form.Subscribe>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>

			<CreatePlayerModal
				open={openCreatePlayerModal}
				close={() => setOpenCreatePlayerModal(false)}
				onPlayerCreated={handlePlayerCreated}
			/>
		</BgtDialog>
	);
};

export default NewLoanModal;
