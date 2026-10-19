import { describe, expect, it } from "vitest";
import { renderWithTheme, screen, within } from "@/test/test-utils";
import { allBadges, earnedBadges } from "../-hooks/achievementFixtures";
import { PlayerAchievementsCard } from "./PlayerAchievementsCard";
import { PlayerAchievementsSummary } from "./PlayerAchievementsSummary";

describe("PlayerAchievementsCard", () => {
	it("shows one milestone per badge type with its current level and the next goal", () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={earnedBadges} />);

		expect(screen.getByText("achievements.milestones")).toBeInTheDocument();
		expect(screen.getByText("badges:sessions.blue.title")).toBeInTheDocument();
		expect(screen.getByText("badges:wins.green.title")).toBeInTheDocument();
		expect(screen.getAllByText("achievements.next")).toHaveLength(2);
		expect(screen.getByRole("img", { name: "2/4" })).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "0/4" })).toBeInTheDocument();
	});

	it("says a milestone is complete once gold is earned", () => {
		const sessions = allBadges.filter((badge) => badge.type === "sessions");
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={sessions} />);

		expect(screen.getByText("achievements.complete")).toBeInTheDocument();
	});

	it("shows the feats with their descriptions on hover", () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={earnedBadges} />);

		expect(screen.getByText("achievements.feats")).toBeInTheDocument();
		expect(screen.getByText("firstTry.title").closest("li")).toHaveAttribute("title", "firstTry.description");
		expect(screen.getByText("marathonRunner.title")).toBeInTheDocument();
	});
});

describe("PlayerAchievementsSummary", () => {
	it("shows the earned count and the progress, and links to the full list instead of repeating the badges", () => {
		renderWithTheme(<PlayerAchievementsSummary badges={allBadges} playerBadges={earnedBadges} />);

		expect(screen.getByText("titles.achievements (2/4)")).toBeInTheDocument();
		const progress = screen.getByRole("progressbar", { name: "titles.achievements" });
		expect(progress).toHaveAttribute("value", "2");
		expect(progress).toHaveAttribute("max", "4");
		expect(within(progress.parentElement as HTMLElement).getByTestId("progress-fill")).toHaveStyle({ width: "50%" });
		expect(screen.queryAllByRole("img")).toHaveLength(0);
		expect(screen.getByRole("link", { name: "achievements.see-all" })).toHaveAttribute("href", "#player-achievements");
	});

	it("shows an empty progress bar when there are no badges", () => {
		renderWithTheme(<PlayerAchievementsSummary badges={[]} playerBadges={[]} />);

		const progress = screen.getByRole("progressbar", { name: "titles.achievements" });
		expect(progress).toHaveAttribute("value", "0");
		expect(progress).toHaveAttribute("max", "1");
		expect(within(progress.parentElement as HTMLElement).getByTestId("progress-fill")).toHaveStyle({ width: "0%" });
	});
});
