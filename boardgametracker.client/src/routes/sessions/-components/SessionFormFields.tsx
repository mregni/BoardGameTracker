import type { AnyFieldApi } from "@tanstack/react-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtDateTimePicker, BgtInputField, BgtSelect } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateSessionSchema, type Game, type Location } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { toGameSelectItems } from "../-utils/gameSelectItems";
import { sessionFormOpts } from "../-utils/sessionFormOpts";

export const SessionFormFields = withForm({
	...sessionFormOpts,
	props: {
		games: [] as Game[],
		recentGameIds: [] as number[],
		recentGamesLabel: "",
		locations: [] as Location[],
		disabled: false,
		onGameChange: (() => {}) as (gameId: number) => void,
		onMinutesChange: (() => {}) as (minutes: number | undefined) => void,
	},
	render: function Render({
		form,
		games,
		recentGameIds,
		recentGamesLabel,
		locations,
		disabled,
		onGameChange,
		onMinutesChange,
	}) {
		const { t } = useTranslation(["player-session", "common"]);

		const gameSelectItems = useMemo(
			() => toGameSelectItems(games, recentGameIds, recentGamesLabel, t("form.game.all")),
			[games, recentGameIds, recentGamesLabel, t],
		);

		const locationsSelectItems = useMemo(
			() => [
				{ value: 0, label: t("new.location.none") },
				...(locations?.map((x: Location) => ({
					value: x.id,
					label: x.name,
				})) ?? []),
			],
			[locations, t],
		);

		return (
			<div className="@container flex flex-col gap-3">
				<form.Field
					name="gameId"
					validators={zodValidator(CreateSessionSchema, "gameId")}
					listeners={{ onChange: ({ value }) => onGameChange(value) }}
				>
					{(field: AnyFieldApi) => (
						<BgtSelect
							field={field}
							hasSearch
							items={gameSelectItems}
							label={t("new.game.label")}
							disabled={disabled}
							placeholder={t("new.game.placeholder")}
						/>
					)}
				</form.Field>
				<div className="grid grid-cols-2 @2xl:grid-cols-[minmax(0,1.6fr)_minmax(0,0.7fr)_minmax(0,1fr)] gap-x-3 gap-y-3">
					<div className="col-span-2 @2xl:col-span-1">
						<form.Field name="start" validators={zodValidator(CreateSessionSchema, "start")}>
							{(field: AnyFieldApi) => (
								<BgtDateTimePicker field={field} disabled={disabled} label={t("new.start.label")} />
							)}
						</form.Field>
					</div>
					<form.Field
						name="minutes"
						validators={zodValidator(CreateSessionSchema, "minutes")}
						listeners={{ onChange: ({ value }) => onMinutesChange(value) }}
					>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								type="number"
								inputMode="numeric"
								disabled={disabled}
								label={t("new.duration.label")}
								suffixLabel={t("common:minutes-abbreviation")}
							/>
						)}
					</form.Field>
					<form.Field name="locationId" validators={zodValidator(CreateSessionSchema, "locationId")}>
						{(field: AnyFieldApi) => (
							<BgtSelect
								field={field}
								items={locationsSelectItems}
								label={t("new.location.label")}
								disabled={disabled}
								placeholder={t("new.location.placeholder")}
							/>
						)}
					</form.Field>
				</div>
			</div>
		);
	},
});
