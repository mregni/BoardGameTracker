import { useTranslation } from "react-i18next";
import { type Game, GameState } from "@/models";
import { type GamePrice, isPriceError } from "@/models/Games/GamePrice";
import { formatPrice } from "@/utils/priceUtils";

interface Props {
	livePrice?: GamePrice;
	currency?: string;
	uiLanguage?: string;
}

export const LivePrice = ({ livePrice, currency, uiLanguage }: Props) => {
	const { t } = useTranslation("games");

	if (livePrice && isPriceError(livePrice.status)) {
		return (
			<span title={t("live-price.unavailable")} className="text-red-400">
				!
			</span>
		);
	}

	if (!livePrice?.available || livePrice.price == null) {
		return <>-</>;
	}

	return <>{formatPrice(livePrice.price, livePrice.currency ?? currency, uiLanguage)}</>;
};

export const withStateChange = (game: Game, state: GameState, livePrice?: GamePrice): Game => {
	const prefill =
		state === GameState.Owned && !game.buyingPrice && livePrice?.price != null ? { buyingPrice: livePrice.price } : {};
	return { ...game, state, ...prefill };
};
