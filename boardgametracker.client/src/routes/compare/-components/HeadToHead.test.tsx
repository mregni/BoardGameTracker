import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { i18n as I18n } from "i18next";
import { beforeAll, describe, expect, it, vi } from "vitest";
import type { CompareResult, Player } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
import { HeadToHead } from "./HeadToHead";

const locales = ["base", "en-US", "nl-BE", "nl-NL", "es-ES"] as const;
const readCompare = (lang: string) =>
	JSON.parse(readFileSync(resolve(process.cwd(), "public/locales", lang, "compare.json"), "utf8"));

const i18n = vi.hoisted(() => ({ instance: null as I18n | null }));

vi.mock("react-i18next", () => ({
	useTranslation: () => ({ t: i18n.instance!.t, i18n: i18n.instance }),
}));

vi.mock("../../-hooks/useGameById", () => ({
	useGameById: () => ({ gameById: (id: number) => ({ 5: { id: 5, title: "Carcassonne" } })[id] ?? null }),
}));

const createI18n = async (lang: string) => {
	const { createInstance } = await vi.importActual<typeof import("i18next")>("i18next");
	const instance = createInstance();
	await instance.init({
		lng: lang === "base" ? "en-US" : lang,
		ns: ["compare"],
		defaultNS: "compare",
		resources: { [lang === "base" ? "en-US" : lang]: { compare: readCompare(lang) } },
		interpolation: { escapeValue: false },
	});
	return instance;
};

const playerOne = { id: 1, name: "Kathleen" } as Player;
const playerTwo = { id: 2, name: "Mikhaël" } as Player;

const compare = (count: number) =>
	({
		directWins: { playerOne: count, playerTwo: count },
		mostWonGame: { playerOne: { gameId: 5, count }, playerTwo: null },
		lastWonGame: null,
		preferredGame: { gameId: 5, sessionCount: count },
		closestGame: { playerId: 1, gameId: 5, scoringDifference: count },
		firstGameTogether: null,
	}) as unknown as CompareResult;

const renderHeadToHead = (count: number) =>
	renderWithTheme(
		<HeadToHead
			playerOne={playerOne}
			playerTwo={playerTwo}
			compare={compare(count)}
			dateFormat="dd-MM-yyyy"
			uiLanguage="en-US"
		/>,
	);

describe("HeadToHead plurals", () => {
	beforeAll(async () => {
		i18n.instance = await createI18n("en-US");
	});

	it("uses the singular for a count of one", () => {
		renderHeadToHead(1);

		expect(screen.getByText("Kathleen won by just 1 point in Carcassonne")).toBeInTheDocument();
		expect(screen.getByText("Most played together: Carcassonne (1 session)")).toBeInTheDocument();
		expect(screen.getByText("Kathleen won 1 time against Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("Kathleen won most from Mikhaël in Carcassonne (1 time)")).toBeInTheDocument();
	});

	it("uses the plural for other counts", () => {
		renderHeadToHead(3);

		expect(screen.getByText("Kathleen won by just 3 points in Carcassonne")).toBeInTheDocument();
		expect(screen.getByText("Most played together: Carcassonne (3 sessions)")).toBeInTheDocument();
		expect(screen.getByText("Kathleen won 3 times against Mikhaël")).toBeInTheDocument();
		expect(screen.getByText("Kathleen won most from Mikhaël in Carcassonne (3 times)")).toBeInTheDocument();
	});
});

describe("compare locale plurals", () => {
	const expectations: Record<(typeof locales)[number], { one: string; other: string }> = {
		base: { one: "1 point", other: "2 points" },
		"en-US": { one: "1 point", other: "2 points" },
		"nl-BE": { one: "1 punt ", other: "2 punten" },
		"nl-NL": { one: "1 punt ", other: "2 punten" },
		"es-ES": { one: "1 punto ", other: "2 puntos" },
	};

	it.each(locales)("pluralises the closest game in %s", async (lang) => {
		const instance = await createI18n(lang);
		const args = { player: "A", game: "B" };

		expect(instance.t("head-to-head.closest-game", { ...args, count: 1 })).toContain(expectations[lang].one);
		expect(instance.t("head-to-head.closest-game", { ...args, count: 2 })).toContain(expectations[lang].other);
	});

	it.each(locales)("has a one and other form for every head-to-head count string in %s", (lang) => {
		const headToHead: Record<string, string> = readCompare(lang)["head-to-head"];
		const counted = Object.entries(headToHead).filter(([, value]) => value.includes("{{count}}"));

		expect(counted.length).toBeGreaterThan(0);
		for (const [key] of counted) {
			const base = key.replace(/_(one|other)$/, "");
			expect(headToHead).toHaveProperty(`${base}_one`);
			expect(headToHead).toHaveProperty(`${base}_other`);
		}
		expect(Object.values(headToHead).some((value) => value.includes("{{points}}"))).toBe(false);
	});
});
