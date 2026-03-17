import { createFileRoute } from "@tanstack/react-router";
import { DocumentationLayout, PageTitle } from "../../components/documentation";
import { BugsFeatures } from "./-sections/BugsFeatures";
import { Development } from "./-sections/Development";
import { Logging } from "./-sections/Logging";
import { Translations } from "./-sections/Translations";

export const Route = createFileRoute("/documentation/extra")({
	component: ExtraPage,
});

function ExtraPage() {
	return (
		<DocumentationLayout activePage="extra">
			<PageTitle translationKey="documentation.extra" />
			<div className="space-y-16">
				<Development />
				<Translations />
				<BugsFeatures />
				<Logging />
			</div>
		</DocumentationLayout>
	);
}
