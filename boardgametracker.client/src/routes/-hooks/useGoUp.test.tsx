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
import { useGoUp } from "./useGoUp";

const UpButton = ({ parent, isParent }: { parent: string; isParent?: (pathname: string) => boolean }) => {
	const goUp = useGoUp(parent, isParent);
	return (
		<button type="button" onClick={goUp}>
			up
		</button>
	);
};

const rootRoute = createRootRoute({ component: () => <Outlet /> });
const routes = [
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/",
		component: () => (
			<Link to="/games/$gameId" params={{ gameId: 3 }}>
				open game
			</Link>
		),
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/games",
		component: () => (
			<Link to="/games/$gameId" params={{ gameId: 3 }}>
				open game
			</Link>
		),
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/games/$gameId",
		component: () => (
			<>
				<UpButton parent="/games" />
				<Link to="/games/$gameId/sessions" params={{ gameId: 3 }}>
					open sessions
				</Link>
				<Link to="/games/$gameId/update" params={{ gameId: 3 }}>
					edit game
				</Link>
			</>
		),
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/games/$gameId/sessions",
		component: () => <UpButton parent="/games/3" />,
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/games/$gameId/update",
		component: () => <UpButton parent="/games/3" />,
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/game-nights",
		component: () => (
			<Link to="/sessions/update/$sessionId" params={{ sessionId: 8 }}>
				edit session
			</Link>
		),
	}),
	createRoute({
		getParentRoute: () => rootRoute,
		path: "/sessions/update/$sessionId",
		component: () => (
			<UpButton
				parent="/games/3/sessions"
				isParent={(pathname) => pathname === "/games/3/sessions" || pathname === "/game-nights"}
			/>
		),
	}),
];

let untrack: (() => void) | undefined;

afterEach(() => {
	untrack?.();
});

const setup = async (initialEntry: string) => {
	const history = createMemoryHistory({ initialEntries: [initialEntry] });
	untrack = trackHistory(history);
	const router = createRouter({ routeTree: rootRoute.addChildren(routes), history });
	await act(() => router.load());
	render(<RouterProvider router={router} />);
	await screen.findByText(/up|open game|edit session/);
	const user = userEvent.setup();
	const at = () => router.state.location.pathname;
	const index = () => history.location.state.__TSR_index;
	const click = async (name: string) => {
		await user.click(await screen.findByRole(name === "up" ? "button" : "link", { name }));
	};
	return { at, index, click, history };
};

describe("useGoUp", () => {
	it("goes up from sessions to the game and from the game to the games list without looping", async () => {
		const { at, index, click } = await setup("/games");

		await click("open game");
		await click("open sessions");
		expect(at()).toBe("/games/3/sessions");

		await click("up");
		expect(at()).toBe("/games/3");
		expect(index()).toBe(1);

		await click("up");
		expect(at()).toBe("/games");
		expect(index()).toBe(0);
	});

	it("opens the parent instead of the previous page when that page is not the parent", async () => {
		const { at, index, click, history } = await setup("/");

		await click("open game");
		await click("up");

		expect(at()).toBe("/games");
		expect(index()).toBe(1);
		await act(async () => history.back());
		expect(at()).toBe("/");
	});

	it("walks up to the parents with replace when a page is opened directly", async () => {
		const { at, index, click } = await setup("/games/3/sessions");

		await click("up");
		expect(at()).toBe("/games/3");

		await click("up");
		expect(at()).toBe("/games");
		expect(index()).toBe(0);
	});

	it("returns from the edit game page to the game", async () => {
		const { at, index, click } = await setup("/games/3");

		await click("edit game");
		await click("up");

		expect(at()).toBe("/games/3");
		expect(index()).toBe(0);
	});

	it("returns from the edit game page to the game when opened directly", async () => {
		const { at, click } = await setup("/games/3/update");

		await click("up");

		expect(at()).toBe("/games/3");
	});

	it("returns from the edit session page to the game night it came from", async () => {
		const { at, index, click } = await setup("/game-nights");

		await click("edit session");
		await click("up");

		expect(at()).toBe("/game-nights");
		expect(index()).toBe(0);
	});

	it("returns from the edit session page to the game sessions when opened directly", async () => {
		const { at, click } = await setup("/sessions/update/8");

		await click("up");

		expect(at()).toBe("/games/3/sessions");
	});
});
