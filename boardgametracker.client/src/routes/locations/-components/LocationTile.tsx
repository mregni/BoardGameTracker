import { useTranslation } from "react-i18next";
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
				<>
					<BgtTileSessions count={location.playCount} />
					{location.lastPlayed && (
						<> · {t("tile.last-played", { when: relativeDay(location.lastPlayed, i18n.language) })}</>
					)}
				</>
			}
		/>
	);
};
