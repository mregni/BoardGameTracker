import { Languages } from "lucide-react";
import { useTranslation } from "react-i18next";
import CrowdinIcon from "../../../assets/icons/crowdin.svg?react";
import {
	Paragraph,
	SectionHeader,
	SubHeader,
} from "../../../components/documentation";
import ExternalLinkButton from "../../../components/ExternalLinkButton";

export const Translations = () => {
	const { t } = useTranslation();

	return (
		<section id="translations">
			<SectionHeader icon={Languages} title={t("extra:translations.title")} />
			<div className="space-y-4">
				<Paragraph translationKey="extra:translations.description" />
				<ExternalLinkButton
					href="https://crowdin.com/project/boardgametracker"
					icon={CrowdinIcon}
				>
					{t("extra:translations.crowdinButton")}
				</ExternalLinkButton>
				<SubHeader content={t("extra:translations.howTo")} />

				<Paragraph translationKey="extra:translations.howToDescription" />
				<img
					src={`${import.meta.env.BASE_URL}images/crowdin.png`}
					alt={t("extra:development.crowdinAlt")}
					className="w-full h-auto"
				/>
			</div>
		</section>
	);
};
