import {
	createMemoryHistory,
	createRootRoute,
	createRoute,
	createRouter,
	Link,
	Outlet,
	RouterProvider,
} from "@tanstack/react-router";
import { afterEach, describe, expect, it } from "vitest";
import { act, render, screen, userEvent } from "@/test/test-utils";
import { trackHistory } from "@/utils/historyTrail";
import { useBack, useBackTo } from "./useBack";

const BackButton = ({ parent }: { parent: string }) => {
	const back = useBack(parent);
	return (
		<button type="button" onClick={back}>
			back
		</button>
	);
};

const SaveButton = ({ parent }: { parent: string }) => {
	const backTo = useBackTo();
	return (
		<button type="button" onClick={() => backTo(parent)}>
			save
		</button>
	);
};

const rootRoute = createRootRoute({ component: () => <Outlet /> });
const page = (path: string, component: () => React.ReactNode) =>
	createRoute({ getParentRoute: () => rootRoute, path, component });

const routes = [
	page("/", () => <Link to="/games">games</Link>),
	page("/games", () => (
		<Link to="/games/$gameId" params={{ gameId: 3 }}>
			open game
		</Link>
	)),
	page("/games/$gameId", () => (
		<>
			<BackButton parent="/games" />
			<Link to="/games/$gameId/sessions" params={{ gameId: 3 }}>
				open sessions
			</Link>
			<Link to="/games/$gameId/update" params={{ gameId: 3 }}>
				edit game
			</Link>
		</>
	)),
	page("/games/$gameId/sessions", () => <BackButton parent="/games/3" />),
	page("/games/$gameId/update", () => <BackButton parent="/games/3" />),
	page("/players", () => <span>players</span>),
	page("/players/$playerId", () => <BackButton parent="/players" />),
	page("/game-nights", () => (
		<>
			<Link to="/players/$playerId" params={{ playerId: 1 }}>
				open player
			</Link>
			<Link to="/sessions/update/$sessionId" params={{ sessionId: 8 }}>
				edit session
			</Link>
		</>
	)),
	page("/sessions/update/$sessionId", () => <SaveButton parent="/games/3/sessions" />),
];

let untrack: (() => void) | undefined;

afterEach(() => {
	untrack?.();
});

const setup = async (initialEntries: string[]) => {
	const history = createMemoryHistory({ initialEntries, initialIndex: initialEntries.length - 1 });
	untrack = trackHistory(history);
	const router = createRouter({ routeTree: rootRoute.addChildren(routes), history });
	await act(() => router.load());
	render(<RouterProvider router={router} />);
	await screen.findAllByText(/back|save|open|games|players/);
	const user = userEvent.setup();
	const click = async (name: string) => {
		await user.click(
			await screen.findByRole(name === "back" || name === "save" ? "button" : "link", { name: new RegExp(name) }),
		);
	};
	return {
		at: () => router.state.location.pathname,
		index: () => history.location.state.__TSR_index,
		length: () => history.length,
		click,
	};
};

describe("useBack", () => {
	it("returns to game nights after opening a player from there", async () => {
		const { at, index, click } = await setup(["/game-nights"]);

		await click("open player");
		await click("back");

		expect(at()).toBe("/game-nights");
		expect(index()).toBe(0);
	});

	it("goes from sessions to the game and on to the games list without looping", async () => {
		const { at, index, click } = await setup(["/games"]);

		await click("open game");
		await click("open sessions");
		await click("back");
		expect(at()).toBe("/games/3");

		await click("back");
		expect(at()).toBe("/games");
		expect(index()).toBe(0);
	});

	it("replaces a directly opened page with its parent", async () => {
		const { at, index, length, click } = await setup(["/games/3/sessions"]);

		await click("back");
		expect(at()).toBe("/games/3");

		await click("back");
		expect(at()).toBe("/games");
		expect(index()).toBe(0);
		expect(length()).toBe(1);
	});

	it("uses the parent after a reload left the trail empty", async () => {
		const { at, index, click } = await setup(["/", "/games/3/update"]);

		await click("back");

		expect(at()).toBe("/games/3");
		expect(index()).toBe(1);
	});

	it("returns from the edit game page to the game it was opened from", async () => {
		const { at, index, click } = await setup(["/games/3"]);

		await click("edit game");
		await click("back");

		expect(at()).toBe("/games/3");
		expect(index()).toBe(0);
	});

	it("returns to game nights after saving a session opened from there", async () => {
		const { at, index, click } = await setup(["/game-nights"]);

		await click("edit session");
		await click("save");

		expect(at()).toBe("/game-nights");
		expect(index()).toBe(0);
	});

	it("opens the game sessions after saving a session opened directly", async () => {
		const { at, click } = await setup(["/sessions/update/8"]);

		await click("save");

		expect(at()).toBe("/games/3/sessions");
	});
});
