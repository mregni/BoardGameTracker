import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Package from "@/assets/icons/package.svg?react";
import { BgtTile } from "@/components/BgtTile/BgtTile";
import { BgtTileSessions } from "@/components/BgtTile/BgtTileSessions";
import type { Location } from "@/models";
import { relativeDay } from "@/utils/relativeDay";
import { LocationPin } from "./LocationPin";

interface Props {
	location: Location;
}

export const LocationTile = (props: Props) => {
	const { location } = props;
	const { t, i18n } = useTranslation("location");

	return (
		<BgtTile
			title={location.name}
			link={`/locations/${location.id}`}
			visual={<LocationPin />}
			stats={
				<span className="flex flex-wrap gap-x-3 gap-y-0.5">
					<span className="inline-flex items-center gap-1">
						<Calendar aria-hidden="true" className="size-3.5 shrink-0" />
						<span>
							<BgtTileSessions count={location.playCount} />
							{location.lastPlayed && (
								<> · {t("tile.last-played", { when: relativeDay(location.lastPlayed, i18n.language) })}</>
							)}
						</span>
					</span>
					<span className="inline-flex items-center gap-1">
						<Package aria-hidden="true" className="size-3.5 shrink-0" />
						<span>
							<span className="font-semibold text-white">{location.storedCount}</span>{" "}
							{t("tile.stored", { count: location.storedCount })}
						</span>
					</span>
				</span>
			}
		/>
	);
};
