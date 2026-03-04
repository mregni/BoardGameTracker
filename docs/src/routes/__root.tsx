import { Outlet, createRootRoute } from "@tanstack/react-router";
import { Footer } from "../components/Footer";
import { MenuBar } from "../components/MenuBar";

export const Route = createRootRoute({
	component: RootComponent,
});

function RootComponent() {
	return (
		<div className="min-h-screen bg-linear-to-br from-slate-900 via-purple-900 to-slate-900">
			<MenuBar />
			<div className="pt-16">
				<Outlet />
			</div>
			<Footer />
		</div>
	);
}
