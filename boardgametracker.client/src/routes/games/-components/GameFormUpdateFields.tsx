import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormUpdateFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
		currency: undefined as string | undefined,
	},
	render: function Render({ form, disabled, currency }) {
		const { t } = useTranslation("game");

		return (
			<>
				<form.Field name="rating" validators={zodValidator(CreateGameSchema, "rating")}>
					{(field: AnyFieldApi) => (
						<BgtInputField field={field} label={t("update.rating.label")} type="number" disabled={disabled} />
					)}
				</form.Field>
				<form.Field name="weight" validators={zodValidator(CreateGameSchema, "weight")}>
					{(field: AnyFieldApi) => (
						<BgtInputField field={field} label={t("update.weight.label")} type="number" disabled={disabled} />
					)}
				</form.Field>
				<form.Field name="soldPrice" validators={zodValidator(CreateGameSchema, "soldPrice")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("update.sold-price.label")}
							type="number"
							disabled={disabled}
							prefixLabel={currency}
						/>
					)}
				</form.Field>
			</>
		);
	},
});
