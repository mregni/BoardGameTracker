import { describe, expect, it } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import {
	formatLoanDate,
	getLoanStats,
	getLoanStatus,
	getLoanStatusTone,
	getPresetDueDate,
	resolveDueDate,
	sortActiveLoans,
	sortReturnedLoans,
} from "./loanStatus";

const now = new Date(2026, 9, 11, 15, 30);

const loan = (overrides: Partial<Loan> = {}): Loan => ({
	id: 1,
	loanDate: new Date(2026, 8, 20),
	dueDate: null,
	returnedDate: null,
	gameId: 1,
	playerId: 1,
	isActive: true,
	...overrides,
});

describe("getLoanStatus", () => {
	it("marks a loan past its due date as overdue with the number of days", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 8) }), now);

		expect(status).toEqual({ kind: "overdue", days: 3, date: new Date(2026, 9, 8) });
		expect(getLoanStatusTone(status)).toBe("red");
	});

	it("marks a loan due today", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 11) }), now);

		expect(status).toEqual({ kind: "due-today" });
		expect(getLoanStatusTone(status)).toBe("amber");
	});

	it("marks a loan due within a week as due soon", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 15) }), now);

		expect(status).toEqual({ kind: "due-soon", days: 4, date: new Date(2026, 9, 15) });
		expect(getLoanStatusTone(status)).toBe("amber");
	});

	it("still counts a loan due in exactly seven days as due soon", () => {
		expect(getLoanStatus(loan({ dueDate: new Date(2026, 9, 18) }), now)).toEqual(
			expect.objectContaining({ kind: "due-soon", days: 7 }),
		);
	});

	it("shows the due date when it is more than a week away", () => {
		const dueDate = new Date(2026, 9, 25);
		const status = getLoanStatus(loan({ dueDate }), now);

		expect(status).toEqual({ kind: "due", date: dueDate });
		expect(getLoanStatusTone(status)).toBe("neutral");
	});

	it("handles a loan without a due date", () => {
		const status = getLoanStatus(loan(), now);

		expect(status).toEqual({ kind: "no-due-date" });
		expect(getLoanStatusTone(status)).toBe("neutral");
	});

	it("treats a returned loan as returned even when it was overdue", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 8, 25), returnedDate: new Date(2026, 9, 1) }), now);

		expect(status).toEqual({ kind: "returned" });
	});

	it("accepts the ISO strings the api returns", () => {
		const raw = loan({ dueDate: "2026-10-09T00:00:00" as unknown as Date });

		expect(getLoanStatus(raw, now)).toEqual(expect.objectContaining({ kind: "overdue", days: 2 }));
	});
});

describe("getLoanStats", () => {
	it("counts on loan, overdue, due this week and returned loans", () => {
		const loans = [
			loan({ id: 1, dueDate: new Date(2026, 9, 8) }),
			loan({ id: 2, dueDate: new Date(2026, 9, 11) }),
			loan({ id: 3, dueDate: new Date(2026, 9, 15) }),
			loan({ id: 4, dueDate: new Date(2026, 10, 30) }),
			loan({ id: 5 }),
			loan({ id: 6, dueDate: new Date(2026, 8, 25), returnedDate: new Date(2026, 9, 1) }),
			loan({ id: 7, returnedDate: new Date(2026, 9, 2) }),
		];

		expect(getLoanStats(loans, now)).toEqual({ onLoan: 5, overdue: 1, dueThisWeek: 2, returned: 2 });
	});

	it("returns zeros without loans", () => {
		expect(getLoanStats([], now)).toEqual({ onLoan: 0, overdue: 0, dueThisWeek: 0, returned: 0 });
	});
});

describe("sorting", () => {
	it("puts the earliest due date first and loans without a due date last", () => {
		const loans = [
			loan({ id: 1 }),
			loan({ id: 2, dueDate: new Date(2026, 9, 20) }),
			loan({ id: 3, dueDate: new Date(2026, 9, 5) }),
		];

		expect(sortActiveLoans(loans).map((x) => x.id)).toEqual([3, 2, 1]);
	});

	it("shows the most recently returned loan first", () => {
		const loans = [
			loan({ id: 1, returnedDate: new Date(2026, 9, 1) }),
			loan({ id: 2, returnedDate: new Date(2026, 9, 9) }),
		];

		expect(sortReturnedLoans(loans).map((x) => x.id)).toEqual([2, 1]);
	});
});

describe("due date presets", () => {
	it.each([
		["one-week", new Date(2026, 9, 18)],
		["two-weeks", new Date(2026, 9, 25)],
		["one-month", new Date(2026, 10, 11)],
	] as const)("calculates %s from the loan date", (preset, expected) => {
		expect(getPresetDueDate("2026-10-11", preset)).toEqual(expected);
	});

	it("has no date for the no due date and pick date presets", () => {
		expect(getPresetDueDate("2026-10-11", "none")).toBeNull();
		expect(getPresetDueDate("2026-10-11", "custom")).toBeNull();
	});

	it("has no date without a valid loan date", () => {
		expect(getPresetDueDate("", "one-week")).toBeNull();
	});

	it("clamps one month to the end of a shorter month", () => {
		expect(getPresetDueDate("2026-01-31", "one-month")).toEqual(new Date(2026, 1, 28));
	});

	it("resolves the due date the form submits", () => {
		expect(resolveDueDate("2026-10-11", "two-weeks", "")).toBe("2026-10-25");
		expect(resolveDueDate("2026-10-11", "none", "2026-12-01")).toBe("");
		expect(resolveDueDate("2026-10-11", "custom", "2026-12-01")).toBe("2026-12-01");
	});
});

describe("formatLoanDate", () => {
	it("uses the date format from the settings", () => {
		expect(formatLoanDate(new Date(2026, 8, 14), "dd-MM-yyyy", "en-us")).toBe("14-09-2026");
		expect(formatLoanDate(new Date(2026, 8, 14), "yyyy/MM/dd", "en-us")).toBe("2026/09/14");
	});

	it("accepts the setting in its upper case form", () => {
		expect(formatLoanDate(new Date(2026, 8, 14), "DD-MM-YYYY", "en-us")).toBe("14-09-2026");
	});

	it("formats the iso strings the api sends", () => {
		expect(formatLoanDate("2026-09-14T10:00:00", "dd-MM-yyyy", "en-us")).toBe("14-09-2026");
	});

	it("falls back to the default format without a setting", () => {
		expect(formatLoanDate(new Date(2026, 8, 14), undefined, "en-us")).toBe("14-09-2026");
	});

	it("returns an empty string without a date", () => {
		expect(formatLoanDate(null, "dd-MM-yyyy", "en-us")).toBe("");
	});
});
