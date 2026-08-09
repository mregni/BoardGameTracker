import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import { BgtText } from "../BgtText/BgtText";

interface Props {
	message?: string;
	className?: string;
}

export const BgtNoData = ({ message, className }: Props) => {
	const { t } = useTranslation("common");

	return (
		<div className={cx("flex items-center justify-center w-full h-full min-h-[120px] py-8 text-center", className)}>
			<BgtText color="white" opacity={50} size="2">
				{message ?? t("no-data-available")}
			</BgtText>
		</div>
	);
};
