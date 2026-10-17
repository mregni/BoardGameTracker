import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import { compareResult, createCompareI18n, games, translationFor } from "../-test/compareTestSetup";
import { Highlights } from "./Highlights";

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: (namespace?: string | string[]) => translationFor(i18n.instance, namespace),
}));

vi.mock("@/routes/-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => games[id] ?? null }),
}));

describe("Highlights", () => {
	beforeAll(async () => {
		i18n.instance = await createCompareI18n("en-US");
	});

	it("turns the first and longest game together into chips", () => {
		render(<Highlights compare={compareResult()} dateFormat="dd-MM-yyyy" uiLanguage="en-US" />);

		const chips = screen.getAllByRole("listitem");
		expect(chips[0]).toHaveTextContent("Carcassonne");
		expect(chips[0]).toHaveTextContent("first session together · 12-03-2025");
		expect(chips[1]).toHaveTextContent("Brass: Birmingham");
		expect(chips[1]).toHaveTextContent("longest session · 2h 50m");
	});

	it("renders nothing without highlights", () => {
		const { container } = render(
			<Highlights
				compare={compareResult({ firstGameTogether: null, longestSessionTogether: null })}
				dateFormat="dd-MM-yyyy"
				uiLanguage="en-US"
			/>,
		);

		expect(container).toBeEmptyDOMElement();
	});
});
