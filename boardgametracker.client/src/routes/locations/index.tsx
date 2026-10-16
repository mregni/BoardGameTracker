import { noop } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import { SearchInputField } from "@/components/BgtForm";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtText } from "@/components/BgtText/BgtText";
import { BgtTileGrid } from "@/components/BgtTile/BgtTileGrid";
import { useFilteredList } from "@/hooks/useFilteredList";
import { usePermissions } from "@/hooks/usePermissions";
import { NewLocationModal } from "@/routes/locations/-modals/NewLocationModal";
import { getLocations } from "@/services/queries/locations";
import { LocationTile } from "./-components/LocationTile";
import { useLocationModals } from "./-hooks/useLocationModals";
import { useLocationsData } from "./-hooks/useLocationsData";

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
	const { locations, isLoading } = useLocationsData();
	const { filterValue, setFilterValue, filtered: filteredLocations } = useFilteredList(locations, "name");

	if (isLoading) return null;

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
								},
							]
						: []
				}
			/>
			<BgtPageContent>
				<div className="w-full md:w-64 xl:w-[300px]">
					<SearchInputField value={filterValue} onChange={(event) => setFilterValue(event.target.value)} />
				</div>
				<BgtText size="3" className="pb-6 text-primary-text" weight="medium">
					{t("location:count", { count: filteredLocations.length })}
				</BgtText>
				<BgtTileGrid>
					{filteredLocations.map((x) => (
						<LocationTile key={x.id} location={x} />
					))}
				</BgtTileGrid>
				<NewLocationModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtPageContent>
		</BgtPage>
	);
}
