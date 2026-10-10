import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Coins from "@/assets/icons/coins.svg?react";
import { BgtFallbackImage } from "@/components/BgtImage/BgtFallbackImage";
import { BgtText } from "@/components/BgtText/BgtText";
import type { Shame } from "@/models";
import { getDaysSincePurchase, toDisplay } from "@/utils/dateUtils";
import { formatPrice } from "@/utils/priceUtils";

interface Props {
	shame: Shame;
	dateFormat: string;
	currency: string;
}

const ShameDataLines = ({ content, title }: { content: string; title: string }) => {
	return (
		<div className="flex items-center justify-between text-xs">
			<BgtText color="white" opacity={50} size="2" className="flex items-center gap-1">
				<Calendar className="size-4" />
				<span>{title}</span>
			</BgtText>
			<BgtText color="white" size="2" weight="bold">
				{content}
			</BgtText>
		</div>
	);
};

export const ShameGame = ({ shame, dateFormat, currency }: Props) => {
	const { t, i18n } = useTranslation(["common", "shames"]);
	const link = `/games/${shame.id}`;

	const daysLastSession = getDaysSincePurchase(shame.lastSessionDate);

	return (
		<Link to={link} from="/shames/">
			<div className="flex flex-col justify-center cursor-pointer flex-nowrap relative group gap-1 bg-primary/20 rounded-lg hover:border-primary/50 transition-all border border-white/10">
				<div className="aspect-square overflow-hidden border border-none transition-all duration-200 relative rounded-t-lg">
					<BgtFallbackImage
						title={shame.title}
						image={shame.image}
						lazy
						className="aspect-square group-hover:scale-105 transition-transform duration-200"
					/>
					{daysLastSession > 0 && (
						<div className="absolute top-2 right-2 bg-red-500/90 backdrop-blur-sm px-2 py-1 rounded-lg z-20">
							<p className="text-xs font-bold text-white">{t("day", { count: daysLastSession })}</p>
						</div>
					)}
				</div>
				<div className="flex flex-col items-start justify-start pb-2 px-2">
					<BgtText size="4" className="line-clamp-1 w-full" weight="medium">
						{shame.title}
					</BgtText>
					<div className="flex flex-col w-full gap-1">
						<ShameDataLines
							content={
								shame.lastSessionDate !== null
									? toDisplay(shame.lastSessionDate, dateFormat, i18n.language)
									: t("never")
							}
							title={t("shames:last-session")}
						/>
						<div className="flex items-center justify-between text-xs">
							<BgtText color="white" opacity={50} size="2" className="flex items-center gap-1">
								<Coins className="size-4" />
								<span>{t("price")}</span>
							</BgtText>
							<BgtText color="cyan" weight="bold" size="2">
								{formatPrice(shame.price, currency, i18n.language)}
							</BgtText>
						</div>
					</div>
				</div>
			</div>
		</Link>
	);
};
