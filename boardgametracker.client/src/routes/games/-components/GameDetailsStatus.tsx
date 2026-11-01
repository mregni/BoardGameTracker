import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSpinner } from "@/components/BgtLoadingSpinner/BgtSpinner";
import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	failed: boolean;
	onRetry: () => void;
}

export const GameDetailsStatus = ({ failed, onRetry }: Props) => {
	const { t } = useTranslation(["game", "common"]);

	if (!failed) {
		return (
			<output className="flex justify-center py-10">
				<BgtSpinner className="size-8 border-4 text-primary" />
				<span className="sr-only">{t("common:loading-data")}</span>
			</output>
		);
	}

	return (
		<div
			role="alert"
			className="flex flex-col items-center gap-3 rounded-lg border border-primary/20 bg-primary/10 px-4 py-8 text-center"
		>
			<BgtText color="white" opacity={70}>
				{t("load-error.details-failed")}
			</BgtText>
			<BgtButton variant="secondary" onClick={onRetry}>
				{t("load-error.retry")}
			</BgtButton>
		</div>
	);
};
