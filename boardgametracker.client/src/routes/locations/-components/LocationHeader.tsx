import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtText } from "@/components/BgtText/BgtText";
import { textSizeClasses } from "@/components/BgtText/textStyles";
import { relativeDay } from "@/utils/relativeDay";
import { LocationPin } from "./LocationPin";

interface Props {
	name: string;
	lastPlayed: Date | string | null;
	lastGame?: string;
	uiLanguage: string;
	canWrite: boolean;
	onEdit: () => void;
	onDelete: () => void;
}

export const LocationHeader = (props: Props) => {
	const { name, lastPlayed, lastGame, uiLanguage, canWrite, onEdit, onDelete } = props;
	const { t } = useTranslation("location");

	const subtitle = lastPlayed
		? [t("detail.last-played", { when: relativeDay(lastPlayed, uiLanguage) }), lastGame].filter(Boolean).join(" · ")
		: t("detail.never-played");

	return (
		<div className="flex items-center justify-between gap-3">
			<div className="flex min-w-0 items-center gap-3">
				<LocationPin />
				<div className="min-w-0">
					<h1 className={cx(textSizeClasses["7"], "truncate font-bold")} title={name}>
						{name}
					</h1>
					<BgtText size="2" color="white" opacity={60} className="truncate">
						{subtitle}
					</BgtText>
				</div>
			</div>
			{canWrite && (
				<div className="shrink-0">
					<BgtEditDeleteButtons name={name} onEdit={onEdit} onDelete={onDelete} />
				</div>
			)}
		</div>
	);
};
