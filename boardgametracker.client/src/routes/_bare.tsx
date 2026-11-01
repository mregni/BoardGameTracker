import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_bare")({
	component: BareLayout,
});

function BareLayout() {
	return (
		<main className="relative h-dvh w-full text-white bg-background overflow-auto">
			<Outlet />
		</main>
	);
}
