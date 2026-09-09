import { useTranslation } from "react-i18next";
import Target from "@/assets/icons/target.svg?react";
import { type GamePrice, isPriceError, priceErrorKey } from "@/models/Games/GamePrice";

const iconClass = (livePrice: GamePrice | undefined): string => {
	if (!livePrice) return "text-white/40";
	if (isPriceError(livePrice.status)) return "text-amber-400";
	if (!livePrice.available || livePrice.inStock == null) return "text-white/40";
	return livePrice.inStock ? "text-green-400" : "text-red-400";
};

const tooltipKey = (livePrice: GamePrice | undefined): string => {
	if (!livePrice) return "games:tracked";
	if (isPriceError(livePrice.status)) return priceErrorKey(livePrice.status);
	if (!livePrice.available || livePrice.inStock == null) return "games:tracked";
	return livePrice.inStock ? "games:in-stock.yes" : "games:in-stock.no";
};

const sizeClass = {
	"4": "size-4",
	"5": "size-5",
	"6": "size-6",
} as const;

interface Props {
	livePrice: GamePrice | undefined;
	size?: keyof typeof sizeClass;
}

export const TrackedPriceIcon = ({ livePrice, size = "4" }: Props) => {
	const { t } = useTranslation(["games", "game"]);

	return (
		<span title={t(tooltipKey(livePrice))} className="inline-flex shrink-0">
			<Target className={`${sizeClass[size]} ${iconClass(livePrice)}`} />
		</span>
	);
};
