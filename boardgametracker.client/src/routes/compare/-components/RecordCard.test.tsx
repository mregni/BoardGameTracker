import type { i18n as I18n } from "i18next";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@/test/test-utils";
import {
	COMPARE_LOCALES,
	compareResult,
	createCompareI18n,
	games,
	playerOne,
	playerTwo,
	sessionRow,
	translationFor,
} from "../-test/compareTestSetup";
import { RecordCard } from "./RecordCard";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

describe("RecordCard", () => {
	beforeEach(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("shows the record as the hero with a crown for the leader", () => {
		render(<RecordCard compare={compareResult()} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(screen.getByTestId("record-playerOne")).toHaveTextContent("23");
		expect(screen.getByTestId("record-playerTwo")).toHaveTextContent("17");
		expect(screen.getByText("wins for Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("wins for Kathleen")).toBeInTheDocument();
		expect(screen.getByText("46 games together")).toBeInTheDocument();
		expect(
			within(screen.getByTestId("record-playerOne")).getByRole("img", { name: "Mikhaël leads" }),
		).toBeInTheDocument();
		expect(within(screen.getByTestId("record-playerTwo")).queryByRole("img")).toBeNull();
	});

	it("gives nobody the crown on a tie and says when a player has no wins yet", () => {
		render(
			<RecordCard
				compare={compareResult({
					directWins: { playerOne: 0, playerTwo: 0 },
					wonByOthers: 1,
					totalSessionsTogether: 1,
				})}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(screen.queryByRole("img", { name: /leads/ })).toBeNull();
		expect(screen.getByText("no wins yet for Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("1 game together")).toBeInTheDocument();
	});

	it("labels the split bar with every part of the record and shows the legend", () => {
		render(<RecordCard compare={compareResult()} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(
			screen.getByRole("img", {
				name: "46 games together: Mikhaël won 23, someone else won 6, Kathleen won 17",
			}),
		).toBeInTheDocument();
		const segments = screen.getAllByTestId("progress-segment");
		expect(segments.map((segment) => segment.dataset.tone)).toEqual(["playerOne", "muted", "playerTwo"]);
		const legend = screen.getAllByRole("listitem").map((item) => item.textContent);
		expect(legend).toEqual(["Mikhaël won23", "someone else won6", "Kathleen won17"]);
	});

	it("counts a game both won for both players without breaking the bar", () => {
		render(
			<RecordCard
				compare={compareResult({
					directWins: { playerOne: 2, playerTwo: 2 },
					wonByOthers: 0,
					totalSessionsTogether: 3,
				})}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		const bar = screen.getByRole("img", { name: /3 games together/ });
		expect(bar).not.toHaveAttribute("data-overflow");
		expect(screen.queryByText("someone else won")).toBeNull();
	});

	it("shows the last games and the streak", () => {
		render(<RecordCard compare={compareResult()} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(screen.getByText("Kathleen 2 in a row")).toBeInTheDocument();
		expect(screen.getAllByTestId("form-tile")).toHaveLength(2);
	});

	it.each(COMPARE_LOCALES)("uses the right plural forms in %s", async (lang) => {
		i18n.instance = await createCompareI18n(lang);
		const t = i18n.instance.getFixedT(null, "compare");

		render(
			<RecordCard
				compare={compareResult({
					directWins: { playerOne: 1, playerTwo: 0 },
					wonByOthers: 0,
					totalSessionsTogether: 1,
					recentSessions: [sessionRow({ sessionId: 1, outcome: "playerOne" })],
					streak: null,
				})}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		expect(screen.getByText(t("record.wins_one", { player: "Mikhaël" }))).toBeInTheDocument();
		expect(screen.getByText(t("record.wins_zero", { player: "Kathleen" }))).toBeInTheDocument();
		expect(screen.getByText(t("record.together_one", { count: 1 }))).toBeInTheDocument();
		expect(screen.getByText(t("form.title_one"), { exact: false })).toBeInTheDocument();
		expect(t("record.wins", { count: 5, player: "X" })).toBe(t("record.wins_other", { player: "X" }));
	});
});
