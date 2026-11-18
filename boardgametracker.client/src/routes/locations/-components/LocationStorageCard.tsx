import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Package from "@/assets/icons/package.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { type Location, StorageLayout, type StorageLocation } from "@/models";
import { childrenOf, itemsAt, itemsIn, nodeName, placeKey, rootsOf, type StorageItem } from "@/utils/storage";

interface Props {
	location: Location;
	nodes: StorageLocation[];
	items: StorageItem[];
}

export const LocationStorageCard = (props: Props) => {
	const { location, nodes, items } = props;
	const { t } = useTranslation(["location", "storage"]);

	const here = { locationId: location.id, storageLocationId: null };
	const roots = rootsOf(nodes, location.id);
	const rows = [
		...roots.map((root) => ({
			key: root.id,
			name: nodeName(root, nodes, t),
			inside: childrenOf(nodes, root.id)
				.filter((child) => child.layout !== StorageLayout.Slot)
				.map((child) => nodeName(child, nodes, t))
				.join(", "),
			count: itemsIn(items, { locationId: location.id, storageLocationId: root.id }, nodes).length,
		})),
		{
			key: "loose",
			name: t("storage:place.somewhere-in", { name: location.name }),
			inside: "",
			count: itemsAt(items, here).length,
		},
	];
	const isEmpty = roots.length === 0 && itemsIn(items, here, nodes).length === 0;

	return (
		<BgtCard>
			<div className="mb-4 flex items-center justify-between gap-3">
				<h2 className="flex items-center gap-2 tracking-wide text-white">
					<Package aria-hidden="true" className="size-6 text-primary" />
					{t("detail.storage.title")}
				</h2>
				<Link to="/storage" search={{ at: placeKey(here) }} className="text-sm text-primary-text hover:text-white">
					{t("detail.storage.open")}
				</Link>
			</div>
			{isEmpty ? (
				<BgtText size="2" color="white" opacity={50}>
					{t("detail.storage.empty")}
				</BgtText>
			) : (
				<ul className="flex flex-col divide-y divide-primary/10">
					{rows.map((row) => (
						<li key={row.key} className="flex items-center gap-3 py-2">
							<span className="flex min-w-0 flex-1 flex-col">
								<span className="truncate font-semibold">{row.name}</span>
								{row.inside && <span className="truncate text-xs text-white/50">{row.inside}</span>}
							</span>
							<span className="shrink-0 text-sm tabular-nums text-white/60">
								<span className="font-semibold text-white">{row.count}</span>{" "}
								{t("detail.storage.count", { count: row.count })}
							</span>
						</li>
					))}
				</ul>
			)}
		</BgtCard>
	);
};
