import { cx } from "class-variance-authority";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Refresh from "@/assets/icons/refresh.svg?react";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";

interface Props {
	onRefresh: () => void;
	isRefreshing: boolean;
}

export const PriceRefreshButton = ({ onRefresh, isRefreshing }: Props) => {
	const { t } = useTranslation("game");
	const [spinning, setSpinning] = useState(false);
	const shouldSpin = spinning || isRefreshing;

	return (
		<BgtIconButton
			intent="subtile"
			icon={
				<Refresh
					className={cx("size-4", shouldSpin && "animate-spin")}
					onAnimationIteration={() => {
						if (!isRefreshing) {
							setSpinning(false);
						}
					}}
				/>
			}
			onClick={() => {
				setSpinning(true);
				onRefresh();
			}}
			disabled={isRefreshing}
			title={t("price.refresh")}
			aria-label={t("price.refresh")}
		/>
	);
};
