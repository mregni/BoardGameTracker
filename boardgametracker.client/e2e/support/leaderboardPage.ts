import { expect, type Page } from "@playwright/test";
import { subDays } from "date-fns";
import { games } from "./seedData";
import { between, brusselsDay, literal, percent, playerId, type RankBy, type Standing, short } from "./standings";

const pinnedYearStart = brusselsDay(subDays(new Date(), 100));

export const pinnedYear = between(pinnedYearStart);

export const pinYearStart = (page: Page) =>
	page.route("**/api/leaderboard?*", async (route) => {
		const url = new URL(route.request().url());
		if (url.searchParams.get("from") === `${brusselsDay(new Date()).slice(0, 4)}-01-01`) {
			url.searchParams.set("from", pinnedYearStart);
		}
		await route.continue({ url: url.href });
	});

type Ranked = Standing["ranked"][number];
type Entry = Standing["unranked"][number];

const plural = (amount: number, word: string) => `${amount} ${word}${amount === 1 ? "" : "s"}`;

export const podiumOf = (standing: Standing) => {
	const top = standing.ranked.slice(0, 3);
	return top.length < 2 ? [] : top;
};

const metric = (entry: Entry, rankBy: RankBy) => {
	if (rankBy === "winRate") return `${percent(entry.winRate)} Win rate`;
	return rankBy === "sessions" ? `${entry.sessions} Sessions` : `${entry.wins} Wins`;
};

const podiumName = (entry: Ranked, rankBy: RankBy) => {
	const details = [
		rankBy === "wins" ? percent(entry.winRate) : plural(entry.wins, "win"),
		rankBy === "sessions" ? percent(entry.winRate) : plural(entry.sessions, "session"),
	];
	return `Place ${entry.rank}, ${entry.name}, ${metric(entry, rankBy)}, ${details.join(" · ")}`;
};

const rowName = (entry: Entry, rank: number | null, minimum: number, showPodiums: boolean) =>
	[
		rank === null ? "Not ranked" : `Place ${rank}`,
		entry.name,
		plural(entry.wins, "win"),
		plural(entry.sessions, "session"),
		`Win rate ${percent(entry.winRate)}`,
		...(showPodiums && rank !== null ? [plural(entry.podiums, "podium")] : []),
		...(rank === null ? [`${minimum - entry.sessions} more to be ranked`] : []),
		`Time ${short(entry.minutes)}`,
	].join(", ");

const rowText = (entry: Entry, rank: number | null, minimum: number, showPodiums: boolean) => {
	const columns = [
		entry.name,
		...(rank === null ? [`${minimum - entry.sessions} more to be ranked`] : []),
		entry.wins,
		entry.sessions,
		percent(entry.winRate),
		...(showPodiums ? [entry.podiums] : []),
		short(entry.minutes),
	].join(" ");
	return new RegExp(`^${rank ?? "– Not ranked"} (. )?${literal(columns)}$`.replaceAll(" ", "\\s+"));
};

const expectRow = async (
	page: Page,
	index: number,
	entry: Entry,
	rank: number | null,
	standing: Standing,
	showPodiums: boolean,
) => {
	const row = page.getByTestId("leaderboard-row").nth(index);

	await expect(row).toHaveAccessibleName(rowName(entry, rank, standing.minimum, showPodiums));
	await expect(row).toHaveAttribute("href", `/players/${playerId(entry.name)}`);
	await expect(row).toHaveText(rowText(entry, rank, standing.minimum, showPodiums), { useInnerText: true });
};

const expectRanking = async (page: Page, standing: Standing, gameTitle?: string) => {
	const podium = podiumOf(standing);
	const ranked = standing.ranked.filter((entry) => !podium.includes(entry));
	const shown = ranked.length + standing.unranked.length;
	const showPodiums = [...ranked, ...standing.unranked].some((entry) => entry.podiums > 0);
	const region = page.getByRole("region", {
		name: gameTitle ? `Ranking · ${gameTitle}` : "Ranking",
		exact: true,
	});
	const unrankedHeading = region.getByRole("heading", { level: 3 });

	await expect(page.getByTestId("leaderboard-row")).toHaveCount(shown);
	if (shown === 0) {
		await expect(region).toHaveCount(0);
		return;
	}
	if (standing.unranked.length > 0) {
		await expect(unrankedHeading).toHaveText(`Fewer than ${plural(standing.minimum, "session")} · not ranked`);
	} else {
		await expect(unrankedHeading).toHaveCount(0);
	}
	for (const [index, entry] of ranked.entries()) {
		await expectRow(page, index, entry, entry.rank, standing, showPodiums);
	}
	for (const [index, entry] of standing.unranked.entries()) {
		await expectRow(page, ranked.length + index, entry, null, standing, showPodiums);
	}
};

const expectPodium = async (page: Page, standing: Standing) => {
	const top = podiumOf(standing);
	const region = page.getByRole("region", { name: "Podium" });

	if (top.length === 0) {
		await expect(region).toHaveCount(0);
		return;
	}
	const links = region.getByRole("link");
	await expect(links).toHaveCount(top.length);
	for (const [index, entry] of top.entries()) {
		await expect(links.nth(index)).toHaveAccessibleName(podiumName(entry, standing.rankBy));
		await expect(links.nth(index)).toHaveAttribute("href", `/players/${playerId(entry.name)}`);
	}
};

const expectRecords = async (page: Page, standing: Standing) => {
	const podium = podiumOf(standing);
	const bestLabel = `Best win rate, min. ${standing.minimum}`;
	const all = [
		{
			repeats: "wins",
			title: "Most wins",
			text: "Most wins",
			entry: standing.mostWins,
			value: (x: Entry) => `${x.wins}`,
		},
		{
			repeats: "sessions",
			title: "Most sessions",
			text: "Most sessions",
			entry: standing.mostSessions,
			value: (x: Entry) => `${x.sessions}`,
		},
		{
			repeats: "winRate",
			title: bestLabel,
			text: `Best win rate · min. ${standing.minimum}`,
			entry: standing.bestWinRate,
			value: (x: Entry) => percent(x.winRate),
		},
		{
			repeats: null,
			title: "Most time",
			text: "Most time",
			entry: standing.mostTime,
			value: (x: Entry) => short(x.minutes),
		},
	];
	const shown = all.filter(
		(tile) =>
			tile.repeats !== standing.rankBy && !(tile.repeats && podium.some((entry) => entry.name === tile.entry?.name)),
	);
	const items = page.getByRole("region", { name: "Records" }).getByRole("listitem");

	await expect(items).toHaveCount(shown.length);
	for (const [index, tile] of shown.entries()) {
		const item = items.nth(index);
		if (tile.entry) {
			const name = `${tile.title}: ${tile.entry.name}, ${tile.value(tile.entry)}`;
			await expect(item.getByRole("link", { name, exact: true })).toHaveAttribute(
				"href",
				`/players/${playerId(tile.entry.name)}`,
			);
			await expect(item).toHaveText(
				new RegExp(`^${literal(`${tile.text}${tile.value(tile.entry)}`)}.?${literal(tile.entry.name)}$`),
			);
		} else {
			await expect(item).toHaveText(`${tile.text}-Nobody yet`);
			await expect(item.getByRole("link")).toHaveCount(0);
		}
	}
};

const expectRules = async (page: Page, standing: Standing) => {
	const showPodiums = standing.entries.some((entry) => entry.podiums > 0);
	const ties: Record<RankBy, string> = {
		wins: "Equal wins: the higher win rate goes first, then more sessions. Identical stats share the rank.",
		winRate: "Equal win rate: more wins go first, then more sessions. Identical stats share the rank.",
		sessions: "Equal sessions: more wins go first. Identical stats share the rank.",
	};
	const rules = [
		...(standing.coopWins > 0
			? [
					`Co-op wins and losses count: a team win is a win for everyone at the table, a team loss a loss (${standing.coopWins} of the ${standing.totalWins} wins).`,
				]
			: []),
		...(standing.ranked.length > 1 ? [ties[standing.rankBy]] : []),
	].join(" ");
	const details = page.getByRole("group").filter({ hasText: "How ranking works" });

	if (!rules && !showPodiums) {
		await expect(details).toHaveCount(0);
		return;
	}
	await details.getByText("How ranking works").click();
	await expect(details.getByRole("paragraph")).toHaveText([
		...(rules ? [rules] : []),
		...(showPodiums ? ["Top-3 finishes in scored games with 4 or more players"] : []),
	]);
	await details.getByText("How ranking works").click();
};

const expectNobodyRanked = async (page: Page, standing: Standing) => {
	const status = page.getByRole("main").getByRole("status");
	if (standing.ranked.length > 0) {
		await expect(status).toHaveCount(0);
		return;
	}
	await expect(status).toContainText(
		`Nobody has played ${plural(standing.minimum, "session")} yet, so nobody is ranked.`,
	);
};

export const expectLeaderboard = async (page: Page, standing: Standing, gameTitle?: string) => {
	await expect(page.getByRole("heading", { name: "Leaderboard", level: 1 })).toBeVisible();
	await expect(page.getByRole("region", { name: "Records" })).toBeVisible();
	await expectRanking(page, standing, gameTitle);
	await expectPodium(page, standing);
	await expectRecords(page, standing);
	await expectNobodyRanked(page, standing);
	await expectRules(page, standing);
};

export const expectFilters = async (
	page: Page,
	filters: { period: string; game: string; min: string; rank: string },
) => {
	await expect(page.getByRole("radio", { name: filters.period })).toBeChecked();
	await expect(page.getByRole("combobox", { name: "Game" })).toHaveText(filters.game);
	await expect(page.getByRole("combobox", { name: "Min. sessions" })).toHaveText(filters.min);
	await expect(page.getByRole("combobox", { name: "Rank by" })).toHaveText(filters.rank);
};

export const gameTitles = ["All games", ...games.map((game) => game.title).sort((a, b) => a.localeCompare(b))];
