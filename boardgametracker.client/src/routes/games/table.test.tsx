import type { FC } from "react";
import { describe, expect, it, vi } from "vitest";
import { renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => ({}) };
	},
	useNavigate: () => vi.fn(),
	Link: ({ children }: { children: React.ReactNode }) => <a href="/">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("./-hooks/useGamesData", () => ({
	useGamesData: () => ({ games: [], isLoading: false }),
}));

vi.mock("./-hooks/useInlineGameUpdate", () => ({
	useInlineGameUpdate: () => ({ updateGame: vi.fn() }),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ currency: "€", uiLanguage: "en-US", dateFormat: "dd-MM-yyyy" }),
	}),
}));

await import("./table");

describe("Games table filters", () => {
	it("says what each filter filters while it is set to any", () => {
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		const filters = screen.getAllByRole("combobox");
		expect(filters.map((filter) => filter.textContent)).toEqual([
			"games:filters.state-any",
			"games:filters.language-any",
		]);
	});
});
