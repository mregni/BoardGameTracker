import { createFileRoute } from "@tanstack/react-router";
import { DocumentationLayout, PageTitle } from "../../components/documentation";
import { EnvironmentVariables } from "./-sections/EnvironmentVariables";
import { Docker } from "./-sections/Docker";
import { ProxySetup } from "./-sections/ProxySetup";
import { QuickStartGuide } from "./-sections/QuickStartGuide";

export const Route = createFileRoute("/documentation/getting-started")({
	component: GettingStartedPage,
});

function GettingStartedPage() {
	return (
		<DocumentationLayout activePage="getting-started">
			<PageTitle translationKey="documentation.getting-started" />

			<div className="space-y-16">
				<QuickStartGuide />
				<Docker />
				<EnvironmentVariables />
				<ProxySetup />
			</div>
		</DocumentationLayout>
	);
}
