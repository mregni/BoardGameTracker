import { Trans } from "react-i18next";
import { InlineCode } from "./InlineCode";

interface ParagraphProps {
	translationKey: string;
}

export const Paragraph = ({ translationKey }: ParagraphProps) => (
	<p className="text-lg text-white">
		<Trans i18nKey={translationKey} components={{ code: <InlineCode /> }} />
	</p>
);
