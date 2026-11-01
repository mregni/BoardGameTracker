import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { renderWithTheme, screen } from "@/test/test-utils";
import { allBadges, earnedBadges } from "../-hooks/achievementFixtures";
import { PlayerAchievementsCard } from "./PlayerAchievementsCard";

describe("PlayerAchievementsCard", () => {
	it("shows earned milestones with their level and the next goal", () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={earnedBadges} />);

		expect(screen.getByText("achievements.milestones")).toBeInTheDocument();
		expect(screen.getByText("badges:sessions.blue.title")).toBeInTheDocument();
		expect(screen.getByText("2/4")).toBeInTheDocument();
		expect(screen.getByText("achievements.levels")).toHaveClass("sr-only");
		expect(screen.getByText("achievements.next")).toHaveClass("line-clamp-2");
		expect(screen.queryByText("badges:wins.green.title")).not.toBeInTheDocument();
	});

	it("reveals the locked achievements on request", async () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={earnedBadges} />);

		await userEvent.click(screen.getByRole("button", { name: "achievements.show-locked" }));

		expect(screen.getByText("badges:wins.green.title")).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "marathonRunner.title, player:achievements.locked" })).toHaveClass(
			"grayscale",
		);
		expect(screen.getByRole("button", { name: "achievements.hide-locked" })).toHaveAttribute("aria-expanded", "true");
	});

	it("shows everything when nothing is earned yet", () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={[]} />);

		expect(screen.getByText("badges:wins.green.title")).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "achievements.show-locked" })).not.toBeInTheDocument();
	});

	it("says a milestone is complete once gold is earned", () => {
		const sessions = allBadges.filter((badge) => badge.type === "sessions");
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={sessions} />);

		expect(screen.getByText("achievements.complete")).toBeInTheDocument();
	});

	it("names the earned feats", () => {
		renderWithTheme(<PlayerAchievementsCard badges={allBadges} playerBadges={earnedBadges} />);

		expect(screen.getByText("achievements.feats")).toBeInTheDocument();
		expect(screen.getByRole("img", { name: "firstTry.title" })).toBeInTheDocument();
		expect(
			screen.queryByRole("img", { name: "marathonRunner.title, player:achievements.locked" }),
		).not.toBeInTheDocument();
	});
});
