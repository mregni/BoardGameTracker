import { addMonths, addWeeks, differenceInCalendarDays, formatDuration } from "date-fns";
import type { Loan } from "@/models/Loan/Loan";
import { DEFAULT_DATE_FORMAT, safeParseDate, toDisplay, toInputDate } from "@/utils/dateUtils";
import { parseLocalDate } from "@/utils/localDate";
import { getDateFnsLocale } from "@/utils/localeUtils";

export const DUE_SOON_DAYS = 7;

export type LoanStatus =
	| { kind: "returned"; date: Date }
	| { kind: "overdue"; days: number }
	| { kind: "due-today" }
	| { kind: "due-soon"; days: number }
	| { kind: "due"; date: Date }
	| { kind: "no-due-date" };

export type LoanStatusTone = "red" | "amber" | "neutral";

export const LOAN_STATUS_TONE: Record<LoanStatus["kind"], LoanStatusTone> = {
	overdue: "red",
	"due-today": "amber",
	"due-soon": "amber",
	due: "neutral",
	"no-due-date": "neutral",
	returned: "neutral",
};

export type DuePreset = "one-week" | "two-weeks" | "one-month" | "none" | "custom";

export const DUE_PRESETS: DuePreset[] = ["one-week", "two-weeks", "one-month", "none", "custom"];

type DateValue = Date | string | null | undefined;

const toDate = (value: DateValue) => safeParseDate(value ?? undefined);

export const getLoanStatus = (loan: Loan, now: Date = new Date()): LoanStatus => {
	const returned = toDate(loan.returnedDate);
	if (returned) {
		return { kind: "returned", date: returned };
	}

	const due = toDate(loan.dueDate);
	if (!due) {
		return { kind: "no-due-date" };
	}

	const days = differenceInCalendarDays(due, now);
	if (days < 0) {
		return { kind: "overdue", days: -days };
	}
	if (days === 0) {
		return { kind: "due-today" };
	}
	if (days <= DUE_SOON_DAYS) {
		return { kind: "due-soon", days };
	}
	return { kind: "due", date: due };
};

export const getLoanStats = (activeLoans: Loan[], now: Date = new Date()) => {
	const kinds = activeLoans.map((loan) => getLoanStatus(loan, now).kind);
	return {
		overdue: kinds.filter((kind) => kind === "overdue").length,
		dueThisWeek: kinds.filter((kind) => kind === "due-today" || kind === "due-soon").length,
	};
};

const timeOf = (value: DateValue, fallback: number): number => toDate(value)?.getTime() ?? fallback;

export const partitionLoans = (loans: Loan[]) => {
	const active = loans.filter((loan) => !loan.returnedDate);
	const returned = loans.filter((loan) => !!loan.returnedDate);

	return {
		active: active.sort(
			(a, b) =>
				timeOf(a.dueDate, Number.POSITIVE_INFINITY) - timeOf(b.dueDate, Number.POSITIVE_INFINITY) ||
				timeOf(a.loanDate, 0) - timeOf(b.loanDate, 0),
		),
		returned: returned.sort((a, b) => timeOf(b.returnedDate, 0) - timeOf(a.returnedDate, 0)),
	};
};

export const getPresetDueDate = (loanDate: string, preset: DuePreset): string => {
	const start = parseLocalDate(loanDate);
	if (!start) {
		return "";
	}

	switch (preset) {
		case "one-week":
			return toInputDate(addWeeks(start, 1), false);
		case "two-weeks":
			return toInputDate(addWeeks(start, 2), false);
		case "one-month":
			return toInputDate(addMonths(start, 1), false);
		default:
			return "";
	}
};

export const formatDayCount = (days: number, language: string): string =>
	formatDuration({ days: Math.abs(days) }, { format: ["days"], locale: getDateFnsLocale(language) });

export const formatLoanDate = (value: DateValue, dateFormat: string | null | undefined, language: string): string =>
	toDisplay(value, dateFormat || DEFAULT_DATE_FORMAT, language);
