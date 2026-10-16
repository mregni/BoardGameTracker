import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	label?: string;
}

export const BgtLoadingSpinner = ({ label }: Props) => {
	const { t } = useTranslation("common");

	return (
		<div className="flex flex-1 flex-col items-center justify-center gap-3 min-h-[60vh] w-full">
			<Bars className="size-12 text-primary" />
			<BgtText color="primary">{label ?? t("loading-data")}</BgtText>
		</div>
	);
};
