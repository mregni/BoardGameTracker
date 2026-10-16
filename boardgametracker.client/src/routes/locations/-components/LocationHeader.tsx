import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtText } from "@/components/BgtText/BgtText";
import { textSizeClasses } from "@/components/BgtText/textStyles";
import { LocationPin } from "./LocationPin";

interface Props {
	name: string;
	canWrite: boolean;
	onEdit: () => void;
	onDelete: () => void;
}

export const LocationHeader = (props: Props) => {
	const { name, canWrite, onEdit, onDelete } = props;
	const { t } = useTranslation("location");

	return (
		<div className="flex items-center justify-between gap-3">
			<div className="flex min-w-0 items-center gap-3">
				<LocationPin />
				<div className="min-w-0">
					<BgtText size="2" color="white" opacity={60}>
						{t("detail.label")}
					</BgtText>
					<h1 className={cx(textSizeClasses["7"], "truncate font-bold")} title={name}>
						{name}
					</h1>
				</div>
			</div>
			{canWrite && (
				<div className="shrink-0">
					<BgtEditDeleteButtons onEdit={onEdit} onDelete={onDelete} />
				</div>
			)}
		</div>
	);
};
