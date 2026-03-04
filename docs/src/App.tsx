import { createRouter, RouterProvider } from "@tanstack/react-router";
import { routeTree } from "./routeTree.gen";
import { Spinner } from "./components/Spinner";

const router = createRouter({
	routeTree,
	defaultPreload: "intent",
	defaultViewTransition: true,
	defaultPendingComponent: Spinner,
	defaultPendingMinMs: 200,
});

declare module "@tanstack/react-router" {
	interface Register {
		router: typeof router;
	}
}

function App() {
	return <RouterProvider router={router} />;
}

export default App;
