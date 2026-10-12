import { describe, expect, it, vi } from "vitest";
import type { Game, GameNightManuals } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";

vi.mock("@/services/manualService", () => ({
	manualInviteDownloadUrl: (linkId: string, manualId: number) => `/manuals/${linkId}/${manualId}`,
}));

import { RsvpGames } from "./RsvpGames";

const games = [{ id: 9, title: "Carcassonne", image: null }] as unknown as Game[];

const manuals = [
	{
		gameId: 9,
		gameTitle: "Carcassonne",
		manuals: [
			{ id: 1, title: "Rules", fileSizeBytes: 2048 },
			{ id: 2, title: "Expansion rules", fileSizeBytes: 1024 },
		],
	},
] as unknown as GameNightManuals[];

describe("RsvpGames", () => {
	it("renders nothing without games or manuals", () => {
		const { container } = renderWithTheme(<RsvpGames linkId="abc" games={[]} manuals={[]} />);

		expect(container).toBeEmptyDOMElement();
	});

	it("shows the suggested games as chips without manuals", () => {
		renderWithTheme(<RsvpGames linkId="abc" games={games} manuals={[]} />);

		expect(screen.getByText("suggested-games")).toBeInTheDocument();
		expect(screen.getByText("Carcassonne")).toBeInTheDocument();
		expect(screen.queryByText("manuals")).not.toBeInTheDocument();
	});

	it("keeps the manuals collapsed until opened", async () => {
		const user = userEvent.setup();
		renderWithTheme(<RsvpGames linkId="abc" games={[]} manuals={manuals} />);

		const details = screen.getByText("manuals").closest("details") as HTMLDetailsElement;
		expect(details.open).toBe(false);

		await user.click(screen.getByText("manuals"));

		expect(details.open).toBe(true);
		expect(screen.getByRole("link", { name: /Expansion rules/ })).toHaveAttribute("href", "/manuals/abc/2");
	});
});
