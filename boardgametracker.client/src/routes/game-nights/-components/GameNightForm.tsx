import type { AnyFieldApi } from "@tanstack/react-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import { BgtDateTimePicker, BgtInputField, BgtSelect, BgtTextArea } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import type { Game, Location, Player } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { MultiSelectField } from "./MultiSelectField";

export const GameNightFormSchema = z.object({
	title: z.string().min(1, { message: "game-nights:validation.title-required" }),
	startDate: z.coerce.date({
		error: "player-session:new.start.required",
	}),
	locationId: z.number().min(1, { message: "game-nights:validation.location-required" }),
	hostId: z.number().min(1, { message: "game-nights:validation.host-required" }),
	notes: z.string().optional(),
});

export interface GameNightFormValues {
	title: string;
	startDate: Date;
	locationId: number;
	hostId: number;
	notes: string;
	suggestedGameIds: number[];
	invitedPlayerIds: number[];
}

interface Props {
	defaultValues?: Partial<GameNightFormValues>;
	players: Player[];
	games: Game[];
	locations: Location[];
	isLoading: boolean;
	onSubmit: (values: GameNightFormValues) => Promise<void>;
	children?: React.ReactNode;
	onClose: () => void;
}

export const GameNightForm = (props: Props) => {
	const { defaultValues, players, games, locations, isLoading, onSubmit, children, onClose } = props;
	const { t } = useTranslation("game-nights");

	const playerOptions = useMemo(
		() =>
			players.map((p) => ({
				value: p.id,
				label: p.name,
				image: p.image,
			})),
		[players],
	);

	const gameOptions = useMemo(
		() =>
			games.map((g) => ({
				value: g.id,
				label: g.title,
				image: g.image,
			})),
		[games],
	);

	const locationOptions = useMemo(
		() =>
			locations.map((l) => ({
				value: l.id,
				label: l.name,
			})),
		[locations],
	);

	const hostOptions = useMemo(
		() =>
			players.map((p) => ({
				value: p.id,
				label: p.name,
				image: p.image,
			})),
		[players],
	);

	const form = useAppForm({
		defaultValues: {
			title: defaultValues?.title ?? "",
			startDate: defaultValues?.startDate ?? new Date(),
			locationId: defaultValues?.locationId ?? 0,
			hostId: defaultValues?.hostId ?? 0,
			notes: defaultValues?.notes ?? "",
			selectedPlayers: defaultValues?.invitedPlayerIds ?? [],
			selectedGames: defaultValues?.suggestedGameIds ?? [],
		},
		onSubmit: async ({ value }) => {
			const validatedData = GameNightFormSchema.parse(value);
			await onSubmit({
				...validatedData,
				notes: validatedData.notes ?? "",
				suggestedGameIds: value.selectedGames,
				invitedPlayerIds: value.selectedPlayers,
			});
			handleClose();
		},
	});

	const handleClose = () => {
		form.reset();
		onClose();
	};

	return (
		<form id="game-night-form" onSubmit={handleFormSubmit(form)} className="w-full">
			{children}

			<div className="flex flex-col gap-4 mt-3 mb-3 max-h-[60vh] overflow-y-auto pr-2">
				<form.Field name="title" validators={zodValidator(GameNightFormSchema, "title")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							type="text"
							label={t("form.title.label")}
							disabled={isLoading}
							placeholder={t("form.title.placeholder")}
						/>
					)}
				</form.Field>

				<form.Field name="startDate" validators={zodValidator(GameNightFormSchema, "startDate")}>
					{(field: AnyFieldApi) => (
						<BgtDateTimePicker field={field} disabled={isLoading} label={t("form.start.label")} />
					)}
				</form.Field>

				<div className="grid grid-cols-1 md:grid-cols-2 gap-4">
					<form.Field name="hostId" validators={zodValidator(GameNightFormSchema, "hostId")}>
						{(field: AnyFieldApi) => (
							<BgtSelect
								field={field}
								hasSearch
								items={hostOptions}
								label={t("form.host.label")}
								disabled={isLoading}
								placeholder={t("form.host.placeholder")}
							/>
						)}
					</form.Field>
					<form.Field name="locationId" validators={zodValidator(GameNightFormSchema, "locationId")}>
						{(field: AnyFieldApi) => (
							<BgtSelect
								field={field}
								hasSearch
								items={locationOptions}
								label={t("form.location.label")}
								disabled={isLoading}
								placeholder={t("form.location.placeholder")}
							/>
						)}
					</form.Field>
				</div>

				<form.Field name="selectedPlayers">
					{(field: AnyFieldApi) => (
						<MultiSelectField
							label={t("form.invited-players.label")}
							options={playerOptions}
							selected={field.state.value}
							disabled={isLoading}
							onChange={(values: number[]) => field.handleChange(values)}
							placeholder={t("form.invited-players.description")}
						/>
					)}
				</form.Field>

				<form.Field name="selectedGames">
					{(field: AnyFieldApi) => (
						<MultiSelectField
							label={t("form.suggested-games.label")}
							options={gameOptions}
							selected={field.state.value}
							disabled={isLoading}
							onChange={(values: number[]) => field.handleChange(values)}
							placeholder={t("form.suggested-games.description")}
						/>
					)}
				</form.Field>

				<form.Field name="notes" validators={zodValidator(GameNightFormSchema, "notes")}>
					{(field: AnyFieldApi) => <BgtTextArea field={field} label={t("form.notes.label")} disabled={isLoading} />}
				</form.Field>
			</div>
		</form>
	);
};
