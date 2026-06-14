import type { AnyFieldApi } from "@tanstack/react-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtDatePicker, BgtInputField, BgtSelect, BgtTextArea } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema, GameState } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { COMMON_LANGUAGE_CODES, getLanguageName, LANGUAGE_INDEPENDENT, LANGUAGE_NONE } from "@/utils/languageUtils";
import { zodValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormPlayerFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
		currency: undefined as string | undefined,
	},
	render: function Render({ form, disabled, currency }) {
		const { t, i18n } = useTranslation("game");

		const languageItems = useMemo(
			() => [
				{ value: LANGUAGE_NONE, label: t("language.none") },
				{ value: LANGUAGE_INDEPENDENT, label: t("language-independent") },
				...COMMON_LANGUAGE_CODES.map((code) => ({ value: code, label: getLanguageName(code, i18n.language) })),
			],
			[t, i18n.language],
		);

		return (
			<>
				<form.Field name="bggId" validators={zodValidator(CreateGameSchema, "bggId")}>
					{(field: AnyFieldApi) => (
						<BgtInputField field={field} type="number" disabled={disabled} label={t("bgg.placeholder")} />
					)}
				</form.Field>
				<form.Field name="buyingPrice" validators={zodValidator(CreateGameSchema, "buyingPrice")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("price.label")}
							type="number"
							placeholder={t("price.placeholder")}
							disabled={disabled}
							suffixLabel={currency}
						/>
					)}
				</form.Field>
				<form.Field name="shopUrl" validators={zodValidator(CreateGameSchema, "shopUrl")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("shop-url.label")}
							type="text"
							placeholder={t("shop-url.placeholder")}
							disabled={disabled}
						/>
					)}
				</form.Field>
				<form.Field name="additionDate" validators={zodValidator(CreateGameSchema, "additionDate")}>
					{(field: AnyFieldApi) => (
						<BgtDatePicker
							field={field}
							label={t("added-date.label")}
							disabled={disabled}
							placeholder={t("added-date.placeholder")}
						/>
					)}
				</form.Field>
				<form.Field name="state" validators={zodValidator(CreateGameSchema, "state")}>
					{(field: AnyFieldApi) => (
						<BgtSelect
							field={field}
							label={t("state.label")}
							disabled={disabled}
							items={Object.values(GameState).map((value) => ({
								label: t(getItemStateTranslationKey(value, false)),
								value: value,
							}))}
						/>
					)}
				</form.Field>
				<form.Field name="language" validators={zodValidator(CreateGameSchema, "language")}>
					{(field: AnyFieldApi) => (
						<BgtSelect field={field} label={t("language.label")} disabled={disabled} hasSearch items={languageItems} />
					)}
				</form.Field>
				<form.Field name="yearPublished" validators={zodValidator(CreateGameSchema, "yearPublished")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("new.manual.year.label")}
							type="number"
							disabled={disabled}
							className="pr-2"
						/>
					)}
				</form.Field>
				<form.Field name="description" validators={zodValidator(CreateGameSchema, "description")}>
					{(field: AnyFieldApi) => (
						<BgtTextArea field={field} label={t("new.manual.description.label")} disabled={disabled} />
					)}
				</form.Field>
				<div className="flex flex-row gap-2">
					<form.Field name="minPlayers" validators={zodValidator(CreateGameSchema, "minPlayers")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								label={t("new.manual.min-players.label")}
								type="number"
								disabled={disabled}
								className="pr-2"
							/>
						)}
					</form.Field>
					<form.Field name="maxPlayers" validators={zodValidator(CreateGameSchema, "maxPlayers")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								label={t("new.manual.max-players.label")}
								type="number"
								disabled={disabled}
								className="pr-2"
							/>
						)}
					</form.Field>
				</div>
			</>
		);
	},
});
