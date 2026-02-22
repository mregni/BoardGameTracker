import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_bare")({
	component: BareLayout,
});

function BareLayout() {
	return (
		<main className="size-full text-white bg-background overflow-auto">
			<Outlet />
		</main>
	);
}
