import { addMonths, addWeeks, differenceInCalendarDays, format, isValid, parseISO } from "date-fns";
import type { Loan } from "@/models/Loan/Loan";
import { DEFAULT_DATE_FORMAT, toDisplay } from "@/utils/dateUtils";
import { parseLocalDate } from "@/utils/localDate";

export const DUE_SOON_DAYS = 7;

export type LoanStatus =
	| { kind: "returned" }
	| { kind: "overdue"; days: number; date: Date }
	| { kind: "due-today" }
	| { kind: "due-soon"; days: number; date: Date }
	| { kind: "due"; date: Date }
	| { kind: "no-due-date" };

export type LoanStatusTone = "red" | "amber" | "neutral";

export interface LoanStats {
	onLoan: number;
	overdue: number;
	dueThisWeek: number;
	returned: number;
}

export type DuePreset = "one-week" | "two-weeks" | "one-month" | "none" | "custom";

export const DUE_PRESETS: DuePreset[] = ["one-week", "two-weeks", "one-month", "none", "custom"];

export const toLoanDate = (value: Date | string | null | undefined): Date | null => {
	if (!value) {
		return null;
	}

	const date = typeof value === "string" ? parseISO(value) : value;
	return isValid(date) ? date : null;
};

export const getLoanStatus = (loan: Loan, now: Date = new Date()): LoanStatus => {
	if (toLoanDate(loan.returnedDate)) {
		return { kind: "returned" };
	}

	const due = toLoanDate(loan.dueDate);
	if (!due) {
		return { kind: "no-due-date" };
	}

	const days = differenceInCalendarDays(due, now);
	if (days < 0) {
		return { kind: "overdue", days: -days, date: due };
	}
	if (days === 0) {
		return { kind: "due-today" };
	}
	if (days <= DUE_SOON_DAYS) {
		return { kind: "due-soon", days, date: due };
	}
	return { kind: "due", date: due };
};

export const getLoanStatusTone = (status: LoanStatus): LoanStatusTone => {
	switch (status.kind) {
		case "overdue":
			return "red";
		case "due-today":
		case "due-soon":
			return "amber";
		default:
			return "neutral";
	}
};

export const isActiveLoan = (loan: Loan): boolean => toLoanDate(loan.returnedDate) === null;

export const getLoanStats = (loans: Loan[], now: Date = new Date()): LoanStats => {
	const stats: LoanStats = { onLoan: 0, overdue: 0, dueThisWeek: 0, returned: 0 };

	for (const loan of loans) {
		const status = getLoanStatus(loan, now);
		if (status.kind === "returned") {
			stats.returned += 1;
			continue;
		}

		stats.onLoan += 1;
		if (status.kind === "overdue") {
			stats.overdue += 1;
		}
		if (status.kind === "due-today" || status.kind === "due-soon") {
			stats.dueThisWeek += 1;
		}
	}

	return stats;
};

const timeOf = (value: Date | string | null | undefined, fallback: number): number =>
	toLoanDate(value)?.getTime() ?? fallback;

export const sortActiveLoans = (loans: Loan[]): Loan[] =>
	[...loans].sort(
		(a, b) =>
			timeOf(a.dueDate, Number.POSITIVE_INFINITY) - timeOf(b.dueDate, Number.POSITIVE_INFINITY) ||
			timeOf(a.loanDate, 0) - timeOf(b.loanDate, 0),
	);

export const sortReturnedLoans = (loans: Loan[]): Loan[] =>
	[...loans].sort((a, b) => timeOf(b.returnedDate, 0) - timeOf(a.returnedDate, 0));

export const getPresetDueDate = (loanDate: string, preset: DuePreset): Date | null => {
	const start = parseLocalDate(loanDate);
	if (!start) {
		return null;
	}

	switch (preset) {
		case "one-week":
			return addWeeks(start, 1);
		case "two-weeks":
			return addWeeks(start, 2);
		case "one-month":
			return addMonths(start, 1);
		default:
			return null;
	}
};

export const resolveDueDate = (loanDate: string, preset: DuePreset, customDate: string): string => {
	if (preset === "custom") {
		return customDate;
	}

	const date = getPresetDueDate(loanDate, preset);
	return date ? format(date, "yyyy-MM-dd") : "";
};

export const formatLoanDate = (
	value: Date | string | null | undefined,
	dateFormat: string | null | undefined,
	language: string,
): string => toDisplay(toLoanDate(value), dateFormat || DEFAULT_DATE_FORMAT, language);
