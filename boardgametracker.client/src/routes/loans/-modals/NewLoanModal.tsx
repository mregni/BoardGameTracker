import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { useEffect, useMemo, useRef, useState } from "react";
import { ToggleButton, ToggleButtonGroup } from "react-aria-components";
import { useTranslation } from "react-i18next";
import Plus from "@/assets/icons/plus.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtDatePicker, BgtSelect } from "@/components/BgtForm";
import { BgtFieldLabel } from "@/components/BgtForm/BgtFieldLabel";
import { useAppForm } from "@/hooks/form";
import { GameState, type ModalProps, type Player } from "@/models";
import { CreateLoanSchema } from "@/models/Loan/CreateLoan";
import { CreatePlayerModal } from "@/routes/players/-modals/CreatePlayerModal";
import { getSettings } from "@/services/queries/settings";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { notBeforeValidator, zodValidator } from "@/utils/zodValidator";
import { useNewLoanModal } from "../-hooks/useNewLoanModal";
import { DUE_PRESETS, type DuePreset, formatLoanDate, getPresetDueDate } from "../-utils/loanStatus";

const DEFAULT_PRESET: DuePreset = "two-weeks";

const defaultValues = () => {
	const loanDate = toInputDate(undefined, true);
	return {
		gameId: "" as number | "",
		playerId: "",
		loanDate,
		duePreset: DEFAULT_PRESET as DuePreset,
		dueDate: getPresetDueDate(loanDate, DEFAULT_PRESET),
	};
};

const NewLoanModal = (props: ModalProps) => {
	const { open, close } = props;
	const { t, i18n } = useTranslation(["loans", "player-session", "common"]);

	const { data: settings } = useQuery(getSettings());
	const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
	const newlyCreatedPlayerIdRef = useRef<number | null>(null);

	const onSuccess = () => {
		form.reset(defaultValues());
		close();
	};

	const { games, players, isLoading, saveLoan } = useNewLoanModal({
		onSuccess,
	});

	const gamesSelectItems = useMemo(
		() =>
			games
				.filter((x) => x.state === GameState.Owned)
				.map((x) => ({
					value: x.id,
					label: x.title,
					image: x.image,
				})),
		[games],
	);

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
			await saveLoan(CreateLoanSchema.parse(value));
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

	const selectPreset = (preset: DuePreset, loanDate: string) => {
		form.setFieldValue("duePreset", preset);
		if (preset !== "custom") {
			form.setFieldValue("dueDate", getPresetDueDate(loanDate, preset));
		}
	};

	const presetLabel = (preset: DuePreset, selected: boolean, dueDate: string) => {
		const label = t(`new.due.${preset}`);
		return selected && preset !== "custom" && dueDate
			? `${label} · ${formatLoanDate(dueDate, settings?.dateFormat, i18n.language)}`
			: label;
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent aria-describedby={undefined}>
				<form onSubmit={handleFormSubmit(form)} className="w-full">
					<BgtDialogTitle>{t("new.title")}</BgtDialogTitle>
					<div className="mt-3 mb-3 flex w-full flex-col gap-3">
						<form.Field name="gameId" validators={zodValidator(CreateLoanSchema, "gameId")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									hasSearch
									items={gamesSelectItems}
									label={t("player-session:new.game.label")}
									disabled={isLoading}
									placeholder={t("player-session:new.game.placeholder")}
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
								<Plus className="size-3.5" aria-hidden="true" />
								{t("player-session:new.create-player")}
							</BgtButton>
						</div>
						<form.Field
							name="loanDate"
							validators={zodValidator(CreateLoanSchema, "loanDate")}
							listeners={{
								onChange: ({ value }) => {
									const preset = form.getFieldValue("duePreset");
									if (preset !== "custom") {
										form.setFieldValue("dueDate", getPresetDueDate(value, preset));
									}
								},
							}}
						>
							{(field: AnyFieldApi) => (
								<BgtDatePicker
									field={field}
									label={t("new.start.label")}
									disabled={isLoading}
									placeholder={t("new.start.placeholder")}
								/>
							)}
						</form.Field>
						<form.Subscribe
							selector={(state) => [state.values.loanDate, state.values.duePreset, state.values.dueDate] as const}
						>
							{([loanDate, duePreset, dueDate]) => (
								<div className="flex flex-col gap-2">
									<BgtFieldLabel>{t("new.end.label")}</BgtFieldLabel>
									<ToggleButtonGroup
										selectionMode="single"
										disallowEmptySelection
										selectedKeys={[duePreset]}
										onSelectionChange={(keys) => {
											const [preset] = [...keys] as DuePreset[];
											if (preset) {
												selectPreset(preset, loanDate);
											}
										}}
										isDisabled={isLoading}
										aria-label={t("new.end.label")}
										className="flex flex-wrap gap-1.5"
									>
										{DUE_PRESETS.map((preset) => (
											<ToggleButton
												key={preset}
												id={preset}
												className={({ isSelected }) =>
													cx(
														"rounded-full border px-3 py-1.5 text-xs transition-colors outline-none data-[focus-visible]:ring-2 data-[focus-visible]:ring-primary",
														isSelected
															? "border-primary bg-primary/30 text-white"
															: "border-primary/20 bg-primary/10 text-white/80 hover:border-primary/50",
													)
												}
											>
												{presetLabel(preset, preset === duePreset, dueDate)}
											</ToggleButton>
										))}
									</ToggleButtonGroup>
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
													label={t("new.due.custom-label")}
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
					<BgtDialogFooter onCancel={close} confirmLabel={t("new.save")} confirmType="submit" loading={isLoading} />
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
