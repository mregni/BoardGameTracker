import type { FC } from "react";
import { describe, expect, it, vi } from "vitest";
import TrendUp from "@/assets/icons/trend-up.svg?react";
import { menuItems } from "@/routes/-hooks/useMenuInfo";
import { render, renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => ({}) };
	},
	useNavigate: () => vi.fn(),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => new Promise(() => {}) }),
}));

vi.mock("./-hooks/useCompareData", () => ({
	useCompareData: () => ({ players: [], compare: undefined, settings: undefined, isLoading: true }),
}));

await import("./index");

describe("Compare page", () => {
	it("uses the sidebar's compare icon in the page header", () => {
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		const heading = screen.getByRole("heading", { name: "title" });
		const headerIcon = heading.parentElement?.querySelector("svg");
		const { container } = render(<TrendUp className="text-primary size-7" />);

		expect(menuItems.find((item) => item.path === "/compare")?.icon).toBe(TrendUp);
		expect(headerIcon?.outerHTML).toBe(container.querySelector("svg")?.outerHTML);
	});
});
