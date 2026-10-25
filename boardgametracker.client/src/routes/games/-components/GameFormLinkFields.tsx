import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import CaretRight from "@/assets/icons/caret-right.svg?react";
import { BgtInputField } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateGameSchema } from "@/models";
import { getSettings } from "@/services/queries/settings";
import { zodValidator } from "@/utils/zodValidator";
import { gameFormOpts } from "../-utils/gameFormOpts";
import { WatchIdPreview } from "./WatchIdPreview";

export const GameFormLinkFields = withForm({
	...gameFormOpts,
	props: {
		disabled: false,
		defaultOpen: false,
	},
	render: function Render({ form, disabled, defaultOpen }) {
		const { t } = useTranslation("game");
		const { data: settings } = useQuery(getSettings());
		const watchEnabled = settings?.changeDetectionStatus?.isConfigured === true;

		return (
			<details className="group lg:col-span-2" open={defaultOpen}>
				<summary className="flex cursor-pointer list-none items-center gap-1 text-sm font-semibold text-white/80 [&::-webkit-details-marker]:hidden">
					<CaretRight className="size-4 transition-transform group-open:rotate-90" aria-hidden="true" />
					{t("links.title")}
				</summary>
				<div className="mt-3 grid grid-cols-1 gap-x-4 gap-y-3 lg:grid-cols-2">
					<form.Field name="bggId" validators={zodValidator(CreateGameSchema, "bggId")}>
						{(field: AnyFieldApi) => (
							<BgtInputField field={field} type="number" disabled={disabled} label={t("bgg.placeholder")} />
						)}
					</form.Field>
					{watchEnabled && (
						<div>
							<form.Field
								name="changeDetectionWatchId"
								validators={zodValidator(CreateGameSchema, "changeDetectionWatchId")}
							>
								{(field: AnyFieldApi) => (
									<BgtInputField
										field={field}
										label={t("watch-id.label")}
										type="text"
										placeholder={t("watch-id.placeholder")}
										disabled={disabled}
									/>
								)}
							</form.Field>
							<form.Subscribe
								selector={(state: { values: { changeDetectionWatchId?: string } }) =>
									state.values.changeDetectionWatchId
								}
							>
								{(watchId?: string) => <WatchIdPreview watchId={watchId} />}
							</form.Subscribe>
						</div>
					)}
				</div>
			</details>
		);
	},
});
