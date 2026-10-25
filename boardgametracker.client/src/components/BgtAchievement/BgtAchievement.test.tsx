import { describe, expect, it } from "vitest";
import { type Badge, BadgeLevel, BadgeType } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { BgtAchievementIcon } from "./BgtAchievement";

describe("BgtAchievementIcon", () => {
	const createBadge = (overrides: Partial<Badge> = {}): Badge => ({
		id: 1,
		titleKey: "first-win",
		descriptionKey: "first-win-desc",
		type: BadgeType.wins,
		level: BadgeLevel.gold,
		image: "first-win.png",
		...overrides,
	});

	describe("Rendering", () => {
		it("should render badge image", () => {
			renderWithTheme(<BgtAchievementIcon badge={createBadge()} />);
			const image = screen.getByRole("img");
			expect(image).toHaveAttribute("src", "/images/badges/first-win.png");
		});

		it("names the badge when it gets keyboard focus", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtAchievementIcon badge={createBadge()} />);

			await user.tab();

			expect(screen.getByRole("button", { name: "first-win" })).toHaveFocus();
			expect(await screen.findByRole("tooltip")).toHaveTextContent("first-win");
		});

		it("opens and closes the name with a tap", async () => {
			const user = userEvent.setup();
			renderWithTheme(<BgtAchievementIcon badge={createBadge()} />);
			const trigger = screen.getByRole("button", { name: "first-win" });

			await user.click(trigger);
			expect(await screen.findByRole("tooltip")).toHaveTextContent("first-win-desc");

			await user.click(trigger);
			expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
		});
	});
});
