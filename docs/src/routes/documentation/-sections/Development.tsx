import { Code } from "lucide-react";
import { useTranslation } from "react-i18next";
import {
	BulletList,
	CodeBlock,
	Paragraph,
	SectionHeader,
	SubHeader,
} from "../../../components/documentation";

export const Development = () => {
	const { t } = useTranslation();

	return (
		<section id="development">
			<SectionHeader icon={Code} title={t("extra:development.title")} />
			<div className="space-y-4">
				<Paragraph translationKey="extra:development.description" />

				<SubHeader content={t("extra:development.prerequisites")} />
				<BulletList
					items={
						t("extra:development.prerequisiteItems", {
							returnObjects: true,
						}) as unknown as string[]
					}
				/>

				<SubHeader content={t("extra:development.setup")} />
				<Paragraph translationKey="extra:development.step1Description" />
				<CodeBlock label="Clone & Install">
					{`git clone https://github.com/mregni/BoardGameTracker.git
cd BoardGameTracker
dotnet restore
cd boardgametracker.client
npm install`}
				</CodeBlock>

				<Paragraph translationKey="extra:development.step2Description" />
				<CodeBlock label="Run">
					{`set JWT_SECRET=CHANGEME
dotnet run --project BoardGameTracker.Host`}
				</CodeBlock>

				<Paragraph translationKey="extra:development.step3Description" />
				<Paragraph translationKey="extra:development.step4Description" />
			</div>
		</section>
	);
};
