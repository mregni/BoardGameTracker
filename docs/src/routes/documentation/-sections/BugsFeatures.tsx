import { Bug } from "lucide-react";
import { useTranslation } from "react-i18next";
import GitHubIcon from "../../../assets/icons/github.svg?react";
import { Paragraph, SectionHeader } from "../../../components/documentation";
import ExternalLinkButton from "../../../components/ExternalLinkButton";

export const BugsFeatures = () => {
	const { t } = useTranslation();

	return (
		<section id="bugs-features">
			<SectionHeader icon={Bug} title={t("extra:bugsFeatures.title")} />
			<div className="space-y-4">
				<Paragraph translationKey="extra:bugsFeatures.description" />

				<div className="flex flex-col md:flex-row gap-2">
					<ExternalLinkButton
						href="https://github.com/mregni/BoardGameTracker/issues/new?template=bug_report.md"
						icon={GitHubIcon}
					>
						{t("extra:bugsFeatures.bugsButton")}
					</ExternalLinkButton>
					<ExternalLinkButton
						href="https://github.com/mregni/BoardGameTracker/issues/new?template=feature_request.md"
						icon={GitHubIcon}
					>
						{t("extra:bugsFeatures.featuresButton")}
					</ExternalLinkButton>
					<ExternalLinkButton
						href="https://github.com/mregni/BoardGameTracker/issues/new?template=BLANK_ISSUE"
						icon={GitHubIcon}
					>
						{t("extra:bugsFeatures.otherButton")}
					</ExternalLinkButton>
				</div>
			</div>
		</section>
	);
};
