import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import {
	formatDayCount,
	formatLoanDate,
	getLoanStats,
	getLoanStatus,
	getLoanStatusTone,
	getPresetDueDate,
	groupLoansByBorrower,
	matchesLoanFilter,
	partitionLoans,
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

		expect(status).toEqual({ kind: "overdue", days: 3 });
		expect(getLoanStatusTone(status)).toBe("amber");
	});

	it("turns red once a loan is more than a month overdue", () => {
		expect(getLoanStatusTone({ kind: "overdue", days: 30 })).toBe("amber");
		expect(getLoanStatusTone({ kind: "overdue", days: 31 })).toBe("red");
	});

	it("marks a loan due today", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 11) }), now);

		expect(status).toEqual({ kind: "due-today" });
		expect(getLoanStatusTone(status)).toBe("soon");
	});

	it("marks a loan due within a week as due soon", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 15) }), now);

		expect(status).toEqual({ kind: "due-soon", days: 4 });
		expect(getLoanStatusTone(status)).toBe("soon");
	});

	it("still counts a loan due in exactly seven days as due soon", () => {
		expect(getLoanStatus(loan({ dueDate: new Date(2026, 9, 18) }), now)).toEqual({ kind: "due-soon", days: 7 });
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

		expect(status).toEqual({ kind: "returned", date: new Date(2026, 9, 1) });
	});

	it("accepts the ISO strings the api returns", () => {
		const raw = loan({ dueDate: "2026-10-09T00:00:00" as unknown as Date });

		expect(getLoanStatus(raw, now)).toEqual({ kind: "overdue", days: 2 });
	});
});

describe("getLoanStats", () => {
	it("counts overdue loans and loans due this week", () => {
		const loans = [
			loan({ id: 1, dueDate: new Date(2026, 9, 8) }),
			loan({ id: 6, dueDate: new Date(2026, 7, 1) }),
			loan({ id: 2, dueDate: new Date(2026, 9, 11) }),
			loan({ id: 3, dueDate: new Date(2026, 9, 15) }),
			loan({ id: 4, dueDate: new Date(2026, 10, 30) }),
			loan({ id: 5 }),
		];

		expect(getLoanStats(loans, now)).toEqual({ overdue: 2, longOverdue: 1, dueThisWeek: 2 });
	});

	it("returns zeros without loans", () => {
		expect(getLoanStats([], now)).toEqual({ overdue: 0, longOverdue: 0, dueThisWeek: 0 });
	});
});

describe("matchesLoanFilter", () => {
	const overdue = loan({ dueDate: new Date(2026, 9, 8) });
	const today = loan({ dueDate: new Date(2026, 9, 11) });
	const later = loan({ dueDate: new Date(2026, 10, 30) });
	const open = loan();

	it("keeps every loan without a filter", () => {
		expect([overdue, today, later, open].every((x) => matchesLoanFilter(x, "all", now))).toBe(true);
	});

	it("keeps only overdue loans for the overdue filter", () => {
		expect([overdue, today, later, open].map((x) => matchesLoanFilter(x, "overdue", now))).toEqual([
			true,
			false,
			false,
			false,
		]);
	});

	it("keeps loans due today or within the week for the due this week filter", () => {
		const soon = loan({ dueDate: new Date(2026, 9, 15) });
		expect([overdue, today, soon, later, open].map((x) => matchesLoanFilter(x, "due-this-week", now))).toEqual([
			false,
			true,
			true,
			false,
			false,
		]);
	});
});

describe("groupLoansByBorrower", () => {
	it("groups loans per borrower in the order the borrowers first appear", () => {
		const loans = [loan({ id: 1, playerId: 5 }), loan({ id: 2, playerId: 3 }), loan({ id: 3, playerId: 5 })];

		expect(groupLoansByBorrower(loans)).toEqual([
			{ playerId: 5, loans: [loans[0], loans[2]] },
			{ playerId: 3, loans: [loans[1]] },
		]);
	});

	it("returns no groups without loans", () => {
		expect(groupLoansByBorrower([])).toEqual([]);
	});
});

describe("partitionLoans", () => {
	it("splits active and returned loans", () => {
		const { active, returned } = partitionLoans([
			loan({ id: 1 }),
			loan({ id: 2, returnedDate: new Date(2026, 9, 1) }),
			loan({ id: 3, dueDate: new Date(2026, 9, 20) }),
		]);

		expect(active.map((x) => x.id)).toEqual([3, 1]);
		expect(returned.map((x) => x.id)).toEqual([2]);
	});

	it("puts the earliest due date first and loans without a due date last", () => {
		const { active } = partitionLoans([
			loan({ id: 1 }),
			loan({ id: 2, dueDate: new Date(2026, 9, 20) }),
			loan({ id: 3, dueDate: new Date(2026, 9, 5) }),
		]);

		expect(active.map((x) => x.id)).toEqual([3, 2, 1]);
	});

	it("shows the most recently returned loan first", () => {
		const { returned } = partitionLoans([
			loan({ id: 1, returnedDate: new Date(2026, 9, 1) }),
			loan({ id: 2, returnedDate: new Date(2026, 9, 9) }),
		]);

		expect(returned.map((x) => x.id)).toEqual([2, 1]);
	});
});

describe("due date presets", () => {
	it.each([
		["one-week", "2026-10-18"],
		["two-weeks", "2026-10-25"],
		["one-month", "2026-11-11"],
	] as const)("calculates %s from the loan date", (preset, expected) => {
		expect(getPresetDueDate("2026-10-11", preset)).toBe(expected);
	});

	it("has no date for the no due date and pick date presets", () => {
		expect(getPresetDueDate("2026-10-11", "none")).toBe("");
		expect(getPresetDueDate("2026-10-11", "custom")).toBe("");
	});

	it("has no date without a valid loan date", () => {
		expect(getPresetDueDate("", "one-week")).toBe("");
	});

	it("clamps one month to the end of a shorter month", () => {
		expect(getPresetDueDate("2026-01-31", "one-month")).toBe("2026-02-28");
	});
});

describe("formatDayCount", () => {
	beforeEach(() => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(now);
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it.each([
		["en-us", 1, "1 day"],
		["en-us", 3, "3 days"],
		["nl-be", 1, "1 dag"],
		["nl-be", 3, "3 dagen"],
		["es-es", 1, "1 día"],
		["es-es", 3, "3 días"],
	])("words %s for %i days", (language, days, expected) => {
		expect(formatDayCount(days, language)).toBe(expected);
	});

	it("words the days of an overdue loan without a sign", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 8) }));

		expect(status.kind === "overdue" && formatDayCount(status.days, "en-us")).toBe("3 days");
		expect(formatDayCount(-3, "nl-be")).toBe("3 dagen");
	});

	it("words the days until a loan is due", () => {
		const status = getLoanStatus(loan({ dueDate: new Date(2026, 9, 12) }));

		expect(status.kind === "due-soon" && formatDayCount(status.days, "es-es")).toBe("1 día");
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
