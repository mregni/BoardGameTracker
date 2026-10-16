import type { i18n as I18n } from "i18next";
import type { ReactNode } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import {
	compareResult,
	createCompareI18n,
	games,
	playerOne,
	playerTwo,
	translationFor,
} from "../-test/compareTestSetup";
import { TogetherTiles } from "./TogetherTiles";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@tanstack/react-router", () => ({
	Link: ({ to, children, className }: { to: string; children: ReactNode; className?: string }) => (
		<a href={to} className={className}>
			{children}
		</a>
	),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

const renderTiles = (overrides = {}) =>
	render(
		<TogetherTiles
			compare={compareResult(overrides)}
			playerOne={playerOne}
			playerTwo={playerTwo}
			dateFormat="dd-MM-yyyy"
			uiLanguage="en-US"
		/>,
	);

describe("TogetherTiles", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("shows games, time, the most played game and the closest game", () => {
		renderTiles();

		expect(screen.getByText("46")).toBeInTheDocument();
		expect(screen.getByText("since 12-03-2025")).toBeInTheDocument();
		expect(screen.getByText("40h 10m")).toBeInTheDocument();
		expect(screen.getByText("avg 52 min a game")).toBeInTheDocument();
		expect(screen.getByRole("link", { name: /Most played/ })).toHaveAttribute("href", "/games/10");
		expect(screen.getByText("11 games")).toBeInTheDocument();
		expect(screen.getByText("1 pt")).toBeInTheDocument();
		expect(screen.getByText("Brass: Birmingham · Mikhaël won")).toBeInTheDocument();
	});

	it("names the closest game's winner on either side or someone else", () => {
		renderTiles({ closestGame: { gameId: 10, playerId: 2, scoringDifference: 2.5 } });
		expect(screen.getByText("2.5 pts")).toBeInTheDocument();
		expect(screen.getByText("Azul · Kathleen won")).toBeInTheDocument();
	});

	it("says someone else won the closest game when neither player did", () => {
		renderTiles({ closestGame: { gameId: 10, playerId: null, scoringDifference: 0 } });
		expect(screen.getByText("Azul · someone else won")).toBeInTheDocument();
	});

	it("leaves out the tiles it has no data for", () => {
		renderTiles({ preferredGame: null, closestGame: null, firstGameTogether: null });

		expect(screen.queryByText("Most played")).toBeNull();
		expect(screen.queryByText("Closest game")).toBeNull();
		expect(screen.queryByText(/since/)).toBeNull();
	});
});
