import { Download } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
	CodeBlock,
	Paragraph,
	SectionHeader,
} from "../../../components/documentation";

export const QuickStartGuide = () => {
	const { t } = useTranslation();

	return (
		<section id="quick-start-guide">
			<SectionHeader
				icon={Download}
				title={t("getting-started:quickStart.title")}
			/>
			<div className="space-y-4">
				<Paragraph translationKey="getting-started:quickStart.description" />
				<Paragraph translationKey="getting-started:quickStart.step1Description" />
				<CodeBlock label="Download">
					{`wget https://raw.githubusercontent.com/mregni/BoardGameTracker/refs/heads/dev/docker-compose.yml`}
				</CodeBlock>
				<Paragraph translationKey="getting-started:quickStart.step2Description" />
				<CodeBlock label="Docker Compose up">
					{`docker-compose up -d`}
				</CodeBlock>
				<Paragraph translationKey="getting-started:quickStart.step3Description" />
			</div>
		</section>
	);
};
