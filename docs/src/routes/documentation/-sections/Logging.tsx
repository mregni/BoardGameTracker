import { ScrollText } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
	CodeBlock,
	Paragraph,
	SectionHeader,
	SubHeader,
} from "../../../components/documentation";

export const Logging = () => {
	const { t } = useTranslation();

	return (
		<section id="logging">
			<SectionHeader icon={ScrollText} title={t("extra:logging.title")} />
			<div className="space-y-4">
				<Paragraph translationKey="extra:logging.description" />
				<SubHeader content={t("extra:logging.viewing")} />
				<Paragraph translationKey="extra:logging.viewingDescription" />
				<CodeBlock label="Docker Logs">
					{`docker logs -f boardgametracker`}
				</CodeBlock>
				<SubHeader content={t("extra:logging.sentry")} />
				<Paragraph translationKey="extra:logging.sentry1Description" />
				<Paragraph translationKey="extra:logging.sentry2Description" />
			</div>
		</section>
	);
};
