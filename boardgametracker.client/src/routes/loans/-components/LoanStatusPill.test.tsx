import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";
import { render, screen } from "@/test/test-utils";
import { LoanStatusPill } from "./LoanStatusPill";

vi.mock("react-i18next", async () => {
	const loans = (await import("../../../../public/locales/en-US/loans.json")).default as Record<string, unknown>;
	const lookup = (key: string): string =>
		key.split(".").reduce<unknown>((node, part) => (node as Record<string, unknown>)?.[part], loans) as string;

	return {
		useTranslation: () => ({
			t: (key: string, options?: Record<string, string>) =>
				Object.entries(options ?? {}).reduce(
					(text, [name, value]) => text.replace(`{{${name}}}`, value),
					lookup(key) ?? key,
				),
			i18n: { language: "en-us" },
		}),
	};
});

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

const renderPill = (value: Loan) => render(<LoanStatusPill loan={value} dateFormat="dd-MM-yyyy" />);

describe("LoanStatusPill", () => {
	beforeEach(() => {
		vi.useFakeTimers({ toFake: ["Date"] });
		vi.setSystemTime(new Date(2026, 9, 11, 12, 0));
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it.each([
		["an overdue loan", loan({ dueDate: new Date(2026, 9, 8) }), "3 days overdue", "text-warn"],
		["a loan that is one day overdue", loan({ dueDate: new Date(2026, 9, 10) }), "1 day overdue", "text-warn"],
		["a loan overdue for over a month", loan({ dueDate: new Date(2026, 8, 1) }), "40 days overdue", "text-bad"],
		["a loan due today", loan({ dueDate: new Date(2026, 9, 11) }), "Due today", "text-stat-title"],
		["a loan due soon", loan({ dueDate: new Date(2026, 9, 15) }), "Due in 4 days", "text-stat-title"],
		["a loan due later", loan({ dueDate: new Date(2026, 9, 25) }), "Due 25-10-2026", "text-white/60"],
		["a loan without a due date", loan(), "No due date", "text-white/60"],
		["a returned loan", loan({ returnedDate: new Date(2026, 8, 14) }), "Returned 14-09-2026", "text-white/60"],
	])("labels %s", (_, value, text, tone) => {
		renderPill(value);

		const pill = screen.getByText(text);
		expect(pill).toHaveAttribute("title", text);
		expect(pill).toHaveClass(tone);
	});
});
