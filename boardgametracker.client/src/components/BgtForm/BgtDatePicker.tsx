import * as Popover from "@radix-ui/react-popover";
import type { AnyFieldApi } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { isValid as dateFnsIsValid, parse } from "date-fns";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DayPicker } from "react-day-picker";
import CalendarIcon from "@/assets/icons/calendar.svg?react";
import { getSettings } from "@/services/queries/settings";
import { safeParseDate, toDisplay, toInputDate } from "@/utils/dateUtils";
import { getDateFnsLocale } from "@/utils/localeUtils";
import { BgtFormErrors } from "./BgtFormErrors";

import "react-day-picker/dist/style.css";

const START_MONTH = new Date(2000, 0);

const convertDateFormat = (userFormat: string): string => {
	return userFormat.replace(/YYYY/g, "yyyy").replace(/DD/g, "dd");
};

export interface BgtDatePickerProps {
	field: AnyFieldApi;
	label: string;
	disabled?: boolean;
	className?: string;
	placeholder: string;
}

export const BgtDatePicker = (props: BgtDatePickerProps) => {
	const { field, label, disabled = false, className = "", placeholder } = props;
	const [open, setOpen] = useState(false);
	const [inputValue, setInputValue] = useState("");

	const { data: settings } = useQuery(getSettings());

	const locale = useMemo(() => {
		const languageCode = settings?.uiLanguage ?? "en-us";
		return getDateFnsLocale(languageCode);
	}, [settings?.uiLanguage]);

	const hasErrors = field.state.meta.errors.length > 0;
	const selectedDate = field.state.value ? safeParseDate(field.state.value) : undefined;

	const endMonth = useMemo(() => new Date(), []);

	useEffect(() => {
		if (selectedDate && settings?.dateFormat && settings?.uiLanguage) {
			setInputValue(toDisplay(selectedDate, settings.dateFormat, settings.uiLanguage));
		} else {
			setInputValue("");
		}
	}, [selectedDate, settings?.dateFormat, settings?.uiLanguage]);

	const handleSelect = useCallback(
		(date: Date | undefined) => {
			if (date) {
				field.handleChange(toInputDate(date, false));
				setOpen(false);
			}
		},
		[field],
	);

	const handleInputBlur = useCallback(() => {
		if (!inputValue.trim()) {
			return;
		}

		if (!settings?.dateFormat) {
			return;
		}

		const dateFnsFormat = convertDateFormat(settings.dateFormat);
		const parsed = parse(inputValue, dateFnsFormat, new Date());

		if (dateFnsIsValid(parsed)) {
			field.handleChange(toInputDate(parsed, false));
		} else if (selectedDate && settings.uiLanguage) {
			setInputValue(toDisplay(selectedDate, settings.dateFormat, settings.uiLanguage));
		}
	}, [inputValue, settings?.dateFormat, settings?.uiLanguage, field, selectedDate]);

	const handleInputKeyDown = useCallback(
		(e: React.KeyboardEvent<HTMLInputElement>) => {
			if (e.key === "Enter") {
				e.preventDefault();
				handleInputBlur();
			}
		},
		[handleInputBlur],
	);

	return (
		<div className="flex flex-col justify-start w-full">
			<div className="flex items-baseline justify-between">
				<div className="text-[15px] font-medium leading-[35px] uppercase">{label}</div>
				<BgtFormErrors errors={field.state.meta.errors} />
			</div>
			<Popover.Root open={open} onOpenChange={setOpen}>
				<div
					className={cx(
						"w-full bg-background text-white px-4 py-3 rounded-lg border border-primary/30 focus-within:border-primary text-left flex items-center justify-between gap-2",
						className,
						hasErrors && "border border-error bg-error/5!",
						disabled && "opacity-50 cursor-not-allowed",
					)}
				>
					<input
						type="text"
						disabled={disabled}
						value={inputValue}
						onChange={(e) => setInputValue(e.target.value)}
						onBlur={handleInputBlur}
						onKeyDown={handleInputKeyDown}
						placeholder={placeholder}
						className="flex-1 bg-transparent text-white placeholder:text-gray-400 outline-none border-none p-0 text-base"
					/>
					<Popover.Trigger asChild>
						<button
							type="button"
							disabled={disabled}
							className="flex-none text-gray-400 hover:text-white transition-colors cursor-pointer bg-transparent border-none p-0"
						>
							<CalendarIcon className="size-5" />
						</button>
					</Popover.Trigger>
				</div>
				<Popover.Portal>
					<Popover.Content
						className="bg-background border border-primary/30 rounded-lg p-4 shadow-lg z-50"
						side="bottom"
						align="end"
						sideOffset={5}
					>
						<DayPicker
							mode="single"
							selected={selectedDate}
							defaultMonth={selectedDate}
							onSelect={handleSelect}
							locale={locale}
							captionLayout="dropdown"
							startMonth={START_MONTH}
							endMonth={endMonth}
							classNames={{
								root: "text-white w-60",
								month_caption: "flex justify-center items-center px-0 mb-4 relative",
								caption_label: "hidden",
								nav: "hidden",
								dropdowns: "flex gap-2 items-center justify-center w-full [color-scheme:dark]",
								dropdown_root: "relative inline-flex",
								dropdown:
									"bg-primary/20 text-white text-sm font-medium uppercase rounded-md pl-3 pr-8 py-1.5 border-none cursor-pointer outline-none hover:bg-primary/30 transition-colors appearance-none [&>option]:bg-background [&>option]:text-white [&>option]:normal-case",
								chevron: "size-4 fill-white",
								month_grid: "w-full",
								weekdays: "grid grid-cols-7 mb-2",
								weekday: "text-white/50 uppercase text-xs text-center py-2 font-normal",
								week: "grid grid-cols-7 gap-1 mt-1",
								day: "text-white flex items-center justify-center",
								day_button:
									"w-9 h-9 rounded-md transition-all border-none bg-transparent text-white cursor-pointer text-sm flex items-center justify-center hover:bg-primary/20",
								selected: "bg-primary text-white hover:bg-primary rounded-md",
								today: "bg-[#22d3ee]/20 text-[#22d3ee] rounded-md",
								outside: "text-white/20 opacity-50",
							}}
						/>
					</Popover.Content>
				</Popover.Portal>
			</Popover.Root>
		</div>
	);
};
