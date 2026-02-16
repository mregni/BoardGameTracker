import { createFileRoute } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import MapPinIcon from "@/assets/icons/map-pin.svg?react";
import PencilIcon from "@/assets/icons/pencil.svg?react";
import TrashIcon from "@/assets/icons/trash.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import type { Location } from "@/models";
import { EditLocationModal } from "@/routes/locations/-modals/EditLocationModal";
import { NewLocationModal } from "@/routes/locations/-modals/NewLocationModal";
import { BgtDeleteModal } from "../-modals/BgtDeleteModal";
import { useLocationModals } from "./-hooks/useLocationModals";
import { useLocationsData } from "./-hooks/useLocationsData";

export const Route = createFileRoute("/locations/")({
	component: RouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation();
	const modals = useLocationModals();
	const [selectedLocation, setSelectedLocation] = useState<Location | null>(null);

	const onDeleteSuccess = () => {
		modals.deleteModal.hide();
		setSelectedLocation(null);
	};

	const { locations, deleteLocation } = useLocationsData({ onDeleteSuccess });

	const columns: DataTableProps<Location>["columns"] = useMemo(
		() => [
			{
				accessorKey: "0",
				cell: ({ row }) => <div>{row.original.id}</div>,
				header: t("common.id"),
			},
			{
				accessorKey: "1",
				cell: ({ row }) => <div>{row.original.name}</div>,
				header: t("common.name"),
			},
			{
				accessorKey: "2",
				cell: ({ row }) => <div className="flex justify-end">{row.original.playCount}</div>,
				header: () => <div className="flex justify-end">{t("common.count")}</div>,
			},
			{
				accessorKey: "3",
				cell: ({ row }) => (
					<div className="flex flex-row justify-end gap-2">
						<BgtIconButton
							icon={<PencilIcon className="size-5" />}
							onClick={() => {
								setSelectedLocation(row.original);
								modals.editModal.show();
							}}
						/>
						<BgtIconButton
							icon={<TrashIcon className="size-5" />}
							intent="danger"
							onClick={() => {
								setSelectedLocation(row.original);
								modals.deleteModal.show();
							}}
						/>
					</div>
				),
				header: "",
			},
		],
		[t, modals.editModal, modals.deleteModal],
	);

	if (locations.length === 0) {
		return (
			<BgtEmptyPage
				header={t("common.locations")}
				icon={MapPinIcon}
				title={t("location.empty.title")}
				description={t("location.empty.description")}
				action={{
					label: t("location.new.button"),
					onClick: modals.createModal.show,
				}}
			>
				<NewLocationModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
			</BgtEmptyPage>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader
				icon={MapPinIcon}
				header={t("common.locations")}
				actions={[
					{
						onClick: modals.createModal.show,
						variant: "primary",
						content: "location.new.button",
					},
				]}
			/>
			<BgtPageContent>
				<BgtCard className="p-4">
					<BgtDataTable
						columns={columns}
						data={locations}
						noDataMessage={t("common.no-data-yet")}
						widths={["w-[70px]", "w-[100px]", "", "w-[50px]"]}
					/>
				</BgtCard>
				<NewLocationModal open={modals.createModal.isOpen} close={modals.createModal.hide} />
				<BgtDeleteModal
					title={selectedLocation?.name ?? ""}
					open={modals.deleteModal.isOpen}
					close={modals.deleteModal.hide}
					onDelete={() => selectedLocation && deleteLocation(selectedLocation.id)}
					description={
						t("location.delete.description", {
							name: selectedLocation?.name ?? "",
						}) +
						" " +
						((selectedLocation?.playCount ?? 0) > 0
							? t("location.delete.extra-description", {
									count: selectedLocation?.playCount ?? 0,
								})
							: "")
					}
				/>
				{selectedLocation && (
					<EditLocationModal location={selectedLocation} open={modals.editModal.isOpen} close={modals.editModal.hide} />
				)}
			</BgtPageContent>
		</BgtPage>
	);
}
