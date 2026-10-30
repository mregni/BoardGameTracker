import { Time } from "@internationalized/date";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useMemo } from "react";
import { DateInput, DateSegment, I18nProvider, TimeField, type TimeValue } from "react-aria-components";
import { useTranslation } from "react-i18next";
import { getSettings } from "@/services/queries/settings";
import { parseLocalDate } from "@/utils/localDate";
import { getHourCycle } from "@/utils/localeUtils";
import { BgtDatePicker } from "./BgtDatePicker";
import { BgtFieldLabel } from "./BgtFieldLabel";
import { BgtFormErrors } from "./BgtFormErrors";

export interface BgtDateTimePickerProps {
	field: AnyFieldApi;
	label: string;
	disabled?: boolean;
}

export const BgtDateTimePicker = (props: BgtDateTimePickerProps) => {
	const { field, label, disabled = false } = props;
	const { t } = useTranslation();
	const { data: settings } = useQuery(getSettings());

	const hasErrors = field.state.meta.errors.length > 0;
	const hourCycle = getHourCycle(settings?.timeFormat);
	const locale = settings?.uiLanguage || undefined;

	const selectedDate = useMemo(() => parseLocalDate(field.state.value), [field.state.value]);

	const dateField = useMemo(
		() => ({
			state: {
				value: selectedDate ? format(selectedDate, "yyyy-MM-dd") : "",
				meta: field.state.meta,
			},
			handleChange: (dateString: string) => {
				const newDate = parseLocalDate(dateString);
				if (!newDate) return;

				if (selectedDate) {
					newDate.setHours(selectedDate.getHours());
					newDate.setMinutes(selectedDate.getMinutes());
				}
				field.handleChange(newDate);
			},
			handleBlur: field.handleBlur,
			name: field.name,
		}),
		[selectedDate, field],
	);

	const timeValue = selectedDate ? new Time(selectedDate.getHours(), selectedDate.getMinutes()) : null;

	const handleTimeChange = (time: TimeValue | null) => {
		if (!time) return;

		const newDate = selectedDate ? new Date(selectedDate) : new Date();
		newDate.setHours(time.hour);
		newDate.setMinutes(time.minute);
		field.handleChange(newDate);
	};

	const timeField = (
		<TimeField
			value={timeValue}
			onChange={handleTimeChange}
			onBlur={field.handleBlur}
			isDisabled={disabled}
			isInvalid={hasErrors}
			hourCycle={hourCycle}
			shouldForceLeadingZeros
			aria-label={t("common:time")}
			className="w-32"
		>
			<DateInput
				className={cx(
					"w-full bg-background text-white px-4 h-11 md:h-10 rounded-lg border border-primary/30",
					"focus-within:border-primary flex items-center text-base outline-none",
					hasErrors && "border-error bg-error/5",
					disabled && "opacity-50 cursor-not-allowed",
				)}
			>
				{(segment) => (
					<DateSegment
						segment={segment}
						className={cx(
							"outline-none rounded px-0.5 tabular-nums",
							"data-[placeholder]:text-gray-400",
							"data-[focused]:bg-primary/30 data-[focused]:text-white",
							"data-[type=literal]:text-gray-400 data-[type=literal]:px-0",
						)}
					/>
				)}
			</DateInput>
		</TimeField>
	);

	return (
		<div className="flex flex-col justify-start w-full">
			<BgtFieldLabel>{label}</BgtFieldLabel>
			<div className="flex gap-2">
				<div className="flex-1">
					<BgtDatePicker field={dateField as AnyFieldApi} label="" placeholder="" disabled={disabled} />
				</div>
				<I18nProvider locale={locale}>{timeField}</I18nProvider>
			</div>
			<BgtFormErrors errors={field.state.meta.errors} />
		</div>
	);
};
