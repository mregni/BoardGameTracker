import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import MapPin from "@/assets/icons/map-pin.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { GameNight, Settings } from "@/models";
import { toLongDisplay } from "@/utils/dateUtils";
import { relativeDay } from "@/utils/relativeDay";

interface Props {
	gameNight: GameNight;
	settings: Settings;
}

export const RsvpEventDetails = ({ gameNight, settings }: Props) => {
	const { t } = useTranslation(["rsvp", "game-nights"]);
	const { startDate } = gameNight;
	const { uiLanguage } = settings;

	return (
		<BgtCard className="gap-4" data-testid="rsvp-event-details">
			<div className="flex gap-3">
				<GameNightDateBox
					date={startDate}
					uiLanguage={uiLanguage}
					variant="highlight"
					className="w-16 shrink-0 self-start md:w-20"
				/>
				<div className="flex min-w-0 flex-col items-start gap-1.5">
					<span className="text-xs font-semibold uppercase tracking-widest text-primary-text">{t("invited")}</span>
					<span className="rounded-full bg-primary/20 px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap text-indigo-300 tabular-nums first-letter:uppercase">
						{t("game-nights:card.when", {
							when: relativeDay(startDate, uiLanguage),
							time: format(startDate, settings.timeFormat),
						})}
					</span>
					<BgtHeading as="h1" size="6" className="wrap-anywhere">
						{gameNight.title}
					</BgtHeading>
					<BgtText size="2" color="gray" className="first-letter:uppercase">
						{toLongDisplay(startDate, uiLanguage)}
					</BgtText>
				</div>
			</div>

			<div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-gray-300">
				<span className="inline-flex items-center gap-1.5">
					<MapPin className="size-4 shrink-0 text-primary" />
					{gameNight.location.name}
				</span>
				<span className="inline-flex items-center gap-2">
					<span className="text-gray-400">{t("hosted-by")}</span>
					<BgtAvatar title={gameNight.host.name} image={gameNight.host.image} size="small" withTitle />
				</span>
			</div>

			{gameNight.notes && (
				<div className="rounded-lg border border-white/10 bg-white/5 p-3">
					<BgtText size="2" color="gray" className="whitespace-pre-line">
						{gameNight.notes}
					</BgtText>
				</div>
			)}
		</BgtCard>
	);
};
