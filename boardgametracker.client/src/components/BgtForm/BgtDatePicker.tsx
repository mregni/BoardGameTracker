import { type CalendarDate, parseDate } from "@internationalized/date";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { useMemo } from "react";
import {
	Button,
	Calendar,
	CalendarCell,
	CalendarGrid,
	CalendarGridBody,
	CalendarGridHeader,
	CalendarHeaderCell,
	DateInput,
	DatePicker,
	DateSegment,
	type DateValue,
	Dialog,
	Group,
	Heading,
	I18nProvider,
	Popover,
} from "react-aria-components";
import CalendarIcon from "@/assets/icons/calendar.svg?react";
import { getSettings } from "@/services/queries/settings";
import { getDatePickerLocale } from "@/utils/localeUtils";
import { BgtFieldLabel } from "./BgtFieldLabel";
import { BgtFormErrors } from "./BgtFormErrors";

export interface BgtDatePickerProps {
	field: AnyFieldApi;
	label: string;
	disabled?: boolean;
	className?: string;
	/** Legacy placeholder prop. The segmented input shows locale-appropriate placeholders automatically. */
	placeholder?: string;
}

const safeParseDateValue = (iso: string | undefined | null): CalendarDate | null => {
	if (!iso) return null;
	try {
		return parseDate(iso);
	} catch {
		return null;
	}
};

export const BgtDatePicker = (props: BgtDatePickerProps) => {
	const { field, label, disabled = false, className = "" } = props;
	const { data: settings } = useQuery(getSettings());

	const locale = useMemo(() => getDatePickerLocale(settings?.dateFormat), [settings?.dateFormat]);

	const hasErrors = field.state.meta.errors.length > 0;
	const value = safeParseDateValue(field.state.value as string | undefined);

	const handleChange = (date: DateValue | null) => {
		field.handleChange(date ? date.toString() : "");
	};

	return (
		<div className="flex flex-col justify-start w-full">
			{label && (
				<div className="flex items-baseline justify-between">
					<BgtFieldLabel>{label}</BgtFieldLabel>
					<BgtFormErrors errors={field.state.meta.errors} />
				</div>
			)}
			<I18nProvider locale={locale}>
				<DatePicker
					value={value}
					onChange={handleChange}
					isDisabled={disabled}
					isInvalid={hasErrors}
					shouldCloseOnSelect
					aria-label={label || "Date"}
				>
					<Group
						className={cx(
							"w-full bg-background text-white px-4 h-11 md:h-10 rounded-lg border border-primary/30",
							"focus-within:border-primary text-left flex items-center justify-between gap-2",
							className,
							hasErrors && "border-error bg-error/5",
							disabled && "opacity-50 cursor-not-allowed",
						)}
					>
						<DateInput className="flex-1 flex items-center text-base outline-none">
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
						<Button
							className={cx(
								"flex-none text-gray-400 hover:text-white transition-colors",
								"cursor-pointer bg-transparent border-none p-0 outline-none",
								"disabled:cursor-not-allowed disabled:hover:text-gray-400",
							)}
							aria-label="Open calendar"
						>
							<CalendarIcon className="size-5" />
						</Button>
					</Group>
					<Popover
						placement="bottom end"
						offset={5}
						className="bg-background border border-primary/30 rounded-lg p-4 shadow-lg z-50"
					>
						<Dialog className="outline-none">
							<Calendar className="text-white w-72">
								<header className="flex items-center justify-between mb-4">
									<Button
										slot="previous"
										className={cx(
											"bg-primary/20 hover:bg-primary/30 text-white rounded-md size-8",
											"inline-flex items-center justify-center transition-colors",
											"border-none cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed",
										)}
										aria-label="Previous month"
									>
										‹
									</Button>
									<Heading className="text-sm tracking-wider font-medium text-white" />
									<Button
										slot="next"
										className={cx(
											"bg-primary/20 hover:bg-primary/30 text-white rounded-md size-8",
											"inline-flex items-center justify-center transition-colors",
											"border-none cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed",
										)}
										aria-label="Next month"
									>
										›
									</Button>
								</header>
								<CalendarGrid className="w-full border-separate border-spacing-1">
									<CalendarGridHeader>
										{(day) => (
											<CalendarHeaderCell className="text-white/50 text-xs text-center py-2 font-normal">
												{day}
											</CalendarHeaderCell>
										)}
									</CalendarGridHeader>
									<CalendarGridBody>
										{(date) => (
											<CalendarCell
												date={date}
												className={cx(
													"size-9 rounded-md flex items-center justify-center text-sm cursor-pointer",
													"outline-none transition-colors",
													"data-[outside-month]:text-white/20 data-[outside-month]:opacity-50",
													"hover:bg-primary/20",
													"data-[selected]:bg-primary data-[selected]:text-white data-[selected]:hover:bg-primary",
													"data-[disabled]:opacity-30 data-[disabled]:cursor-not-allowed data-[disabled]:hover:bg-transparent",
													"data-[today]:text-[#22d3ee]",
												)}
											/>
										)}
									</CalendarGridBody>
								</CalendarGrid>
							</Calendar>
						</Dialog>
					</Popover>
				</DatePicker>
			</I18nProvider>
		</div>
	);
};
