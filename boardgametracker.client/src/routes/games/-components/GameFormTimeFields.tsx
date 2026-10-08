import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema } from "@/models";
import { rangeValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormTimeFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation(["game", "common"]);

		return (
			<>
				<form.Field
					name="minPlayTime"
					validators={rangeValidator(CreateGameSchema, "minPlayTime", "maxPlayTime", "min")}
				>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("new.manual.min-time.label")}
							type="number"
							disabled={disabled}
							suffixLabel={t("common:minutes-abbreviation")}
						/>
					)}
				</form.Field>
				<form.Field
					name="maxPlayTime"
					validators={rangeValidator(CreateGameSchema, "maxPlayTime", "minPlayTime", "max")}
				>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("new.manual.max-time.label")}
							type="number"
							disabled={disabled}
							suffixLabel={t("common:minutes-abbreviation")}
						/>
					)}
				</form.Field>
			</>
		);
	},
});
