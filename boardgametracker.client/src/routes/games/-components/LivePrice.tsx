import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { type Game, GameState } from "@/models";
import { type GamePrice, isPriceError, priceErrorKey } from "@/models/Games/GamePrice";
import { formatPrice } from "@/utils/numberUtils";

interface Props {
	livePrice?: GamePrice;
	currency?: string;
	uiLanguage?: string;
}

export const LivePrice = ({ livePrice, currency, uiLanguage }: Props) => {
	const { t } = useTranslation(["games", "game"]);
	const price = livePrice?.price;
	const lastPrice = useRef(price);
	const [flashes, setFlashes] = useState(0);

	useEffect(() => {
		if (lastPrice.current != null && price != null && lastPrice.current !== price) setFlashes((count) => count + 1);
		lastPrice.current = price;
	}, [price]);

	if (livePrice && isPriceError(livePrice.status)) {
		const reason = t(priceErrorKey(livePrice.status));
		return (
			<span role="img" aria-label={reason} title={reason} className="text-red-400">
				!
			</span>
		);
	}

	if (livePrice?.status === "pending") {
		return <span title={t("games:tracked-pending")}>-</span>;
	}

	if (!livePrice?.available || livePrice.price == null) {
		return <>-</>;
	}

	return (
		<span key={flashes} className={flashes > 0 ? "-mx-1 rounded px-1 animate-value-flash" : undefined}>
			{formatPrice(livePrice.price, livePrice.currency ?? currency, uiLanguage)}
		</span>
	);
};

export const withStateChange = (game: Game, state: GameState, livePrice?: GamePrice): Game => {
	const prefill =
		state === GameState.Owned && !game.buyingPrice && livePrice?.price != null ? { buyingPrice: livePrice.price } : {};
	return { ...game, state, ...prefill };
};
