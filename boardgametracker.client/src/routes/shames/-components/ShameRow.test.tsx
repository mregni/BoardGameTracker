import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Shame, ShameGameNight } from "@/models";
import { render, screen, userEvent } from "@/test/test-utils";
import { ShameRow } from "./ShameRow";

vi.mock("@tanstack/react-router", () => ({
	Link: ({
		to,
		params,
		children,
		className,
		"aria-label": ariaLabel,
	}: {
		to: string;
		params: Record<string, string | number>;
		children: ReactNode;
		className?: string;
		"aria-label"?: string;
	}) => (
		<a
			aria-label={ariaLabel}
			className={className}
			href={Object.entries(params).reduce((path, [name, value]) => path.replace(`$${name}`, String(value)), to)}
		>
			{children}
		</a>
	),
}));

const now = new Date(2026, 9, 16);

const night: ShameGameNight = { id: 4, title: "Friday-ish Strategy Night", startDate: new Date(2026, 9, 23, 19, 0) };

const root: Shame = {
	id: 7,
	title: "Root",
	image: null,
	additionDate: new Date(2023, 7, 14),
	price: null,
	lastSessionDate: null,
	playCount: 0,
	minPlayers: 2,
	maxPlayers: 4,
	minPlayTime: 90,
	maxPlayTime: 90,
	plannedGameNightId: null,
	plannedFor: null,
};

const renderRow = (props: Partial<Parameters<typeof ShameRow>[0]> = {}) => {
	const onPlan = vi.fn();
	render(
		<ul>
			<ShameRow
				shame={root}
				currency="€"
				canWrite
				nextNight={night}
				planning={false}
				onPlan={onPlan}
				now={now}
				{...props}
			/>
		</ul>,
	);
	return { onPlan };
};

describe("ShameRow", () => {
	it("links the title to the game and describes a never played game", () => {
		renderRow();

		expect(screen.getByRole("link", { name: /Root/ })).toHaveAttribute("href", "/games/7");
		expect(screen.getByText("row.owned-since · row.players · row.minutes")).toBeInTheDocument();
		expect(screen.getByText("row.never-played")).toHaveClass("text-bad");
	});

	it("shows a dash instead of a lone currency when there is no price", () => {
		renderRow();

		expect(screen.getByText("–")).toBeInTheDocument();
		expect(screen.getByText("row.no-price")).toHaveClass("sr-only");
		expect(screen.queryByText("€")).not.toBeInTheDocument();
	});

	it("shows the price and the play count for a game played before", () => {
		renderRow({
			shame: { ...root, price: 45, playCount: 2, lastSessionDate: new Date(2025, 2, 1) },
		});

		expect(screen.getByText("€45")).toBeInTheDocument();
		expect(screen.getByText("row.owned-since · row.plays")).toBeInTheDocument();
		expect(screen.getByText("row.months-ago")).toHaveClass("text-warn");
	});

	it("uses a grey pill when the last play is less than a year ago", () => {
		renderRow({ shame: { ...root, playCount: 6, lastSessionDate: new Date(2025, 10, 1) } });

		expect(screen.getByText("row.months-ago")).toHaveClass("text-white/60");
	});

	it("plans the game for the next night", async () => {
		const user = userEvent.setup();
		const { onPlan } = renderRow();

		await user.click(screen.getByRole("button", { name: "row.plan-label" }));

		expect(onPlan).toHaveBeenCalledWith(root, night);
	});

	it("disables Plan while the game is being planned", () => {
		renderRow({ planning: true });

		expect(screen.getByRole("button", { name: "row.plan-label" })).toBeDisabled();
	});

	it("opens Log session with the game filled in", () => {
		renderRow();

		expect(screen.getByRole("link", { name: "row.log-play-label" })).toHaveAttribute("href", "/sessions/new/7");
	});

	it("hides Plan when no game night is coming up but keeps Log a play", () => {
		renderRow({ nextNight: null });

		expect(screen.queryByRole("button", { name: "row.plan-label" })).not.toBeInTheDocument();
		expect(screen.getByRole("link", { name: "row.log-play-label" })).toBeInTheDocument();
	});

	it("shows a green planned pill instead of the actions", () => {
		renderRow({ shame: { ...root, plannedGameNightId: 4, plannedFor: night.startDate } });

		expect(screen.getByText("row.planned")).toHaveClass("text-ok");
		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(screen.queryByRole("link", { name: "row.log-play-label" })).not.toBeInTheDocument();
		expect(screen.getByRole("listitem")).toHaveClass("border-ok/45");
	});

	it("shows no actions to readers", () => {
		renderRow({ canWrite: false });

		expect(screen.queryByRole("button")).not.toBeInTheDocument();
		expect(screen.queryByRole("link", { name: "row.log-play-label" })).not.toBeInTheDocument();
	});

	it("leaves out unknown details", () => {
		renderRow({
			shame: { ...root, additionDate: null, minPlayers: null, maxPlayers: null, minPlayTime: null, maxPlayTime: null },
		});

		expect(screen.getByRole("listitem").querySelector("small")).toBeNull();
	});
});
