import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import TrophyIcon from "@/assets/icons/trophy.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtInputField, BgtSwitch } from "@/components/BgtForm";
import { BgtFormErrors } from "@/components/BgtForm/BgtFormErrors";
import { withForm } from "@/hooks/form";
import { sessionFormOpts } from "../-utils/sessionFormOpts";

const resultClasses =
	"inline-flex flex-1 items-center justify-center gap-1.5 h-11 md:h-10 px-3 rounded-lg border text-sm font-semibold disabled:cursor-not-allowed";

export const SessionCoopSwitch = withForm({
	...sessionFormOpts,
	props: {
		disabled: false,
	},
	render: function Render({ form, disabled }) {
		const { t } = useTranslation("player-session");

		return (
			<form.Field name="isCoop">
				{(field: AnyFieldApi) => <BgtSwitch field={field} label={t("coop.played")} disabled={disabled} />}
			</form.Field>
		);
	},
});

export const SessionTeamResult = withForm({
	...sessionFormOpts,
	props: {
		hasScoring: true,
		disabled: false,
	},
	render: function Render({ form, hasScoring, disabled }) {
		const { t } = useTranslation("player-session");

		return (
			<div className={cx("grid gap-3", hasScoring && "grid-cols-[minmax(0,1fr)_6.5rem]")}>
				<form.Field
					name="teamWon"
					validators={{
						onSubmit: ({ value }: { value: boolean | null }) =>
							value === null ? t("coop.result-required") : undefined,
					}}
				>
					{(field: AnyFieldApi) => (
						<fieldset className="m-0 flex min-w-0 flex-col border-0 p-0">
							<legend className="p-0 text-[15px] font-medium leading-7">{t("coop.result")}</legend>
							<div className="flex gap-2">
								<button
									type="button"
									aria-pressed={field.state.value === true}
									disabled={disabled}
									onClick={() => field.handleChange(true)}
									className={cx(
										resultClasses,
										field.state.value === true
											? "border-ok bg-ok/15 text-ok"
											: "border-primary/30 text-white/60 hover:border-primary/60 hover:text-white",
									)}
								>
									<TrophyIcon className="size-4" />
									{t("coop.won")}
								</button>
								<button
									type="button"
									aria-pressed={field.state.value === false}
									disabled={disabled}
									onClick={() => field.handleChange(false)}
									className={cx(
										resultClasses,
										field.state.value === false
											? "border-bad bg-bad/15 text-bad"
											: "border-primary/30 text-white/60 hover:border-primary/60 hover:text-white",
									)}
								>
									<XIcon className="size-4" />
									{t("coop.lost")}
								</button>
							</div>
							<BgtFormErrors errors={field.state.meta.errors} />
						</fieldset>
					)}
				</form.Field>
				{hasScoring && (
					<form.Field name="teamScore">
						{(field: AnyFieldApi) => (
							<BgtInputField
								field={field}
								type="number"
								inputMode="numeric"
								disabled={disabled}
								label={t("coop.team-score")}
							/>
						)}
					</form.Field>
				)}
			</div>
		);
	},
});
