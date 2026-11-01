import { useTranslation } from "react-i18next";
import AlertTriangle from "@/assets/icons/alert-triangle.svg?react";
import Game from "@/assets/icons/dices.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";

interface Props {
	onRetry: () => void;
}

export const GamesLoadError = ({ onRetry }: Props) => {
	const { t } = useTranslation(["games", "common"]);

	return (
		<BgtEmptyPage
			header={t("title")}
			icon={Game}
			emptyIcon={AlertTriangle}
			title={t("load-error.title")}
			description={t("load-error.description")}
			action={{ label: t("common:try-again"), onClick: onRetry }}
		/>
	);
};
