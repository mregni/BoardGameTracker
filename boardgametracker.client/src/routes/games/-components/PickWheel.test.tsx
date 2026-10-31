import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { Game } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { PickWheel } from "./PickWheel";

const navigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => navigate,
	Link: ({ children }: { children: React.ReactNode }) => <a href="/sessions/new/4">{children}</a>,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canWrite: true }),
}));

const game = (id: number, title: string) =>
	({ id, title, image: `/images/${id}.jpg`, minPlayers: 2, maxPlayers: 4, minPlayTime: 30, maxPlayTime: 45 }) as Game;

const Harness = ({ games, onExclude }: { games: Game[]; onExclude: (id: number) => void }) => {
	const [winnerId, setWinnerId] = useState<number>();
	return <PickWheel games={games} winnerId={winnerId} onResult={setWinnerId} onExclude={onExclude} />;
};

describe("PickWheel", () => {
	it("draws covers, lands on a game, focuses Open game and offers to exclude it", async () => {
		const onExclude = vi.fn();
		const { container } = renderWithTheme(
			<Harness games={[game(4, "Azul"), game(5, "Catan")]} onExclude={onExclude} />,
		);

		expect(container.querySelectorAll("image")).toHaveLength(2);

		await userEvent.click(screen.getByRole("button", { name: "pick.spin" }));
		const open = await screen.findByRole("button", { name: "pick.open" });

		expect(open).toHaveFocus();
		await userEvent.click(open);
		expect(navigate).toHaveBeenCalledWith({ to: "/games/$gameId", params: { gameId: expect.any(Number) } });

		await userEvent.click(screen.getByRole("button", { name: "pick.exclude" }));
		expect(onExclude).toHaveBeenCalledOnce();
	});

	it("shows the only candidate straight away without a spin", () => {
		renderWithTheme(<Harness games={[game(4, "Azul")]} onExclude={vi.fn()} />);

		expect(screen.getByRole("button", { name: "pick.open" })).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "pick.exclude" })).not.toBeInTheDocument();
	});
});
