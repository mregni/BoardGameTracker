import type { AnyFieldApi } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormTimeFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation();

		return (
			<>
				<div className="flex flex-row gap-2">
					<form.Field name="minPlayTime" validators={zodValidator(CreateGameSchema, "minPlayTime")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								label={t("game.new.manual.min-time.label")}
								type="number"
								disabled={disabled}
								className="pr-2"
								suffixLabel={t("common.minutes-abbreviation")}
							/>
						)}
					</form.Field>
					<form.Field name="maxPlayTime" validators={zodValidator(CreateGameSchema, "maxPlayTime")}>
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								label={t("game.new.manual.max-time.label")}
								type="number"
								disabled={disabled}
								className="pr-2"
								suffixLabel={t("common.minutes-abbreviation")}
							/>
						)}
					</form.Field>
				</div>
				<form.Field name="minAge" validators={zodValidator(CreateGameSchema, "minAge")}>
					{(field: AnyFieldApi) => (
						<BgtInputField
							field={field}
							label={t("game.new.manual.min-age.label")}
							type="number"
							disabled={disabled}
							className="pr-2"
						/>
					)}
				</form.Field>
			</>
		);
	},
});
