import { describe, expect, it } from "vitest";
import { type Badge, BadgeLevel, BadgeType } from "@/models";
import { renderWithTheme, screen } from "@/test/test-utils";
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
	});
});
