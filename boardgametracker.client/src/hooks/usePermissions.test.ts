import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { usePermissions } from "./usePermissions";

const mockHasRole = vi.fn<(role: string) => boolean>();
let mockAuthStatus: { authEnabled: boolean } | null = null;

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { hasRole: typeof mockHasRole; authStatus: typeof mockAuthStatus }) => unknown) =>
		selector({ hasRole: mockHasRole, authStatus: mockAuthStatus }),
}));

describe("usePermissions", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockAuthStatus = { authEnabled: true };
		mockHasRole.mockReturnValue(false);
	});

	describe("when auth is disabled", () => {
		beforeEach(() => {
			mockAuthStatus = { authEnabled: false };
		});

		it("should grant admin regardless of role", () => {
			const { result } = renderHook(() => usePermissions());

			expect(result.current.isAdmin).toBe(true);
			expect(result.current.canWrite).toBe(true);
			expect(result.current.canManageSettings).toBe(true);
		});

		it("should grant admin even when hasRole returns false", () => {
			mockHasRole.mockReturnValue(false);
			const { result } = renderHook(() => usePermissions());

			expect(result.current.isAdmin).toBe(true);
			expect(result.current.canWrite).toBe(true);
			expect(result.current.canManageSettings).toBe(true);
		});
	});

	describe("when auth is enabled", () => {
		describe("with Admin role", () => {
			beforeEach(() => {
				mockHasRole.mockImplementation((role) => role === "Admin");
			});

			it("should grant all permissions", () => {
				const { result } = renderHook(() => usePermissions());

				expect(result.current.isAdmin).toBe(true);
				expect(result.current.canWrite).toBe(true);
				expect(result.current.canManageSettings).toBe(true);
			});
		});

		describe("with User role", () => {
			beforeEach(() => {
				mockHasRole.mockImplementation((role) => role === "User");
			});

			it("should grant canWrite but not admin permissions", () => {
				const { result } = renderHook(() => usePermissions());

				expect(result.current.isAdmin).toBe(false);
				expect(result.current.canWrite).toBe(true);
				expect(result.current.canManageSettings).toBe(false);
			});
		});

		describe("with no roles", () => {
			it("should deny all permissions", () => {
				const { result } = renderHook(() => usePermissions());

				expect(result.current.isAdmin).toBe(false);
				expect(result.current.canWrite).toBe(false);
				expect(result.current.canManageSettings).toBe(false);
			});
		});

		describe("with both Admin and User roles", () => {
			beforeEach(() => {
				mockHasRole.mockReturnValue(true);
			});

			it("should grant all permissions", () => {
				const { result } = renderHook(() => usePermissions());

				expect(result.current.isAdmin).toBe(true);
				expect(result.current.canWrite).toBe(true);
				expect(result.current.canManageSettings).toBe(true);
			});
		});
	});

	describe("when authStatus is null", () => {
		beforeEach(() => {
			mockAuthStatus = null;
		});

		it("should treat null authStatus as auth disabled", () => {
			const { result } = renderHook(() => usePermissions());

			expect(result.current.isAdmin).toBe(true);
			expect(result.current.canWrite).toBe(true);
			expect(result.current.canManageSettings).toBe(true);
		});
	});
});
