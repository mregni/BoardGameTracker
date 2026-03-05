import { createFileRoute, Navigate } from "@tanstack/react-router";

export const Route = createFileRoute("/documentation/")({
	component: () => <Navigate to="/documentation/getting-started" />,
});
