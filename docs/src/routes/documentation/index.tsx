import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/documentation/")({
	component: RouteComponent,
});

function RouteComponent() {
	return <div>Hello "/documentation/"!</div>;
}
