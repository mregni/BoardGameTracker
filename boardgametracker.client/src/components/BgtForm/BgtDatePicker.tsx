import { type CalendarDate, parseDate } from "@internationalized/date";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { useMemo, useRef } from "react";
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
import { useTranslation } from "react-i18next";
import CalendarIcon from "@/assets/icons/calendar.svg?react";
import { getSettings } from "@/services/queries/settings";
import { getDatePickerLocale, getDateSeparator, shouldForceLeadingZeros } from "@/utils/localeUtils";
import { useDialogPortalContainer } from "../BgtDialog/BgtDialog";
import { BgtFieldLabel } from "./BgtFieldLabel";
import { BgtFormErrors } from "./BgtFormErrors";

interface FieldBinding {
	field: AnyFieldApi;
	value?: never;
	onValueChange?: never;
	errors?: never;
}

interface ControlledBinding {
	field?: never;
	value: string | null;
	onValueChange: (value: string) => void;
	errors?: string[];
}

export interface BgtDatePickerBaseProps {
	label: string;
	disabled?: boolean;
	className?: string;
	/** Legacy placeholder prop. The segmented input shows locale-appropriate placeholders automatically. */
	placeholder?: string;
	clearable?: boolean;
	minValue?: string | null;
	maxValue?: string | null;
}

export type BgtDatePickerProps = (FieldBinding | ControlledBinding) & BgtDatePickerBaseProps;

const safeParseDateValue = (iso: string | undefined | null): CalendarDate | null => {
	if (!iso) return null;
	try {
		return parseDate(iso);
	} catch {
		return null;
	}
};

export const BgtDatePicker = (props: BgtDatePickerProps) => {
	const { label, disabled = false, className = "", clearable = false, minValue, maxValue } = props;
	const { t } = useTranslation();
	const { data: settings } = useQuery(getSettings());
	const portalContainer = useDialogPortalContainer();
	const calendarButtonRef = useRef<HTMLButtonElement>(null);

	const handleOpenChange = (isOpen: boolean) => {
		if (!isOpen && portalContainer) {
			requestAnimationFrame(() => calendarButtonRef.current?.focus());
		}
	};

	const locale = useMemo(
		() => getDatePickerLocale(settings?.dateFormat, settings?.uiLanguage),
		[settings?.dateFormat, settings?.uiLanguage],
	);
	const calendarLocale = settings?.uiLanguage || locale;
	const separator = getDateSeparator(settings?.dateFormat);
	const forceLeadingZeros = shouldForceLeadingZeros(settings?.dateFormat);

	const errors: string[] = props.field ? props.field.state.meta.errors : (props.errors ?? []);
	const hasErrors = errors.length > 0;
	const value = safeParseDateValue((props.field ? props.field.state.value : props.value) as string | undefined | null);

	const emit = (next: string) => {
		if (props.field) {
			props.field.handleChange(next);
		} else {
			props.onValueChange(next);
		}
	};

	const handleChange = (date: DateValue | null) => {
		emit(date ? date.toString() : "");
	};

	return (
		<div className="flex flex-col justify-start w-full">
			{label && <BgtFieldLabel>{label}</BgtFieldLabel>}
			<I18nProvider locale={locale}>
				<DatePicker
					value={value}
					onChange={handleChange}
					isDisabled={disabled}
					isInvalid={hasErrors}
					minValue={safeParseDateValue(minValue) ?? undefined}
					maxValue={safeParseDateValue(maxValue) ?? undefined}
					shouldForceLeadingZeros={forceLeadingZeros}
					shouldCloseOnSelect
					onOpenChange={handleOpenChange}
					aria-label={label || t("common:date")}
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
								>
									{segment.type === "literal" && separator ? separator : segment.text}
								</DateSegment>
							)}
						</DateInput>
						{clearable && value !== null && !disabled && (
							<button
								type="button"
								onClick={() => emit("")}
								className={cx(
									"flex-none inline-flex size-6 -mx-1.5 items-center justify-center rounded text-gray-400 hover:text-white transition-colors",
									"cursor-pointer bg-transparent border-none p-0 text-lg leading-none",
								)}
								aria-label={t("common:clear-date")}
							>
								×
							</button>
						)}
						<Button
							className={cx(
								"flex-none inline-flex size-6 -m-0.5 items-center justify-center rounded text-gray-400 hover:text-white transition-colors",
								"cursor-pointer bg-transparent border-none p-0",
								"disabled:cursor-not-allowed disabled:hover:text-gray-400",
							)}
							ref={calendarButtonRef}
							aria-label={t("common:open-calendar")}
						>
							<CalendarIcon className="size-5" />
						</Button>
					</Group>
					<I18nProvider locale={calendarLocale}>
						<Popover
							UNSTABLE_portalContainer={portalContainer}
							placement="bottom end"
							offset={5}
							className="bg-background border border-primary/30 rounded-lg p-4 shadow-lg z-[9999] pointer-events-auto"
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
											aria-label={t("common:previous-month")}
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
											aria-label={t("common:next-month")}
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
														"transition-colors",
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
					</I18nProvider>
				</DatePicker>
			</I18nProvider>
			{label && <BgtFormErrors errors={errors} />}
		</div>
	);
};
