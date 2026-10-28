import { useTranslation } from "react-i18next";
import { BgtText } from "../BgtText/BgtText";
import { BgtSpinner } from "./BgtSpinner";

interface Props {
	label?: string;
}

export const BgtLoadingSpinner = ({ label }: Props) => {
	const { t } = useTranslation("common");

	return (
		<div className="flex flex-1 flex-col items-center justify-center gap-3 min-h-[60vh] w-full">
			<BgtSpinner className="size-12 border-4 text-primary" />
			<BgtText color="primary">{label ?? t("loading-data")}</BgtText>
		</div>
	);
};
