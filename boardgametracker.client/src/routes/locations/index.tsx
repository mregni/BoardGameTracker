import { noop } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import { SearchInputField } from "@/components/BgtForm";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { BgtTileGrid } from "@/components/BgtTile/BgtTileGrid";
import { useFilteredList } from "@/hooks/useFilteredList";
import { usePermissions } from "@/hooks/usePermissions";
import type { Location } from "@/models";
import { NewLocationModal } from "@/routes/locations/-modals/NewLocationModal";
import { getLocations } from "@/services/queries/locations";
import { LocationTile } from "./-components/LocationTile";
import { useLocationModals } from "./-hooks/useLocationModals";
import { useLocationsData } from "./-hooks/useLocationsData";

type Sort = "recent" | "most-played" | "name";

const lastPlayedTime = (location: Location) => (location.lastPlayed ? new Date(location.lastPlayed).getTime() : 0);

const compare: Record<Sort, (a: Location, b: Location) => number> = {
	recent: (a, b) => lastPlayedTime(b) - lastPlayedTime(a) || b.playCount - a.playCount || a.name.localeCompare(b.name),
	"most-played": (a, b) => b.playCount - a.playCount || a.name.localeCompare(b.name),
	name: (a, b) => a.name.localeCompare(b.name),
};

export const Route = createFileRoute("/locations/")({
	component: RouteComponent,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getLocations()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation(["common", "location"]);
	const { canWrite } = usePermissions();
	const modals = useLocationModals();
	const { locations, isLoading, isError, refetch } = useLocationsData();
	const { filterValue, setFilterValue, filtered } = useFilteredList(locations, "name");
	const [sort, setSort] = useState<Sort>("recent");
	const filteredLocations = [...filtered].sort(compare[sort]);

	if (isLoading) return <BgtLoadingSpinner />;

	if (isError) {
		return (
			<BgtEmptyPage
				header={t("locations")}
				icon={MapPinIcon}
				title={t("location:error.title")}
				description={t("location:error.description")}
				action={{ label: t("common:retry"), onClick: () => refetch() }}
			/>
		);
	}

	if (locations.length === 0) {
		return (
			<BgtEmptyPage
				header={t("locations")}
				icon={MapPinIcon}
				title={t("location:empty.title")}
				description={t("location:empty.description")}
				action={canWrite ? { label: t("location:new.button"), onClick: modals.createModal.show } : undefined}
			>
				<NewLocationModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				icon={MapPinIcon}
				header={t("locations")}
				actions={
					canWrite
						? [
								{
									onClick: modals.createModal.show,
									variant: "primary",
									content: "location:new.button",
									icon: PlusIcon,
								},
							]
						: []
				}
			/>
			<BgtPageContent>
				<div className="flex flex-col gap-3 pb-6 md:flex-row md:items-center md:justify-between">
					<div className="w-full md:w-64 xl:w-[300px]">
						<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
					</div>
					<BgtSegmentedControl
						aria-label={t("location:sort.label")}
						size="sm"
						value={sort}
						onChange={setSort}
						options={[
							{ value: "recent", label: t("location:sort.recent") },
							{ value: "most-played", label: t("location:sort.most-played") },
							{ value: "name", label: t("location:sort.name") },
						]}
					/>
				</div>
				{filteredLocations.length === 0 ? (
					<BgtEmptyState
						icon={MapPinIcon}
						title={t("location:filter.no-matches", { filter: filterValue })}
						action={{ label: t("location:filter.clear"), onClick: () => setFilterValue("") }}
					/>
				) : (
					<BgtTileGrid>
						{filteredLocations.map((x) => (
							<LocationTile key={x.id} location={x} />
						))}
					</BgtTileGrid>
				)}
				<NewLocationModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtPageContent>
		</BgtPage>
	);
}
