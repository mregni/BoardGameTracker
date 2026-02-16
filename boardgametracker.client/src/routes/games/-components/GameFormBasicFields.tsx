import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormBasicFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation();

		return (
			<form.Field name="title" validators={zodValidator(CreateGameSchema, "title")}>
				{(field: AnyFieldApi) => (
					<BgtInputField field={field} type="text" disabled={disabled} label={t("game.new.manual.game-title.label")} />
				)}
			</form.Field>
		);
	},
});
