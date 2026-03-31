import { createFileRoute } from "@tanstack/react-router";
import { DocumentationLayout, PageTitle } from "../../components/documentation";

export const Route = createFileRoute("/documentation/user-guide")({
	component: UserGuidePage,
});

function UserGuidePage() {
	return (
		<DocumentationLayout activePage="user-guide">
			<PageTitle translationKey="documentation.user-guide" />
			{/* Content will be added later */}
		</DocumentationLayout>
	);
}
