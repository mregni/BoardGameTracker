import { type AnyFieldApi, useStore } from "@tanstack/react-form";
import { useTranslation } from "react-i18next";
import { BgtSwitch } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { gameFormOpts } from "../-utils/gameFormOpts";

export const GameFormCoopFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation("game");
		const hasCoopMode = useStore(form.store, (state) => state.values.hasCoopMode);

		return (
			<div className="flex flex-col gap-2">
				<form.Field name="hasCoopMode">
					{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("coop.mode.label")} disabled={disabled} />}
				</form.Field>
				{hasCoopMode && (
					<div className="flex flex-col gap-2 pl-[50px]">
						<form.Field name="coopByDefault">
							{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("coop.default.label")} disabled={disabled} />}
						</form.Field>
						<BgtText size="2" color="gray">
							{t("coop.hint")}
						</BgtText>
					</div>
				)}
			</div>
		);
	},
});
