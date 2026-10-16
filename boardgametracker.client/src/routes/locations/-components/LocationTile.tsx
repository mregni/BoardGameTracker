import { BgtTile } from "@/components/BgtTile/BgtTile";
import { BgtTileSessions } from "@/components/BgtTile/BgtTileSessions";
import type { Location } from "@/models";
import { LocationPin } from "./LocationPin";

interface Props {
	location: Location;
}

export const LocationTile = (props: Props) => {
	const { location } = props;

	return (
		<BgtTile
			title={location.name}
			link={`/locations/${location.id}`}
			visual={<LocationPin />}
			stats={<BgtTileSessions count={location.playCount} />}
		/>
	);
};
