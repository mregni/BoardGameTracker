import type { AnyFieldApi } from "@tanstack/react-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtDateTimePicker, BgtInputField, BgtSelect, BgtTextArea } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateSessionSchema, type Game, type Location } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { sessionFormOpts } from "../-utils/sessionFormOpts";

export const SessionFormFields = withForm({
	...sessionFormOpts,
	props: {
		games: [] as Game[],
		locations: [] as Location[],
		disabled: false,
	},
	render: function Render({ form, games, locations, disabled }) {
		const { t } = useTranslation();

		const gamesSelectItems = useMemo(
			() =>
				games?.map((x: Game) => ({
					value: x.id,
					label: x.title,
					image: x.image,
				})) ?? [],
			[games],
		);

		const locationsSelectItems = useMemo(
			() =>
				locations?.map((x: Location) => ({
					value: x.id,
					label: x.name,
				})) ?? [],
			[locations],
		);

		return (
			<>
				<form.Field name="gameId" validators={zodValidator(CreateSessionSchema, "gameId")}>
					{(field: AnyFieldApi) => (
						<BgtSelect
							field={field}
							hasSearch
							items={gamesSelectItems}
							label={t("player-session.new.game.label")}
							disabled={disabled}
							placeholder={t("player-session.new.game.placeholder")}
						/>
					)}
				</form.Field>
				<form.Field name="locationId" validators={zodValidator(CreateSessionSchema, "locationId")}>
					{(field: AnyFieldApi) => (
						<BgtSelect
							field={field}
							hasSearch
							items={locationsSelectItems}
							label={t("player-session.new.location.label")}
							disabled={disabled}
							placeholder={t("player-session.new.location.placeholder")}
						/>
					)}
				</form.Field>
				<form.Field name="minutes" validators={zodValidator(CreateSessionSchema, "minutes")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							type="number"
							disabled={disabled}
							label={t("player-session.new.duration.label")}
							placeholder={t("player-session.new.duration.placeholder")}
						/>
					)}
				</form.Field>
				<form.Field name="start" validators={zodValidator(CreateSessionSchema, "start")}>
					{(field: AnyFieldApi) => (
						<BgtDateTimePicker field={field} disabled={disabled} label={t("player-session.new.start.label")} />
					)}
				</form.Field>
				<form.Field name="comment" validators={zodValidator(CreateSessionSchema, "comment")}>
					{(field: AnyFieldApi) => (
						<BgtTextArea field={field} disabled={disabled} label={t("player-session.new.comment.label")} />
					)}
				</form.Field>
			</>
		);
	},
});
