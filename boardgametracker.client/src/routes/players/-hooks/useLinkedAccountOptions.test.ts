import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";

const calls = vi.hoisted(() => ({
	users: vi.fn(),
	players: vi.fn(),
}));

vi.mock("@/services/queries/auth", () => ({
	getUsers: () => ({ queryKey: ["users"], queryFn: calls.users }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: calls.players }),
}));

import { useLinkedAccountOptions } from "./useLinkedAccountOptions";

const render = () => {
	const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } });
	const wrapper = ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
	return renderHook(() => useLinkedAccountOptions(), { wrapper });
};

const signIn = (roles: string[], authEnabled = true) => {
	useAuth.getState().setSession("access", { id: "u1", username: "me", displayName: null, roles, profileImage: null });
	useAuth.setState({ authStatus: { authEnabled } as never });
};

describe("useLinkedAccountOptions", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		calls.users.mockResolvedValue([{ id: "u2", username: "jane" }]);
		calls.players.mockResolvedValue([{ id: 4, name: "Jane" }]);
	});

	it("loads accounts and players for admins", async () => {
		signIn(["Admin"]);

		const { result } = render();

		await waitFor(() => expect(result.current.users).toHaveLength(1));
		expect(result.current.canManage).toBe(true);
		expect(result.current.players).toHaveLength(1);
	});

	it("loads nothing for members", () => {
		signIn(["User"]);

		const { result } = render();

		expect(result.current.canManage).toBe(false);
		expect(result.current.authEnabled).toBe(true);
		expect(calls.users).not.toHaveBeenCalled();
		expect(calls.players).not.toHaveBeenCalled();
	});

	it("loads nothing when sign-in is off", () => {
		signIn(["Admin"], false);

		const { result } = render();

		expect(result.current.canManage).toBe(false);
		expect(calls.users).not.toHaveBeenCalled();
	});
});
