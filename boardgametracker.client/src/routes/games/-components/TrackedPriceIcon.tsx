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
	if (livePrice.status === "pending") return "games:tracked-pending";
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
	variant?: "inline" | "overlay";
}

export const TrackedPriceIcon = ({ livePrice, size = "4", variant = "inline" }: Props) => {
	const { t } = useTranslation(["games", "game"]);

	const label = t(tooltipKey(livePrice));
	const wrapperClass =
		variant === "overlay"
			? "flex size-7 shrink-0 items-center justify-center rounded-full bg-black/60"
			: "inline-flex shrink-0";

	return (
		<span role="img" aria-label={label} title={label} className={wrapperClass}>
			<Target aria-hidden="true" className={`${sizeClass[size]} ${iconClass(livePrice)}`} />
		</span>
	);
};
