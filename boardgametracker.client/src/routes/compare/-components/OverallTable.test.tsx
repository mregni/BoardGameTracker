import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@/test/test-utils";
import { compareResult, createCompareI18n, playerOne, playerTwo, translationFor } from "../-test/compareTestSetup";
import { OverallTable } from "./OverallTable";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

describe("OverallTable", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("always shows the table under its section heading", () => {
		render(<OverallTable compare={compareResult()} playerOne={playerOne} playerTwo={playerTwo} />);

		expect(screen.getByRole("region", { name: "All sessions, not just together" })).toBeInTheDocument();
		expect(screen.getAllByTestId("overall-row")).toHaveLength(4);
	});

	it("puts both players' overall stats in one three-column table and marks the higher value", () => {
		render(<OverallTable compare={compareResult()} playerOne={playerOne} playerTwo={playerTwo} />);

		const rows = screen.getAllByTestId("overall-row");
		const cells = rows.map((row) =>
			within(row)
				.getAllByRole("cell")
				.map((cell) => cell.textContent),
		);
		expect(rows.map((row) => within(row).getByRole("rowheader").textContent)).toEqual([
			"Wins",
			"Win rate",
			"Sessions",
			"Play time",
		]);
		expect(cells).toEqual([
			["54higher", "48"],
			["38%", "41%higher"],
			["142higher", "118"],
			["131hhigher", "104h"],
		]);
		expect(screen.getByRole("columnheader", { name: "Mikhaël" })).toBeInTheDocument();
	});

	it("marks nobody on a tie", () => {
		render(
			<OverallTable
				compare={compareResult({ winCount: { playerOne: 3, playerTwo: 3 } })}
				playerOne={playerOne}
				playerTwo={playerTwo}
			/>,
		);

		const [wins] = screen.getAllByTestId("overall-row");
		expect(within(wins).queryByText("higher")).toBeNull();
	});
});
