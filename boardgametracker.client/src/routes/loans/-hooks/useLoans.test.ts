import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Loan } from "@/models/Loan/Loan";

const { successToast, errorToast, invalidateLoan } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
	invalidateLoan: vi.fn(() => Promise.resolve()),
}));

const loan: Loan = {
	id: 7,
	loanDate: new Date("2026-09-01"),
	dueDate: null,
	returnedDate: null,
	gameId: 3,
	playerId: 2,
	isActive: true,
};

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/hooks/useQueryInvalidator", () => ({
	useQueryInvalidator: () => ({ invalidateLoan }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/queries/loans", () => ({
	getLoans: () => ({ queryKey: ["loans"], queryFn: () => Promise.resolve([loan]) }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ currency: "EUR" }) }),
}));

vi.mock("@/services/loanService", () => ({
	deleteLoanCall: vi.fn(),
	returnLoanCall: vi.fn(),
	updateLoanCall: vi.fn(),
}));

import { deleteLoanCall, returnLoanCall, updateLoanCall } from "@/services/loanService";
import { useLoans } from "./useLoans";

const createWrapper = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } });
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseLoans = async () => {
	const rendered = renderHook(() => useLoans(), { wrapper: createWrapper() });
	await waitFor(() => expect(rendered.result.current.isLoading).toBe(false));
	return rendered;
};

describe("useLoans", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("exposes the loans and settings", async () => {
		const { result } = await renderUseLoans();

		expect(result.current.loans).toEqual([loan]);
		expect(result.current.settings).toEqual({ currency: "EUR" });
	});

	it("deletes a loan and refreshes the loan and its game", async () => {
		vi.mocked(deleteLoanCall).mockResolvedValue(undefined);
		const { result } = await renderUseLoans();

		await act(() => result.current.deleteLoan(7));

		expect(deleteLoanCall).toHaveBeenCalledWith(7);
		expect(invalidateLoan).toHaveBeenCalledWith(7, 3);
		expect(successToast).toHaveBeenCalledWith("loans:delete.successfull");
	});

	it("returns a loan on the given date", async () => {
		vi.mocked(returnLoanCall).mockResolvedValue(loan);
		const { result } = await renderUseLoans();
		const returned = new Date("2026-09-20");

		await act(() => result.current.returnLoan(7, returned));

		expect(returnLoanCall).toHaveBeenCalledWith(7, returned);
		expect(invalidateLoan).toHaveBeenCalledWith(7, 3);
		expect(successToast).toHaveBeenCalledWith("loans:return.successfull");
	});

	it("updates a loan", async () => {
		vi.mocked(updateLoanCall).mockResolvedValue(loan);
		const { result } = await renderUseLoans();

		await act(() => result.current.updateLoan(loan));

		expect(updateLoanCall).toHaveBeenCalledWith(loan);
		expect(invalidateLoan).toHaveBeenCalledWith(7, 3);
		expect(successToast).toHaveBeenCalledWith("loans:notifications.updated");
	});

	it("shows the failure message for each action and refreshes nothing", async () => {
		vi.mocked(deleteLoanCall).mockRejectedValue(new Error("boom"));
		vi.mocked(returnLoanCall).mockRejectedValue(new Error("boom"));
		vi.mocked(updateLoanCall).mockRejectedValue(new Error("boom"));
		const { result } = await renderUseLoans();

		await act(() => result.current.deleteLoan(7));
		await act(() => result.current.returnLoan(7, new Date()));
		await act(() => result.current.updateLoan(loan));

		expect(errorToast.mock.calls).toEqual([
			["loans:delete.failed"],
			["loans:return.failed"],
			["loans:notifications.update-failed"],
		]);
		expect(invalidateLoan).not.toHaveBeenCalled();
		expect(successToast).not.toHaveBeenCalled();
	});
});
